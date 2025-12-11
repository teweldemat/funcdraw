using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FuncScript.Model;
using FuncScript.Package;
using FuncScript.Error;
using FuncScript.Core;

namespace FuncDraw.Net;

internal sealed record EvaluationRequest(
    double? Time,
    double? CanvasWidth,
    double? CanvasHeight,
    bool IncludeSvg,
    TraceOptions? Trace,
    string? ExpressionOverride = null,
    IReadOnlyList<object?>? Events = null,
    bool ResetState = false);

internal sealed class SceneService
{
    private readonly string _projectRoot;
    private ArtResolver _resolver;
    private readonly string? _expressionOverride;
    private double _timeline;
    private double _canvasWidth;
    private double _canvasHeight;
    private object? _state;
    private readonly object _stateLock = new();

    public SceneService(string projectRoot, string? expressionOverride = null, double? initialTime = null)
    {
        _projectRoot = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
        _resolver = new ArtResolver(_projectRoot);
        _expressionOverride = NormalizeExpressionOverride(expressionOverride);
        _timeline = initialTime ?? 0;
        _canvasWidth = 40;
        _canvasHeight = 30;
    }

    public string WatchPath => _resolver.WatchPath;

    public ScenePayload Evaluate(EvaluationRequest request)
    {
        lock (_stateLock)
        {
            if (request.ResetState)
            {
                _state = null;
            }

            if (request.Time.HasValue)
            {
                _timeline = request.Time.Value;
            }

            if (request.CanvasWidth.HasValue)
            {
                _canvasWidth = request.CanvasWidth.Value;
            }

            if (request.CanvasHeight.HasValue)
            {
                _canvasHeight = request.CanvasHeight.Value;
            }

            var queue = new Queue<object?>(request.Events ?? Array.Empty<object?>());
            var includeSvg = request.IncludeSvg && queue.Count == 0;
            var result = EvaluateOnce(includeSvg, request.Trace, request.ExpressionOverride);
            while (queue.Count > 0)
            {
                var step = result.Raw.StepFunction;
                if (step == null)
                {
                    throw new InvalidOperationException("Stepper events were provided but the model did not return a step function.");
                }

                var stepResult = RunStep(step, _state, queue.Dequeue());
                _state = stepResult.State;
                foreach (var evt in stepResult.Events)
                {
                    queue.Enqueue(evt);
                }

                includeSvg = request.IncludeSvg && queue.Count == 0;
                result = EvaluateOnce(includeSvg, request.Trace, request.ExpressionOverride);
            }

            var plainState = _state == null ? null : new ValueConverter().ToPlain(_state);
            return new ScenePayload(result, _timeline, _canvasWidth, _canvasHeight, request.IncludeSvg, plainState);
        }
    }

    private SceneResult EvaluateOnce(bool includeSvg, TraceOptions? traceOptions, string? expressionOverride)
    {
        var hooks = new Dictionary<string, Func<object?>>
        {
            ["t"] = () => _timeline,
            ["canvas"] = () => BuildCanvasValue(),
            ["state"] = () => _state
        };

        return FuncDrawRuntime.LoadGraphics(
            _resolver,
            new FuncDrawOptions
            {
                IncludeSvg = includeSvg,
                ValueHooks = hooks,
                Trace = traceOptions,
                ExpressionOverride = string.IsNullOrWhiteSpace(expressionOverride)
                    ? _expressionOverride
                    : expressionOverride
            });
    }

    public void Reload()
    {
        _resolver = new ArtResolver(_projectRoot);
        _timeline = 0;
        _state = null;
    }

    private object BuildCanvasValue()
    {
        var size = new SimpleKeyValueCollection(null, new[]
        {
            KeyValuePair.Create("width", (object)_canvasWidth),
            KeyValuePair.Create("height", (object)_canvasHeight)
        });

        var canvasEntries = new[]
        {
            KeyValuePair.Create("size", (object)size)
        };

        return new SimpleKeyValueCollection(null, canvasEntries);
    }

    private static string? NormalizeExpressionOverride(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var trimmed = expression.Trim();
        return trimmed.Length > 0 ? trimmed : null;
    }

    private static StepResult RunStep(IFsFunction stepFunction, object? state, object? evt)
    {
        var args = new ArrayFsList(new[] { state ?? (object?)null, evt ?? (object?)null });
        var raw = stepFunction.Evaluate(args);
        if (raw is FsError error)
        {
            throw new InvalidOperationException($"Stepper function failed: {error.ErrorMessage}");
        }

        return NormalizeStepResult(raw);
    }

    private static StepResult NormalizeStepResult(object raw)
    {
        if (raw is FsList list && list.Length >= 2)
        {
            return new StepResult(list[0], NormalizeEventList(list[1]));
        }

        if (raw is KeyValueCollection kvc)
        {
            var state = kvc.Get("nextState") ?? kvc.Get("state");
            var events = kvc.Get("events") ?? kvc.Get("outEvents");
            return new StepResult(state, NormalizeEventList(events));
        }

        if (raw is IEnumerable enumerable && raw is not string)
        {
            var flattened = new List<object?>();
            foreach (var item in enumerable)
            {
                flattened.Add(item);
            }

            if (flattened.Count >= 2)
            {
                return new StepResult(flattened[0], NormalizeEventList(flattened[1]));
            }
        }

        throw new InvalidOperationException("Stepper functions must return [nextState, events] or { state, events }.");
    }

    private static List<object?> NormalizeEventList(object? value)
    {
        if (value == null)
        {
            return new List<object?>();
        }

        if (value is FsList list)
        {
            var events = new List<object?>();
            foreach (var item in list)
            {
                events.Add(item);
            }
            return events;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var events = new List<object?>();
            foreach (var item in enumerable)
            {
                events.Add(item);
            }
            return events;
        }

        return new List<object?> { value };
    }

    private sealed record StepResult(object? State, List<object?> Events);
}

internal sealed class ScenePayload
{
    public ScenePayload(SceneResult result, double timeline, double canvasWidth, double canvasHeight, bool includeSvg, object? state)
    {
        Graphics = result.Graphics;
        View = result.View;
        Warnings = result.Warnings;
        Raw = result.Raw;
        ValueHooks = result.ValueHooks;
        Svg = includeSvg ? result.Svg : null;
        Timeline = new Dictionary<string, object> { ["t"] = timeline };
        Canvas = new Dictionary<string, object>
        {
            ["width"] = canvasWidth,
            ["height"] = canvasHeight
        };
        Trace = result.Trace;
        State = state;
    }

    public List<object> Graphics { get; }
    public object? View { get; }
    public List<string> Warnings { get; }
    public SceneInterpretation Raw { get; }
    public Dictionary<string, HookUsage>? ValueHooks { get; }
    public string? Svg { get; }
    public Dictionary<string, object> Timeline { get; }
    public Dictionary<string, object> Canvas { get; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TraceEntry>? Trace { get; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? State { get; }
}

internal sealed class FuncDrawServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly SceneService _service;
    private readonly string _html;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<HttpListenerResponse> _eventSinks = new();
    private readonly object _sync = new();
    private readonly CancellationTokenSource _cts = new();

    private FuncDrawServer(SceneService service, string host, int port, string html)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _html = html ?? throw new ArgumentNullException(nameof(html));
        _listener = new HttpListener();
        var prefixHost = host == "0.0.0.0" ? "*" : host;
        _listener.Prefixes.Add($"http://{prefixHost}:{port}/");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public static async Task<FuncDrawServer> StartAsync(SceneService service, string host, int port, string html)
    {
        var server = new FuncDrawServer(service, host, port, html);
        server.Start();
        await Task.CompletedTask;
        return server;
    }

    public void BroadcastReload()
    {
        List<HttpListenerResponse>? stale = null;
        lock (_sync)
        {
            foreach (var sink in _eventSinks)
            {
                try
                {
                    WriteEvent(sink, "reload", "{}");
                }
                catch
                {
                    stale ??= new List<HttpListenerResponse>();
                    stale.Add(sink);
                }
            }

            if (stale != null)
            {
                foreach (var sink in stale)
                {
                    _eventSinks.Remove(sink);
                    try
                    {
                        sink.OutputStream.Dispose();
                        sink.Close();
                    }
                    catch
                    {
                        // ignore close errors
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Close();
        lock (_sync)
        {
            foreach (var sink in _eventSinks)
            {
                try
                {
                    sink.OutputStream.Dispose();
                    sink.Close();
                }
                catch
                {
                    // ignore
                }
            }

            _eventSinks.Clear();
        }
    }

    private void Start()
    {
        _listener.Start();
        Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleRequestAsync(context), _cts.Token);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            if (path == "/")
            {
                await HandleRootAsync(context.Response);
                return;
            }

            if (path == "/__funcdraw/scene")
            {
                await HandleSceneAsync(context);
                return;
            }

            if (path == "/__funcdraw/events")
            {
                HandleEvents(context);
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }
        catch
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch
            {
                // ignore double-fault
            }
        }
    }

    private Task HandleRootAsync(HttpListenerResponse response)
    {
        var buffer = Encoding.UTF8.GetBytes(_html);
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        var writeTask = response.OutputStream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
        writeTask.ContinueWith(_ => SafeClose(response));
        return writeTask;
    }

    private async Task HandleSceneAsync(HttpListenerContext context)
    {
        var request = await BuildEvaluationRequestAsync(context.Request);

        var payload = _service.Evaluate(request);
        await WriteJsonAsync(context.Response, payload);
        SafeClose(context.Response);
    }

    private async Task<EvaluationRequest> BuildEvaluationRequestAsync(HttpListenerRequest request)
    {
        var query = request.QueryString;
        var includeSvg = query["svg"] != null;
        var resetState = query["resetState"] != null;
        var time = ParseDouble(query["time"]);
        var canvasWidth = ParseDouble(query["canvasWidth"]);
        var canvasHeight = ParseDouble(query["canvasHeight"]);
        var expressionOverride = query["exp"];
        var events = ParseEventsFromQuery(query);

        if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && request.HasEntityBody)
        {
            using var doc = await JsonDocument.ParseAsync(request.InputStream);
            var root = doc.RootElement;
            if (root.TryGetProperty("time", out var timeProp))
            {
                time = ParseDouble(timeProp);
            }
            if (root.TryGetProperty("canvasWidth", out var widthProp))
            {
                canvasWidth = ParseDouble(widthProp);
            }
            if (root.TryGetProperty("canvasHeight", out var heightProp))
            {
                canvasHeight = ParseDouble(heightProp);
            }
            if (root.TryGetProperty("exp", out var expProp))
            {
                expressionOverride = expProp.GetString() ?? expressionOverride;
            }
            if (root.TryGetProperty("events", out var eventsProp))
            {
                events = ParseEventsElement(eventsProp);
            }
            if (root.TryGetProperty("resetState", out var resetProp))
            {
                resetState = resetProp.ValueKind == JsonValueKind.True;
            }
            if (root.TryGetProperty("svg", out var svgProp))
            {
                includeSvg = svgProp.ValueKind == JsonValueKind.True;
            }
        }

        return new EvaluationRequest(
            time,
            canvasWidth,
            canvasHeight,
            includeSvg,
            null,
            expressionOverride,
            events,
            resetState);
    }

    private static IReadOnlyList<object?>? ParseEventsFromQuery(System.Collections.Specialized.NameValueCollection query)
    {
        var events = new List<object?>();
        var singleEvents = query.GetValues("event");
        if (singleEvents != null)
        {
            foreach (var evt in singleEvents)
            {
                events.Add(evt);
            }
        }

        var eventsJson = query["events"];
        if (!string.IsNullOrWhiteSpace(eventsJson))
        {
            var parsed = JsonSerializer.Deserialize<object?>(eventsJson);
            AppendEvents(events, parsed);
        }

        return events.Count == 0 ? null : events;
    }

    private static IReadOnlyList<object?>? ParseEventsElement(JsonElement element)
    {
        object? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<object?>(element.GetRawText());
        }
        catch
        {
            return null;
        }

        var events = new List<object?>();
        AppendEvents(events, parsed);
        return events.Count == 0 ? null : events;
    }

    private static void AppendEvents(List<object?> target, object? payload)
    {
        if (payload == null)
        {
            return;
        }

        if (payload is IEnumerable enumerable && payload is not string)
        {
            foreach (var item in enumerable)
            {
                target.Add(item);
            }
            return;
        }

        target.Add(payload);
    }

    private void HandleEvents(HttpListenerContext context)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/event-stream";
        context.Response.SendChunked = true;
        context.Response.KeepAlive = true;
        context.Response.Headers["Cache-Control"] = "no-cache";
        WriteEvent(context.Response, null, null);

        lock (_sync)
        {
            _eventSinks.Add(context.Response);
        }
    }

    private static double? ParseDouble(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return null;
    }

    private static double? ParseDouble(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return ParseDouble(element.GetString());
        }

        return null;
    }

    private async Task WriteJsonAsync(HttpListenerResponse response, object payload)
    {
        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        using var buffer = new MemoryStream();
        await JsonSerializer.SerializeAsync(buffer, payload, _jsonOptions, _cts.Token);
        var bytes = buffer.ToArray();
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length, _cts.Token);
    }

    private static void WriteEvent(HttpListenerResponse response, string? eventName, string? data)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(eventName))
        {
            builder.Append("event: ").Append(eventName).Append('\n');
        }

        builder.Append("data: ").Append(data ?? "{}").Append("\n\n");
        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Flush();
    }

    private static void SafeClose(HttpListenerResponse response)
    {
        try
        {
            response.OutputStream.Close();
            response.Close();
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}

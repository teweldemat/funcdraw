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

internal sealed record SceneRequest(
    bool IncludeSvg,
    bool ResetState,
    double? Time,
    double? CanvasWidth,
    double? CanvasHeight,
    IReadOnlyList<object?>? Events);

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
    private readonly IDictionary<string, Func<object?>> _valueHooks;
    private readonly TraceOptions? _traceOptions;

    public SceneService(
        string projectRoot,
        string? expressionOverride = null,
        double? initialTime = null,
        IDictionary<string, Func<object?>>? valueHooks = null,
        TraceOptions? trace = null)
    {
        _projectRoot = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
        _resolver = new ArtResolver(_projectRoot);
        _expressionOverride = NormalizeExpressionOverride(expressionOverride);
        _timeline = initialTime ?? 0;
        _canvasWidth = 40;
        _canvasHeight = 30;
        _valueHooks = InitializeValueHooks(valueHooks);
        _traceOptions = trace;
    }

    public string WatchPath => _resolver.WatchPath;

    public ScenePayload Evaluate(
        bool includeSvg = false,
        double? time = null,
        double? canvasWidth = null,
        double? canvasHeight = null)
    {
        lock (_stateLock)
        {
            ApplyRequestOverrides(time, canvasWidth, canvasHeight);
            var result = EvaluateOnce(includeSvg);
            var plainState = _state == null ? null : new ValueConverter().ToPlain(_state);
            return new ScenePayload(result, _timeline, includeSvg, plainState);
        }
    }

    public ScenePayload PushEvent(
        IReadOnlyList<object?>? events,
        bool includeSvg = false,
        double? time = null,
        double? canvasWidth = null,
        double? canvasHeight = null)
    {
        lock (_stateLock)
        {
            ApplyRequestOverrides(time, canvasWidth, canvasHeight);
            var queue = new Queue<object?>(events ?? Array.Empty<object?>());
            if (queue.Count == 0)
            {
                return Evaluate(includeSvg);
            }

            var includeSvgThisEval = includeSvg && queue.Count == 0;
            var result = EvaluateOnce(includeSvgThisEval);
            while (queue.Count > 0)
            {
                var step = result.Raw.StepFunction;
                if (step == null)
                {
                    throw new InvalidOperationException("Stepper events were provided but the model did not return a step function.");
                }

                var stepResult = RunStep(step, queue.Dequeue());
                _state = stepResult.State;
                foreach (var evt in stepResult.Events)
                {
                    queue.Enqueue(evt);
                }

                includeSvgThisEval = includeSvg && queue.Count == 0;
                result = EvaluateOnce(includeSvgThisEval);
            }

            var plainState = _state == null ? null : new ValueConverter().ToPlain(_state);
            return new ScenePayload(result, _timeline, includeSvg, plainState);
        }
    }

    public void Reset()
    {
        lock (_stateLock)
        {
            _state = null;
            _timeline = 0;
        }
    }

    private SceneResult EvaluateOnce(bool includeSvg)
    {
        return FuncDrawRuntime.LoadGraphics(
            _resolver,
            new FuncDrawOptions
            {
                IncludeSvg = includeSvg,
                ValueHooks = _valueHooks,
                Trace = _traceOptions,
                ExpressionOverride = _expressionOverride,
                StateArg = _state
            });
    }

    public void Reload()
    {
        _resolver = new ArtResolver(_projectRoot);
        _timeline = 0;
        _state = null;
    }

    private IDictionary<string, Func<object?>> InitializeValueHooks(IDictionary<string, Func<object?>>? customHooks)
    {
        var hooks = customHooks != null
            ? new Dictionary<string, Func<object?>>(customHooks, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, Func<object?>>(StringComparer.OrdinalIgnoreCase);

        hooks["t"] = () => _timeline;
        hooks["canvas"] = CreateCanvasHook;
        return hooks;
    }

    private void ApplyRequestOverrides(double? time, double? canvasWidth, double? canvasHeight)
    {
        if (time.HasValue)
        {
            _timeline = time.Value;
        }

        if (canvasWidth.HasValue)
        {
            _canvasWidth = canvasWidth.Value;
        }

        if (canvasHeight.HasValue)
        {
            _canvasHeight = canvasHeight.Value;
        }
    }

    private object CreateCanvasHook()
    {
        var size = new SimpleKeyValueCollection(null, new[]
        {
            KeyValuePair.Create("width", (object)_canvasWidth),
            KeyValuePair.Create("height", (object)_canvasHeight)
        });
        return new SimpleKeyValueCollection(null, new[]
        {
            KeyValuePair.Create("size", (object)size)
        });
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

    private static StepResult RunStep(IFsFunction stepFunction, object? evt)
    {
        var args = new ArrayFsList(new[] { evt ?? (object?)null });
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
    public ScenePayload(SceneResult result, double timeline, bool includeSvg, object? state)
    {
        Graphics = result.Graphics;
        View = result.View;
        Warnings = result.Warnings;
        Raw = result.Raw;
        ValueHooks = result.ValueHooks;
        Svg = includeSvg ? result.Svg : null;
        Timeline = new Dictionary<string, object> { ["t"] = timeline };
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
        var request = await ParseSceneRequestAsync(context.Request);
        if (request.ResetState)
        {
            _service.Reset();
        }

        var payload = request.Events != null && request.Events.Count > 0
            ? _service.PushEvent(request.Events, request.IncludeSvg, request.Time, request.CanvasWidth, request.CanvasHeight)
            : _service.Evaluate(request.IncludeSvg, request.Time, request.CanvasWidth, request.CanvasHeight);
        await WriteJsonAsync(context.Response, payload);
        SafeClose(context.Response);
    }

    private async Task<SceneRequest> ParseSceneRequestAsync(HttpListenerRequest request)
    {
        var query = request.QueryString;
        var includeSvg = query["svg"] != null;
        var resetState = query["resetState"] != null;
        var time = ParseDouble(query["time"]);
        if (time == null)
        {
            time = ParseDouble(query["t"]);
        }
        var canvasWidth = ParseDouble(query["canvasWidth"]);
        var canvasHeight = ParseDouble(query["canvasHeight"]);
        var events = ParseEventsFromQuery(query);

        if (string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && request.HasEntityBody)
        {
            using var doc = await JsonDocument.ParseAsync(request.InputStream);
            var root = doc.RootElement;
            if (root.TryGetProperty("events", out var eventsProp))
            {
                events = ParseEventsElement(eventsProp);
            }
            if (root.TryGetProperty("time", out var timeProp))
            {
                time = ParseDouble(timeProp);
            }
            else if (root.TryGetProperty("t", out var tProp))
            {
                time = ParseDouble(tProp);
            }
            if (root.TryGetProperty("canvasWidth", out var widthProp))
            {
                canvasWidth = ParseDouble(widthProp);
            }
            if (root.TryGetProperty("canvasHeight", out var heightProp))
            {
                canvasHeight = ParseDouble(heightProp);
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

        return new SceneRequest(includeSvg, resetState, time, canvasWidth, canvasHeight, events);
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

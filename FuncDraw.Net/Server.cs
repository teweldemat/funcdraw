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

namespace FuncDraw.Net;

internal sealed record SceneRequest(
    bool IncludeSvg,
    bool ResetState,
    double? Time,
    double? CanvasWidth,
    double? CanvasHeight,
    IReadOnlyList<object?>? Events);

internal sealed class HookTracker
{
    private readonly Dictionary<string, HookEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public (string Name, Func<object> Hook) Register(string name, Func<object> factory)
    {
        var entry = new HookEntry(factory);
        _entries[name] = entry;
        return (name, entry.Invoke);
    }

    public void ResetUsage()
    {
        foreach (var entry in _entries.Values)
        {
            entry.Reset();
        }
    }

    public Dictionary<string, HookUsage> Summarize()
    {
        return _entries.ToDictionary(
            pair => pair.Key,
            pair => new HookUsage { Used = pair.Value.Used },
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed class HookEntry
    {
        private readonly Func<object> _factory;
        public bool Used { get; private set; }

        public HookEntry(Func<object> factory)
        {
            _factory = factory;
        }

        public object Invoke()
        {
            Used = true;
            return _factory();
        }

        public void Reset()
        {
            Used = false;
        }
    }
}

internal sealed class FuncDrawServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _html;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<HttpListenerResponse> _eventSinks = new();
    private readonly object _sync = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly object _stateLock = new();
    private readonly string _projectRoot;
    private readonly string? _expressionOverride;
    private readonly TraceOptions? _traceOptions;
    private readonly IDictionary<string, Func<object?>> _customHooks;
    private readonly HookTracker _hookTracker = new();
    private readonly List<Action<object>> _eventHooks = new();
    private readonly ValueConverter _converter = new();
    private readonly Func<string, object> _measureString;
    private ArtResolver _resolver;
    private FuncDrawEvalService _service;
    private double _timeline;
    private double _canvasWidth;
    private double _canvasHeight;

    private FuncDrawServer(string projectRoot, string host, int port, string html, string? expressionOverride, double? initialTime, TraceOptions? traceOptions)
    {
        _projectRoot = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
        _expressionOverride = NormalizeExpressionOverride(expressionOverride);
        _traceOptions = traceOptions;
        _customHooks = new Dictionary<string, Func<object?>>(StringComparer.OrdinalIgnoreCase);
        _timeline = initialTime ?? 0;
        _canvasWidth = 40;
        _canvasHeight = 30;
        _measureString = DefaultMeasureString;
        _resolver = new ArtResolver(_projectRoot);
        _service = CreateService();

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

    public string WatchPath => _resolver.WatchPath;

    public static async Task<FuncDrawServer> StartAsync(string projectRoot, string host, int port, string html, string? expressionOverride, double? initialTime, TraceOptions? traceOptions)
    {
        var server = new FuncDrawServer(projectRoot, host, port, html, expressionOverride, initialTime, traceOptions);
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

    public void Reload()
    {
        lock (_stateLock)
        {
            _resolver = new ArtResolver(_projectRoot);
            _service = CreateService();
            _timeline = 0;
        }
    }

    public void Reset()
    {
        lock (_stateLock)
        {
            _service.Reset();
            _timeline = 0;
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
        var requestId = $"http-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}";
        var request = await ParseSceneRequestAsync(context.Request);
        var ip = context.Request.RemoteEndPoint?.Address?.ToString() ?? "n/a";
        var eventCount = request.Events?.Count ?? 0;
        var start = DateTimeOffset.UtcNow;
        Console.WriteLine(
            $"[funcdraw.net] [{requestId}] {context.Request.HttpMethod} /__funcdraw/scene (svg={(request.IncludeSvg ? "yes" : "no")}, resetState={(request.ResetState ? "yes" : "no")}, events={eventCount}, ip={ip})"
        );
        if (eventCount > 0)
        {
            Console.WriteLine($"[funcdraw.net] [{requestId}] Incoming events payload: {JsonSerializer.Serialize(request.Events)}");
        }
        if (request.ResetState)
        {
            Reset();
        }

        ScenePayload? payload;
        if (request.Events != null && request.Events.Count > 0)
        {
            payload = PushEvents(request.Events, request.IncludeSvg, request.Time, request.CanvasWidth, request.CanvasHeight);
        }
        else
        {
            payload = Evaluate(request.IncludeSvg, request.Time, request.CanvasWidth, request.CanvasHeight);
        }

        var elapsed = (DateTimeOffset.UtcNow - start).TotalMilliseconds;
        var warningCount = payload == null ? 0 : payload.Warnings.Count;
        Console.WriteLine(
            $"[funcdraw.net] [{requestId}] Responding (ms={elapsed:0.0}, warnings={warningCount})"
        );
        await WriteJsonAsync(context.Response, payload);
        SafeClose(context.Response);
    }

    public ScenePayload Evaluate(bool includeSvg, double? time, double? canvasWidth, double? canvasHeight)
    {
        lock (_stateLock)
        {
            ApplyRequestOverrides(time, canvasWidth, canvasHeight);
            _hookTracker.ResetUsage();
            var result = _service.Evaluate(includeSvg);
            var plainState = _service.State == null ? null : _converter.ToPlain(_service.State);
            return new ScenePayload(result, _timeline, includeSvg, plainState, _hookTracker.Summarize());
        }
    }

    public void SetState(object? state)
    {
        lock (_stateLock)
        {
            _service.SetState(state);
        }
    }

    public ScenePayload? PushEvents(IReadOnlyList<object?> events, bool includeSvg, double? time, double? canvasWidth, double? canvasHeight)
    {
        lock (_stateLock)
        {
            ApplyRequestOverrides(time, canvasWidth, canvasHeight);
            _hookTracker.ResetUsage();
            // Prime the model to ensure a step function is available before processing events.
            _service.Evaluate(includeSvg && events.Count == 0);
            SceneResult? result = null;
            for (var i = 0; i < events.Count; i++)
            {
                var last = i == events.Count - 1;
                var pushed = _service.PushEvent(events[i], includeSvg && last);
                if (pushed != null)
                {
                    result = pushed;
                }
            }

            if (result == null)
            {
                return null;
            }
            var plainState = _service.State == null ? null : _converter.ToPlain(_service.State);
            return new ScenePayload(result, _timeline, includeSvg, plainState, _hookTracker.Summarize());
        }
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

    private FuncDrawEvalService CreateService()
    {
        var hooks = BuildHooks();
        return new FuncDrawEvalService(
            _resolver,
            _expressionOverride,
            hooks,
            _eventHooks,
            _measureString,
            traceOptions: _traceOptions);
    }

    private IEnumerable<(string Name, Func<object> Hook)> BuildHooks()
    {
        var hooks = new List<(string, Func<object>)>
        {
            _hookTracker.Register("t", () => _timeline),
            _hookTracker.Register("canvas", CreateCanvasHook)
        };

        foreach (var pair in _customHooks)
        {
            var normalizedName = pair.Key.Trim();
            hooks.Add(_hookTracker.Register(normalizedName, () => pair.Value!()!));
        }

        return hooks;
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

    private object DefaultMeasureString(string text)
    {
        var size = 12d;
        var length = text?.Length ?? 0;
        var width = length * size * 0.6;
        var lineHeight = size * 1.2;
        var ascent = size;
        var descent = lineHeight - ascent;
        var metrics = new Metrics(
            width,
            lineHeight,
            ascent,
            descent,
            ascent,
            size * 0.6);
        return new SimpleKeyValueCollection(null, metrics.ToDictionary());
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
            if (root.TryGetProperty("resetState", out var resetProp) && resetProp.ValueKind == JsonValueKind.True)
            {
                resetState = true;
            }
            if (root.TryGetProperty("svg", out var svgProp) && svgProp.ValueKind == JsonValueKind.True)
            {
                includeSvg = true;
            }
        }

        return new SceneRequest(includeSvg, resetState, time, canvasWidth, canvasHeight, events);
    }

    private static List<object?>? ParseEventsFromQuery(System.Collections.Specialized.NameValueCollection query)
    {
        var raw = query.GetValues("event");
        if (raw == null || raw.Length == 0)
        {
            return null;
        }

        var events = new List<object?>();
        foreach (var entry in raw)
        {
            events.Add(entry);
        }

        return events.Count == 0 ? null : events;
    }

    private static List<object?>? ParseEventsElement(JsonElement element)
    {
        var parsed = ToPlain(element);
        if (parsed == null)
        {
            return null;
        }
        if (parsed is List<object?> list)
        {
            return list.Count == 0 ? null : list;
        }
        return new List<object?> { parsed };
    }

    private static object? ToPlain(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
            {
                if (element.TryGetInt64(out var longVal))
                {
                    return longVal;
                }
                if (element.TryGetDouble(out var doubleVal))
                {
                    return doubleVal;
                }
                return null;
            }
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ToPlain(item));
                }
                return list;
            }
            case JsonValueKind.Object:
            {
                var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in element.EnumerateObject())
                {
                    map[prop.Name] = ToPlain(prop.Value);
                }
                return map;
            }
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            default:
                return null;
        }
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

    private async Task WriteJsonAsync(HttpListenerResponse response, object? payload)
    {
        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
    }

    private void WriteEvent(HttpListenerResponse response, string? eventName, string? data)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(eventName))
        {
            sb.Append("event: ").Append(eventName).Append("\n");
        }

        if (!string.IsNullOrEmpty(data))
        {
            sb.Append("data: ").Append(data.Replace("\n", "\\n")).Append("\n");
        }

        sb.Append("\n");
        var payload = Encoding.UTF8.GetBytes(sb.ToString());
        response.OutputStream.Write(payload, 0, payload.Length);
        response.OutputStream.Flush();
    }

    private static void SafeClose(HttpListenerResponse response)
    {
        try
        {
            response.OutputStream.Dispose();
            response.Close();
        }
        catch
        {
            // ignore close errors
        }
    }
}

internal sealed class ScenePayload
{
    public ScenePayload(SceneResult result, double timeline, bool includeSvg, object? state, Dictionary<string, HookUsage>? valueHooks = null)
    {
        Graphics = result.Graphics;
        View = result.View;
        Warnings = result.Warnings;
        Raw = result.Raw;
        ValueHooks = valueHooks ?? result.ValueHooks;
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

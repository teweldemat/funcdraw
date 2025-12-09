using System;
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

namespace FuncDraw.Net;

internal sealed record EvaluationRequest(
    double? Time,
    double? CanvasWidth,
    double? CanvasHeight,
    bool IncludeSvg,
    TraceOptions? Trace,
    string? ExpressionOverride = null);

internal sealed class SceneService
{
    private readonly string _projectRoot;
    private ArtResolver _resolver;
    private readonly string? _expressionOverride;
    private double _timeline;
    private double _canvasWidth;
    private double _canvasHeight;

    public SceneService(string projectRoot, string? expressionOverride = null)
    {
        _projectRoot = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
        _resolver = new ArtResolver(_projectRoot);
        _expressionOverride = NormalizeExpressionOverride(expressionOverride);
        _timeline = 0;
        _canvasWidth = 40;
        _canvasHeight = 30;
    }

    public string WatchPath => _resolver.WatchPath;

    public ScenePayload Evaluate(EvaluationRequest request)
    {
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

        var hooks = new Dictionary<string, Func<object>>
        {
            ["t"] = () => _timeline,
            ["canvas"] = () => BuildCanvasValue()
        };

        var result = FuncDrawRuntime.LoadGraphics(
            _resolver,
            new FuncDrawOptions
            {
                IncludeSvg = request.IncludeSvg,
                ValueHooks = hooks,
                Trace = request.Trace,
                ExpressionOverride = string.IsNullOrWhiteSpace(request.ExpressionOverride)
                    ? _expressionOverride
                    : request.ExpressionOverride
            });

        return new ScenePayload(result, _timeline, _canvasWidth, _canvasHeight, request.IncludeSvg);
    }

    public void Reload()
    {
        _resolver = new ArtResolver(_projectRoot);
        _timeline = 0;
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

}

internal sealed class ScenePayload
{
    public ScenePayload(SceneResult result, double timeline, double canvasWidth, double canvasHeight, bool includeSvg)
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
        var query = context.Request.QueryString;
        var includeSvg = query["svg"] != null;
        var request = new EvaluationRequest(
            ParseDouble(query["time"]),
            ParseDouble(query["canvasWidth"]),
            ParseDouble(query["canvasHeight"]),
            includeSvg,
            null);

        var payload = _service.Evaluate(request);
        await WriteJsonAsync(context.Response, payload);
        SafeClose(context.Response);
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

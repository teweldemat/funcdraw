using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using FuncDraw.Net;

var options = CliParser.Parse(args);
var root = Path.GetFullPath(options.Root ?? Environment.CurrentDirectory);
if (options.Test)
{
    Environment.ExitCode = PackageTestCli.Run(root, options.ExpressionOverride);
    return;
}

var traceRequested = (options.Trace != null && options.Trace.Enabled) || !string.IsNullOrWhiteSpace(options.TraceFile);
var traceOptions = traceRequested ? options.Trace ?? new TraceOptions { Enabled = true, StepInto = false, Filter = null } : null;
var traceOutputPath = ResolveTracePath(options.TraceFile, root);
var traceOnly = traceRequested && !options.Dump;
var initialState = ParseJsonArgument(options.StateJson);
var eventPayload = ParseJsonArgument(options.EventJson);

if (options.Dump)
{
    using var dumpServer = await FuncDrawServer.CreateHeadlessAsync(root, options.Host, options.Port, HtmlTemplate.Content, options.ExpressionOverride, options.Time, traceOptions);
    if (initialState != null)
    {
        dumpServer.SetState(initialState);
    }
    ScenePayload? payload = eventPayload != null
        ? dumpServer.PushEvents(new object?[] { eventPayload }, options.IncludeSvg, options.Time, null, null)
        : dumpServer.Evaluate(options.IncludeSvg, options.Time, null, null);
    WriteTraceToFile(payload?.Trace, traceOutputPath, root);
    if (traceRequested)
    {
        PrintTraceEntries(payload?.Trace);
    }
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    Console.WriteLine(JsonSerializer.Serialize(payload, jsonOptions));
    return;
}

if (traceOnly)
{
    using var traceServer = await FuncDrawServer.CreateHeadlessAsync(root, options.Host, options.Port, HtmlTemplate.Content, options.ExpressionOverride, options.Time, traceOptions);
    if (initialState != null)
    {
        traceServer.SetState(initialState);
    }
    ScenePayload? payload = eventPayload != null
        ? traceServer.PushEvents(new object?[] { eventPayload }, options.IncludeSvg, options.Time, null, null)
        : traceServer.Evaluate(options.IncludeSvg, options.Time, null, null);
    WriteTraceToFile(payload?.Trace, traceOutputPath, root);
    PrintTraceEntries(payload?.Trace);
    return;
}

var serverPort = options.Port;
FuncDrawServer server;
try
{
    server = await FuncDrawServer.StartAsync(root, options.Host, serverPort, HtmlTemplate.Content, options.ExpressionOverride, options.Time, traceOptions);
}
catch (HttpListenerException ex) when (IsAddressInUse(ex))
{
    serverPort = FindAvailablePort(options.Host);
    Console.WriteLine($"[funcdraw.net] Port {options.Port} already in use; falling back to {serverPort}.");
    server = await FuncDrawServer.StartAsync(root, options.Host, serverPort, HtmlTemplate.Content, options.ExpressionOverride, options.Time, traceOptions);
}

using var serverHandle = server;
using var watcher = WatchArt(serverHandle);

Console.WriteLine($"FuncDraw.Net ready at http://{(options.Host == "0.0.0.0" ? "localhost" : options.Host)}:{serverPort}");
Console.CancelKeyPress += (_, __) =>
{
    watcher.Dispose();
    serverHandle.Dispose();
    Environment.Exit(0);
};

await Task.Delay(Timeout.Infinite);

static IDisposable WatchArt(FuncDrawServer server)
{
    var watcher = new FileSystemWatcher(server.WatchPath)
    {
        IncludeSubdirectories = true,
        EnableRaisingEvents = true
    };
    var timer = new System.Threading.Timer(_ =>
    {
        server.Reload();
        server.BroadcastReload();
    });

    void Schedule()
    {
        timer.Change(200, Timeout.Infinite);
    }

    FileSystemEventHandler handler = (_, __) => Schedule();
    RenamedEventHandler renamedHandler = (_, __) => Schedule();
    watcher.Changed += handler;
    watcher.Created += handler;
    watcher.Deleted += handler;
    watcher.Renamed += renamedHandler;

    return new CompositeDisposable(new IDisposable[]
    {
        watcher,
        timer,
        new CallbackDisposable(() =>
        {
            watcher.Changed -= handler;
            watcher.Created -= handler;
            watcher.Deleted -= handler;
            watcher.Renamed -= renamedHandler;
        })
    });
}

static string? ResolveTracePath(string? traceFile, string root)
{
    if (string.IsNullOrWhiteSpace(traceFile))
    {
        return null;
    }

    return Path.IsPathRooted(traceFile) ? traceFile : Path.Combine(root, traceFile);
}

static bool IsAddressInUse(HttpListenerException ex)
{
    return ex.ErrorCode is 48 or 98;
}

static int FindAvailablePort(string host)
{
    var address = host == "0.0.0.0"
        ? IPAddress.Any
        : string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? IPAddress.Loopback
            : IPAddress.Parse(host);
    var listener = new TcpListener(address, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static void WriteTraceToFile(List<TraceEntry>? entries, string? targetPath, string root)
{
    if (string.IsNullOrWhiteSpace(targetPath))
    {
        return;
    }

    var directory = Path.GetDirectoryName(targetPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    var payload = entries ?? new List<TraceEntry>();
    File.WriteAllText(targetPath, JsonSerializer.Serialize(payload, options));
    var relative = Path.GetRelativePath(root, targetPath);
    var displayPath = string.IsNullOrWhiteSpace(relative) ? targetPath : relative;
    Console.WriteLine($"[funcdraw.net] Trace written to {displayPath}");
}

static void PrintTraceEntries(IReadOnlyCollection<TraceEntry>? entries)
{
    if (entries == null || entries.Count == 0)
    {
        Console.WriteLine("[funcdraw.net] No FuncScript trace entries recorded.");
        return;
    }

    var nodeCount = CountTraceNodes(entries);
    Console.WriteLine($"[funcdraw.net] FuncScript trace ({nodeCount} entr{(nodeCount == 1 ? "y" : "ies")})");
    foreach (var entry in entries)
    {
        PrintTraceNode(entry, 0);
    }
}

static void PrintTraceNode(TraceEntry entry, int depth)
{
    var indent = new string(' ', depth * 2);
    var pathText = string.IsNullOrWhiteSpace(entry.Path) ? "(root)" : entry.Path;
    var location = FormatTraceLocation(entry);
    var snippet = CleanSnippet(entry.Snippet);
    var line = $"{indent}- {pathText}";
    if (!string.IsNullOrEmpty(location))
    {
        line += $" {location}";
    }
    if (!string.IsNullOrEmpty(snippet))
    {
        line += $" {snippet}";
    }
    Console.WriteLine(line);
    var resultText = FormatTraceResult(entry);
    if (!string.IsNullOrEmpty(resultText))
    {
        Console.WriteLine($"{indent}  value: {resultText}");
    }
    if (entry.Children != null)
    {
        foreach (var child in entry.Children)
        {
            PrintTraceNode(child, depth + 1);
        }
    }
}

static int CountTraceNodes(IEnumerable<TraceEntry>? entries)
{
    if (entries == null)
    {
        return 0;
    }

    var count = 0;
    foreach (var entry in entries)
    {
        count += 1;
        count += CountTraceNodes(entry.Children);
    }
    return count;
}

static string FormatTraceLocation(TraceEntry entry)
{
    if (entry.StartLine.HasValue && entry.StartColumn.HasValue)
    {
        if (entry.EndLine.HasValue && entry.EndColumn.HasValue)
        {
            return $"@{entry.StartLine}:{entry.StartColumn}-{entry.EndLine}:{entry.EndColumn}";
        }

        return $"@{entry.StartLine}:{entry.StartColumn}";
    }

    return string.Empty;
}

static string CleanSnippet(string? snippet)
{
    if (string.IsNullOrWhiteSpace(snippet))
    {
        return string.Empty;
    }

    var compact = string.Join(' ', snippet.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    if (compact.Length > 160)
    {
        return $"{compact[..157]}...";
    }

    return compact;
}

static string FormatTraceResult(TraceEntry entry)
{
    if (string.IsNullOrEmpty(entry.ResultKind))
    {
        return string.Empty;
    }

    if (!string.IsNullOrEmpty(entry.ResultPreview))
    {
        return entry.ResultPreview!;
    }

    return entry.ResultKind switch
    {
        "atomic" => "(atomic)",
        "error" => "error",
        "function" => "<function>",
        "list" => "<list>",
        "kvc" => "<kvc>",
        "object" => "<object>",
        _ => $"<{entry.ResultKind}>"
    };
}

static object? ParseJsonArgument(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return null;
    }

    using var doc = JsonDocument.Parse(raw);
    return ToPlain(doc.RootElement);
}

static object? ToPlain(JsonElement element)
{
    switch (element.ValueKind)
    {
        case JsonValueKind.Null:
        case JsonValueKind.Undefined:
            return null;
        case JsonValueKind.False:
            return false;
        case JsonValueKind.True:
            return true;
        case JsonValueKind.Number:
            if (element.TryGetInt64(out var longVal))
            {
                return longVal;
            }
            if (element.TryGetDouble(out var doubleVal))
            {
                return doubleVal;
            }
            return null;
        case JsonValueKind.String:
            return element.GetString();
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
        default:
            return null;
    }
}

internal sealed record CliOptions(
    string Host,
    int Port,
    bool Dump,
    bool IncludeSvg,
    bool Test,
    string Root,
    TraceOptions? Trace,
    string? TraceFile,
    string? ExpressionOverride,
    double? Time,
    string? StateJson,
    string? EventJson);

internal static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        var host = "127.0.0.1";
        var port = 5177;
        var dump = false;
        var includeSvg = false;
        var test = false;
        var root = Environment.CurrentDirectory;
        TraceOptions? trace = null;
        string? traceFile = null;
        string? expressionOverride = null;
        double? time = null;
        string? stateJson = null;
        string? eventJson = null;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "--host":
                    host = RequireNext(args, ref i, "--host");
                    break;
                case "--port":
                    var rawPort = RequireNext(args, ref i, "--port");
                    if (!int.TryParse(rawPort, out port))
                    {
                        throw new ArgumentException("Port must be a valid integer.");
                    }
                    break;
                case "--dump":
                    dump = true;
                    break;
                case "--svg":
                    includeSvg = true;
                    break;
                case "--test":
                    test = true;
                    break;
                case "--root":
                    root = RequireNext(args, ref i, "--root");
                    break;
                case "--exp":
                    expressionOverride = RequireNext(args, ref i, "--exp");
                    break;
                case "--trace":
                    trace = ParseTraceOption(args, ref i);
                    break;
                case "--trace-file":
                    traceFile = RequireNext(args, ref i, "--trace-file");
                    break;
                case "--t":
                case "--time":
                    time = ParseDouble(RequireNext(args, ref i, current));
                    break;
                case "--state":
                    stateJson = RequireNext(args, ref i, "--state");
                    break;
                case "--event":
                    eventJson = RequireNext(args, ref i, "--event");
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{current}'.");
            }
        }

        return new CliOptions(host, port, dump, includeSvg, test, root, trace, traceFile, expressionOverride, time, stateJson, eventJson);
    }

    private static string RequireNext(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        index += 1;
        return args[index];
    }

    private static TraceOptions ParseTraceOption(string[] args, ref int index)
    {
        var stepInto = false;
        string? filter = null;

        if (index + 1 < args.Length && !IsOption(args[index + 1]))
        {
            var next = args[index + 1];
            if (string.Equals(next, "step-into", StringComparison.OrdinalIgnoreCase))
            {
                stepInto = true;
                if (index + 2 < args.Length && !IsOption(args[index + 2]))
                {
                    filter = args[index + 2];
                    index += 2;
                }
                else
                {
                    index += 1;
                }
            }
            else
            {
                filter = next;
                index += 1;
            }
        }

        return new TraceOptions
        {
            Enabled = true,
            StepInto = stepInto,
            Filter = filter
        };
    }

    private static double ParseDouble(string raw)
    {
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Invalid numeric value '{raw}'.");
    }

    private static bool IsOption(string value)
    {
        return value.StartsWith("--", StringComparison.Ordinal);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("FuncDraw.Net");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  funcdraw.net [--host <host>] [--port <port>] [--root <path>] [--dump] [--test] [--svg] [--trace [step-into [filter]]] [--trace-file <path>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --host     Host interface to bind (default 127.0.0.1)");
        Console.WriteLine("  --port     Port for the web server (default 5177)");
        Console.WriteLine("  --root     Project root containing art/ (default current directory)");
        Console.WriteLine("  --dump     Evaluate once and print the payload to stdout");
        Console.WriteLine("  --test     Run FuncScript package tests and exit (no server)");
        Console.WriteLine("  --svg      Include SVG output when dumping");
        Console.WriteLine("  --trace    Emit FuncScript trace output; optionally pass 'step-into' and a substring filter");
        Console.WriteLine("  --trace-file  Write FuncScript trace output to the given JSON file");
        Console.WriteLine("  --help     Show this help text");
    }
}

internal sealed class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _items;
    private bool _disposed;

    public CompositeDisposable(IEnumerable<IDisposable> items)
    {
        _items = new List<IDisposable>(items ?? Array.Empty<IDisposable>());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var item in _items)
        {
            try
            {
                item.Dispose();
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }
}

internal sealed class CallbackDisposable : IDisposable
{
    private readonly Action _callback;
    private bool _disposed;

    public CallbackDisposable(Action callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _callback();
    }
}

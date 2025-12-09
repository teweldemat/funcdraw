using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using FuncDraw.Net;

var options = CliParser.Parse(args);
var root = Path.GetFullPath(options.Root ?? Environment.CurrentDirectory);
var service = new SceneService(root);

if (options.Dump)
{
    var payload = service.Evaluate(new EvaluationRequest(null, null, null, options.IncludeSvg));
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    Console.WriteLine(JsonSerializer.Serialize(payload, jsonOptions));
    return;
}

using var server = await FuncDrawServer.StartAsync(service, options.Host, options.Port, HtmlTemplate.Content);
using var watcher = WatchArt(service, server);

Console.WriteLine($"FuncDraw.Net ready at http://{(options.Host == "0.0.0.0" ? "localhost" : options.Host)}:{options.Port}");
Console.CancelKeyPress += (_, __) =>
{
    watcher.Dispose();
    server.Dispose();
    Environment.Exit(0);
};

await Task.Delay(Timeout.Infinite);

static IDisposable WatchArt(SceneService service, FuncDrawServer server)
{
    var watcher = new FileSystemWatcher(service.WatchPath)
    {
        IncludeSubdirectories = true,
        EnableRaisingEvents = true
    };
    var timer = new System.Threading.Timer(_ =>
    {
        service.Reload();
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

internal sealed record CliOptions(string Host, int Port, bool Dump, bool IncludeSvg, string Root);

internal static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        var host = "127.0.0.1";
        var port = 5177;
        var dump = false;
        var includeSvg = false;
        var root = Environment.CurrentDirectory;

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
                case "--root":
                    root = RequireNext(args, ref i, "--root");
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

        return new CliOptions(host, port, dump, includeSvg, root);
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

    private static void PrintHelp()
    {
        Console.WriteLine("FuncDraw.Net");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  funcdraw.net [--host <host>] [--port <port>] [--root <path>] [--dump] [--svg]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --host     Host interface to bind (default 127.0.0.1)");
        Console.WriteLine("  --port     Port for the web server (default 5177)");
        Console.WriteLine("  --root     Project root containing art/ (default current directory)");
        Console.WriteLine("  --dump     Evaluate once and print the payload to stdout");
        Console.WriteLine("  --svg      Include SVG output when dumping");
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

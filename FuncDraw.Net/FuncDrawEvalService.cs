using System.Collections;
using System.Threading;
using FuncScript;
using FuncScript.Core;
using FuncScript.Model;
using FuncScript.Package;

namespace FuncDraw.Net;

internal class FuncDrawEvalService(IFsPackageResolver package,string?artExpression,
    IEnumerable<(string Name,Func<object> Hook)> hooks, 
    IEnumerable<Action<object>> eventHooks, 
    PackageLoader.PackageLoaderTraceDelegate? exitTrace=null,
    PackageLoader.PackageLoaderEntryTraceDelegate? entryTrace=null,
    TraceOptions? traceOptions=null)
{
    private static long _globalEvaluationCount = 0;
    private static long _globalStepCallCount = 0;
    private static readonly object MeasureStringHook = Engine.NormalizeDataType(new Func<object, object>(MeasureString));
    private const string MEASURE_STRING_FUNCTION_NAME = "measurestring";
    private const string FD_CONTEXT_NAME = "fd";
    private readonly KeyValueCollection _fdContext = FdContext.Create();
    private IEnumerable<(string Name, Func< object> Hook)> Hooks=>hooks;
    private static object MeasureStringFunction => MeasureStringHook;
    private PackageLoader.PackageLoaderTraceDelegate? ExitTrace => exitTrace;
    private PackageLoader.PackageLoaderEntryTraceDelegate? EntryTrace => entryTrace;
    private TraceOptions? TraceOptions => traceOptions;
    
    private static object MeasureString(object rawText)
    {
        var text = rawText?.ToString() ?? string.Empty;
        var metrics = FontEngine.Default.MeasureText(text, 12d, null);
        return new SimpleKeyValueCollection(null, metrics.ToDictionary());
    }
    class FuncDrawProvider(FuncDrawEvalService service,KeyValueCollection parent) : KeyValueCollection
    {
        private readonly KeyValueCollection _parent = parent;
        public virtual object Get(string key)
        {
            var lowerKey = key.ToLower();
            if (lowerKey == MEASURE_STRING_FUNCTION_NAME)
                return FuncDrawEvalService.MeasureStringFunction;
            if (lowerKey == FD_CONTEXT_NAME)
                return service._fdContext;
            var h = service.Hooks.FirstOrDefault(x => x.Name.ToLower().Equals(lowerKey));
            if (h.Hook != null)
                return Engine.NormalizeDataType(h.Hook());
            return _parent.Get(key);
        }

        public virtual bool IsDefined(string key, bool hierarchy = true)
        {
            var lowerKey = key.ToLower();

            if (lowerKey == MEASURE_STRING_FUNCTION_NAME)
                return true;
            if (lowerKey == FD_CONTEXT_NAME)
                return true;
            if (service.Hooks.Any(x => x.Name.ToLower().Equals(lowerKey)))
                return true;
            if (hierarchy)
                return _parent.IsDefined(key);
            return false;
        }

        public IList<KeyValuePair<string, object>> GetAll()
        {
            return this.GetAllKeys().Select(k => KeyValuePair.Create(k, this.Get(k))).ToList();
        }

        public virtual IList<string> GetAllKeys()
        {
            return new[] { MEASURE_STRING_FUNCTION_NAME, FD_CONTEXT_NAME }.Concat(service.Hooks.Select(x => x.Name)).ToList();
        }

        public KeyValueCollection ParentProvider => _parent;
    }

    class FuncDrawArtProvider(FuncDrawEvalService service,IFsPackageResolver package,FuncDrawProvider provider):KeyValueCollection
    {
        private readonly FuncDrawProvider _provider = provider;
        public object Get(string key)
        {
            if (key.ToLower() == "art")
            {
                var baseProvider = new FuncDrawProvider(service, new DefaultFsDataProvider());
                return FuncScript.Package.PackageLoader.LoadPackage(package, baseProvider, service.ExitTrace,
                    service.EntryTrace);
            }
            return _provider.Get(key);
        }

        public bool IsDefined(string key, bool hierarchy = true)
        {
            if (key.ToLower() == "art")
                return true;
            if (hierarchy)
                return _provider.IsDefined(key);
            return false;
        }

        public IList<KeyValuePair<string, object>> GetAll()
        {
            return this.GetAllKeys().Select(k => KeyValuePair.Create(k, Get(k))).ToList();
        }

        public IList<string> GetAllKeys()
        {
            return new string[] { "art" };
        }

        public KeyValueCollection ParentProvider => _provider;
    }

    private object? _state = null;
    private IFsFunction? _stepFunction = null;
    private bool _hasEvaluated = false;
    internal object? State => _state;
    internal bool HasStepFunction => _stepFunction != null;
    internal bool HasEvaluated => _hasEvaluated;
    
    internal void Reset()
    {
        _state = null;
        _stepFunction = null;
        _hasEvaluated = false;
    }

    internal void SetState(object? state)
    {
        _state = NormalizeFsValue(state);
        _stepFunction = null;
        _hasEvaluated = false;
    }
    
    
    internal SceneResult Evaluate(bool includeSvg=false)
    {
        var evaluationNumber = Interlocked.Increment(ref _globalEvaluationCount);
        Console.WriteLine($"[funcdraw.net] Eval #{evaluationNumber}");
        var converter = new ValueConverter();
        var traceCollector = TraceCollector.Create(TraceOptions, converter);
        var baseProvider = new FuncDrawProvider(this, new DefaultFsDataProvider());
        object typedRoot;

        if (!string.IsNullOrWhiteSpace(artExpression))
        {
            
            typedRoot = Engine.Evaluate(new FuncDrawArtProvider(this,package,baseProvider),artExpression);
        }
        else
        {
            typedRoot = traceCollector!=null
                ? PackageLoader.LoadPackage(package,baseProvider,traceCollector.ExitHook,traceCollector.EntryHook)
                : PackageLoader.LoadPackage(package,baseProvider,exitTrace,entryTrace);
        }

        if (typedRoot is IFsFunction func)
        {
            typedRoot = func.Evaluate(new ArrayFsList(new[] { _state }));
        }
        if (typedRoot is KeyValueCollection kvc)
        {
            _stepFunction = kvc.Get("step") as IFsFunction;
        }
        else
            _stepFunction=null;
        _hasEvaluated = true;
        return InterprateGraphics(typedRoot, includeSvg, converter, traceCollector);
    }

    internal SceneResult? PushEvent(object? e,bool includeSvg=false)
    {
        var queue = new Queue<object?>();
        queue.Enqueue(e);
        SceneResult? res=null;
        while (queue.Count>0 && _stepFunction!=null)
        {
            var q = queue.Dequeue();
            var normalizedEvent = NormalizeFsValue(q);
            foreach (var hook in eventHooks)
            {
                hook(normalizedEvent!);
            }
            var stepNumber = Interlocked.Increment(ref _globalStepCallCount);
            Console.WriteLine($"[funcdraw.net] Step #{stepNumber}");
            var s=_stepFunction.Evaluate(new ArrayFsList(new[] { normalizedEvent }));
            if (s == null)
            {
                continue;
            }
            object? events = null;
            if (s is KeyValueCollection kvc)
            {
                var nextState = kvc.Get("state");
                events = kvc.Get("events");
                if (nextState == null)
                {
                    if (events != null)
                        throw new InvalidOperationException("Next state can't be null");
                    _state = s;
                }
                else
                {
                    _state = nextState;
                }
            }
            else
            {
                _state = s;
                events = null;
            }

            if (events != null)
            {
                if(events is FsList list)
                    foreach (var ev in list)
                    {
                        queue.Enqueue(ev);
                    }
                else
                {
                    queue.Enqueue(events);
                }
            }
            var last = queue.Count == 0;
            res=Evaluate(includeSvg && last);
        }
        if (res != null && includeSvg && res.Svg == null)
        {
            res = Evaluate(true);
        }
        return res;
    }
        
    SceneResult InterprateGraphics(object value,bool includeSvg,ValueConverter converter,TraceCollector? traceCollector)
    {
        var interpretation = GraphicsInterpreter.Interpret(value, converter);
        TextToGlyphConverter.Convert(interpretation);
        var svg = includeSvg ? SvgRenderer.Render(interpretation) : null;
        return new SceneResult(
            interpretation.Graphics,
            interpretation.View,
            interpretation.Warnings,
            interpretation,
            null,
            svg,
            traceCollector?.Export());
    }

    private static object? NormalizeFsValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is IDictionary<string, object?> dict)
        {
            var entries = dict
                .Select(pair => KeyValuePair.Create(pair.Key, NormalizeFsValue(pair.Value) ?? (object?)null))
                .ToArray();
            return new SimpleKeyValueCollection(null, entries);
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(NormalizeFsValue(item));
            }
            return new ArrayFsList(list.ToArray());
        }

        return Engine.NormalizeDataType(value);
    }

}

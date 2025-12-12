using System.Runtime.CompilerServices;
using FuncScript;
using FuncScript.Core;
using FuncScript.Model;
using FuncScript.Package;

namespace FuncDraw.Net;

public class FuncDrawEvalService(IFsPackageResolver package,string?artExpression,
    IEnumerable<(string Name,Func<object> Hook)> hooks, 
    IEnumerable<Action<object>> eventHooks, 
    Func<string,object> measureStringFunction,
    PackageLoader.PackageLoaderTraceDelegate? exitTrace=null,
    PackageLoader.PackageLoaderEntryTraceDelegate? entryTrace=null)
{
    private const string MEASURE_STRING_FUNCTION_NAME = "measurestring";
    private IEnumerable<(string Name, Func< object> Hook)> Hooks=>hooks;
    private object MeasureStringFunction => FuncScript.Engine.NormalizeDataType(measureStringFunction);
    class FuncDrawProvider(FuncDrawEvalService service,KeyValueCollection parent) : KeyValueCollection
    {
        public object Get(string key)
        {
            var lowerKey = key.ToLower();
            if (lowerKey == MEASURE_STRING_FUNCTION_NAME)
                return service.MeasureStringFunction;
            var h = service.Hooks.FirstOrDefault(x => x.Name.ToLower().Equals(lowerKey));
            if (h.Hook != null)
                return Engine.NormalizeDataType(h.Hook());
            return parent.Get(key);
        }

        public bool IsDefined(string key, bool hierarchy = true)
        {
            var lowerKey = key.ToLower();

            if (lowerKey == MEASURE_STRING_FUNCTION_NAME)
                return true;
            if (service.Hooks.Any(x => x.Name.ToLower().Equals(lowerKey)))
                return true;
            if (hierarchy)
                return parent.IsDefined(key);
            return false;
        }

        public IList<KeyValuePair<string, object>> GetAll()
        {
            return this.GetAllKeys().Select(k => KeyValuePair.Create(k, this.Get(k))).ToList();
        }

        public IList<string> GetAllKeys()
        {
            return new[] { MEASURE_STRING_FUNCTION_NAME }.Concat(service.Hooks.Select(x => x.Name)).ToList();
        }

        public KeyValueCollection ParentProvider => parent;
    }

    private object? _state = null;
    private IFsFunction? _stepFunction = null;
    
    public object Evaluate()
    {
        var value = FuncScript.Package.PackageLoader.LoadPackage(package,
            new FuncDrawProvider(this, new DefaultFsDataProvider()), exitTrace, entryTrace);
        if (value is IFsFunction func)
        {
            value = func.Evaluate(new ArrayFsList(new[] { _state }));
        }
        if (value is KeyValueCollection kvc)
        {
            _stepFunction= kvc.Get("step") as IFsFunction;
        }
        return InterprateGraphics(value);
    }

    public void PushEvent(object e)
    {
        var queue = new Queue<object>();
        queue.Enqueue(e);
        while (queue.Count>0 && _stepFunction!=null)
        {
            var q = queue.Dequeue();
            foreach (var hook in eventHooks)
            {
                hook(q);
            }
            var s=_stepFunction.Evaluate(new ArrayFsList(new[] { e }));
            object nextState;
            object events;
            if (s is KeyValueCollection kvc)
            {
                nextState = kvc.Get("state");
                events = kvc.Get("events");
                if (nextState == null)
                {
                    _state = s;
                }
                else
                {
                    _state = nextState;
                }
            }
            else
            {
                nextState = s;
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
        }
    }
        
    object InterprateGraphics(object value)
    {
        throw new NotImplementedException();
    }

}
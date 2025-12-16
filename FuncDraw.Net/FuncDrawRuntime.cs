using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FuncScript;
using FuncScript.Error;
using FuncScript.Model;
using FuncScript.Package;
using FuncScript.Core;

namespace FuncDraw.Net;

internal sealed class FuncDrawOptions
{
    public bool IncludeSvg { get; init; }
    public IDictionary<string, Func<object?>>? ValueHooks { get; init; }
    public TraceOptions? Trace { get; init; }
    public string? ExpressionOverride { get; init; }
    public object? StateArg { get; init; }
}

internal sealed class TraceOptions
{
    public bool Enabled { get; init; } = true;
    public bool StepInto { get; init; }
    public string? Filter { get; init; }
}

internal sealed class TraceEntry
{
    public string? Path { get; set; }
    public int? StartLine { get; set; }
    public int? StartColumn { get; set; }
    public int? EndLine { get; set; }
    public int? EndColumn { get; set; }
    public int? StartIndex { get; set; }
    public int? EndIndex { get; set; }
    public string? Snippet { get; set; }
    public string? ResultKind { get; set; }
    public string? ResultPreview { get; set; }
    public List<TraceEntry> Children { get; set; } = new();
}

internal sealed class SceneResult
{
    public SceneResult(
        List<object> graphics,
        object? view,
        List<string> warnings,
        SceneInterpretation raw,
        Dictionary<string, HookUsage>? valueHooks,
        string? svg,
        List<TraceEntry>? trace)
    {
        Graphics = graphics;
        View = view;
        Warnings = warnings;
        Raw = raw;
        ValueHooks = valueHooks;
        Svg = svg;
        Trace = trace;
    }

    public List<object> Graphics { get; }
    public object? View { get; }
    public List<string> Warnings { get; }
    public SceneInterpretation Raw { get; }
    public Dictionary<string, HookUsage>? ValueHooks { get; }
    public string? Svg { get; }
    public List<TraceEntry>? Trace { get; }
}

internal sealed class HookUsage
{
    public bool Used { get; init; }
}

internal static class FuncDrawRuntime
{
    public static SceneResult LoadGraphics(IFsPackageResolver resolver, FuncDrawOptions options)
    {
        if (resolver == null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        var hooks = ValueHookSet.Create(options.ValueHooks);
        var fdContext = FdContext.Create();
        var baseProvider = new DefaultFsDataProvider();
        var provider = new FuncDrawProvider(fdContext, hooks, baseProvider);
        var converter = new ValueConverter();
        var traceCollector = TraceCollector.Create(options.Trace, converter);
        var baseRoot = traceCollector != null
            ? PackageLoader.LoadPackage(resolver, provider, traceCollector.ExitHook, traceCollector.EntryHook)
            : PackageLoader.LoadPackage(resolver, provider, null, null);
        var typedRoot = baseRoot;

        if (!string.IsNullOrWhiteSpace(options.ExpressionOverride))
        {
            var bindings = new SimpleKeyValueCollection(null, new[]
            {
                KeyValuePair.Create("art", Engine.NormalizeDataType(baseRoot))
            });
            var overrideProvider = new KvcProvider(bindings, provider);
            var overrideResult = Engine.Evaluate(overrideProvider, options.ExpressionOverride);
            typedRoot = overrideResult ?? new FsError(FsError.ERROR_TYPE_MISMATCH, "Expression override returned null");
        }

        if (typedRoot is IFsFunction func)
        {
            var args = new ArrayFsList(new[] { options.StateArg ?? (object?)null });
            typedRoot = func.Evaluate(args);
        }

        var interpretation = GraphicsInterpreter.Interpret(typedRoot, converter);
        TextToGlyphConverter.Convert(interpretation);
        var svg = options.IncludeSvg ? SvgRenderer.Render(interpretation) : null;
        return new SceneResult(
            interpretation.Graphics,
            interpretation.View,
            interpretation.Warnings,
            interpretation,
            hooks?.Summarize(),
            svg,
            traceCollector?.Export());
    }
}

internal sealed class FuncDrawProvider : KeyValueCollection
{
    private readonly Dictionary<string, object?> _entries;
    private readonly ValueHookSet? _hooks;
    private readonly KeyValueCollection _fallback;

    public FuncDrawProvider(KeyValueCollection fdContext, ValueHookSet? hooks, KeyValueCollection fallback)
    {
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _hooks = hooks;
        _entries = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fd"] = Engine.NormalizeDataType(fdContext)
        };
    }

    public object? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (_entries.TryGetValue(name, out var value))
        {
            return value;
        }

        if (_hooks != null && _hooks.TryGet(name, out var hookValue))
        {
            return hookValue;
        }

        return _fallback.Get(name);
    }

    public KeyValueCollection ParentProvider => _fallback;

    public bool IsDefined(string key, bool hierarchy = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (_entries.ContainsKey(key))
        {
            return true;
        }

        if (_hooks != null && _hooks.Has(key))
        {
            return true;
        }

        return hierarchy && _fallback.IsDefined(key, hierarchy);
    }

    public IList<KeyValuePair<string, object?>> GetAll()
    {
        var list = new List<KeyValuePair<string, object?>>();
        foreach (var entry in _entries)
        {
            list.Add(KeyValuePair.Create(entry.Key, entry.Value));
        }

        return list;
    }

    public IList<string> GetAllKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in _entries.Keys)
        {
            keys.Add(entry);
        }

        if (_hooks != null)
        {
            foreach (var key in _hooks.Keys)
            {
                keys.Add(key);
            }
        }

        foreach (var key in _fallback.GetAllKeys())
        {
            keys.Add(key);
        }

        return keys.ToList();
    }
}

internal sealed class ValueHookSet
{
    private readonly Dictionary<string, ValueHookEntry> _entries;

    private ValueHookSet(Dictionary<string, ValueHookEntry> entries)
    {
        _entries = entries;
    }

    public IReadOnlyCollection<string> Keys => _entries.Values.Select(entry => entry.Name).ToArray();

    public static ValueHookSet? Create(IDictionary<string, Func<object?>>? hooks)
    {
        if (hooks == null || hooks.Count == 0)
        {
            return null;
        }

        var entries = new Dictionary<string, ValueHookEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in hooks)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
            {
                continue;
            }

            var entry = new ValueHookEntry(pair.Key.Trim(), pair.Value);
            entries[entry.NormalizedName] = entry;
        }

        return entries.Count == 0 ? null : new ValueHookSet(entries);
    }

    public bool TryGet(string name, out object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            value = null;
            return false;
        }

        if (_entries.TryGetValue(name, out var entry))
        {
            value = entry.GetValue();
            return true;
        }

        value = null;
        return false;
    }

    public bool Has(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (_entries.TryGetValue(name, out var entry))
        {
            return true;
        }

        return false;
    }

    public object? GetValue(string name)
    {
        return _entries[name].GetValue();
    }

    public Dictionary<string, HookUsage> Summarize()
    {
        var summary = new Dictionary<string, HookUsage>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in _entries.Values)
        {
            summary[entry.Name] = new HookUsage { Used = entry.Used };
        }

        return summary;
    }
}

internal sealed class ValueHookEntry
{
    private bool _hasValue;
    private object? _value = null;

    public ValueHookEntry(string name, Func<object?> factory)
    {
        Name = name;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        NormalizedName = name.ToLowerInvariant();
    }

    public string Name { get; }
    public string NormalizedName { get; }
    public Func<object?> Factory { get; }
    public bool Used { get; private set; }

    public object? GetValue()
    {
        Used = true;
        if (!_hasValue)
        {
            _value = Engine.NormalizeDataType(Factory());
            _hasValue = true;
        }

        return _value;
    }
}

internal static class FdContext
{
    private readonly record struct Point(double X, double Y);
    private readonly record struct Matrix(double A, double B, double C, double D, double E, double F)
    {
        public static Matrix Identity => new(1d, 0d, 0d, 1d, 0d, 0d);

        public Matrix Multiply(Matrix other)
        {
            return new Matrix(
                A * other.A + C * other.B,
                B * other.A + D * other.B,
                A * other.C + C * other.D,
                B * other.C + D * other.D,
                A * other.E + C * other.F + E,
                B * other.E + D * other.F + F);
        }

        public Point Transform(Point point)
        {
            return new Point(
                A * point.X + C * point.Y + E,
                B * point.X + D * point.Y + F);
        }
    }

    private struct BoundsAccumulator
    {
        public bool HasValue;
        public double MinX;
        public double MinY;
        public double MaxX;
        public double MaxY;

        public void Include(Point point)
        {
            if (!HasValue)
            {
                HasValue = true;
                MinX = point.X;
                MaxX = point.X;
                MinY = point.Y;
                MaxY = point.Y;
                return;
            }

            MinX = Math.Min(MinX, point.X);
            MinY = Math.Min(MinY, point.Y);
            MaxX = Math.Max(MaxX, point.X);
            MaxY = Math.Max(MaxY, point.Y);
        }

        public void Include(double minX, double minY, double maxX, double maxY)
        {
            Include(new Point(minX, minY));
            Include(new Point(maxX, maxY));
        }

        public void Expand(double dx, double dy)
        {
            MinX -= dx;
            MaxX += dx;
            MinY -= dy;
            MaxY += dy;
        }
    }

    public static KeyValueCollection Create()
    {
        var measureDelegate = new Func<object, object, object>(MeasureText);
        var rotateDelegate = new Func<object, object, object, object>(Rotate);
        var translateDelegate = new Func<object, object, object, object>(Translate);
        var scaleDelegate = new Func<object, object, object, object, object>(Scale);
        var traslateDelegate = new Func<object, object, object, object>(Traslate);
        var boundingBoxDelegate = new Func<object, object?>(BoundingBox);
        var colorRgbDelegate = new Func<object, object, object, object>(ColorRgb);
        var colorRgbaDelegate = new Func<object, object, object, object, object>(ColorRgba);
        var colorHexDelegate = new Func<object, object>(ColorHex);
        var colorParseDelegate = new Func<object, object>(ColorParse);
        var colorAlphaDelegate = new Func<object, object, object>(ColorAlpha);
        var colorMulAlphaDelegate = new Func<object, object, object>(ColorMulAlpha);
        var colorEntries = new[]
        {
            KeyValuePair.Create("rgb", (object)Engine.NormalizeDataType(colorRgbDelegate)),
            KeyValuePair.Create("rgba", (object)Engine.NormalizeDataType(colorRgbaDelegate)),
            KeyValuePair.Create("hex", (object)Engine.NormalizeDataType(colorHexDelegate)),
            KeyValuePair.Create("parse", (object)Engine.NormalizeDataType(colorParseDelegate)),
            KeyValuePair.Create("alpha", (object)Engine.NormalizeDataType(colorAlphaDelegate)),
            KeyValuePair.Create("mulAlpha", (object)Engine.NormalizeDataType(colorMulAlphaDelegate)),
        };
        var colorCollection = new SimpleKeyValueCollection(null, colorEntries);
        var fdEntries = new[]
        {
            KeyValuePair.Create("measureText", (object)Engine.NormalizeDataType(measureDelegate)),
            KeyValuePair.Create("rotate", (object)Engine.NormalizeDataType(rotateDelegate)),
            KeyValuePair.Create("translate", (object)Engine.NormalizeDataType(translateDelegate)),
            KeyValuePair.Create("traslate", (object)Engine.NormalizeDataType(traslateDelegate)),
            KeyValuePair.Create("scale", (object)Engine.NormalizeDataType(scaleDelegate)),
            KeyValuePair.Create("boundingBox", (object)Engine.NormalizeDataType(boundingBoxDelegate)),
            KeyValuePair.Create("color", (object)Engine.NormalizeDataType(colorCollection)),
        };
        return new SimpleKeyValueCollection(null, fdEntries);
    }

    private static object MeasureText(object rawText, object rawSize)
    {
        var text = rawText?.ToString() ?? string.Empty;
        var size = NormalizeFontSize(rawSize);
        var metrics = FontEngine.Default.MeasureText(text, size, null);
        var pairs = metrics.ToDictionary();
        return new SimpleKeyValueCollection(null, pairs);
    }

    private static object Rotate(object graphics, object rawOrigin, object rawAngle)
    {
        if (!TryReadPoint(rawOrigin, out var origin))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.rotate: expected origin [x, y]");
        }

        var angle = NormalizeNumber(rawAngle);
        if (double.IsNaN(angle))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.rotate: expected angle number (radians)");
        }

        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        var e = origin.X * (1 - cos) + origin.Y * sin;
        var f = -origin.X * sin + origin.Y * (1 - cos);
        var matrix = new ArrayFsList(new object[] { cos, sin, -sin, cos, e, f });
        return CreateTransform(graphics, matrix);
    }

    private static object Translate(object graphics, object rawDx, object rawDy)
    {
        var dx = NormalizeNumber(rawDx);
        if (double.IsNaN(dx))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.translate: expected dx number");
        }

        var dy = NormalizeNumber(rawDy);
        if (double.IsNaN(dy))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.translate: expected dy number");
        }

        var matrix = new ArrayFsList(new object[] { 1d, 0d, 0d, 1d, dx, dy });
        return CreateTransform(graphics, matrix);
    }

    private static object Traslate(object graphics, object rawDx, object rawDy)
    {
        return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.traslate is not supported (did you mean fd.translate?)");
    }

    private static object Scale(object graphics, object rawOrigin, object rawScaleX, object rawScaleY)
    {
        if (!TryReadPoint(rawOrigin, out var origin))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.scale: expected origin [x, y]");
        }

        var sx = NormalizeNumber(rawScaleX);
        if (double.IsNaN(sx))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.scale: expected scaleX number");
        }

        var sy = NormalizeNumber(rawScaleY);
        if (double.IsNaN(sy))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.scale: expected scaleY number");
        }

        var e = origin.X * (1 - sx);
        var f = origin.Y * (1 - sy);
        var matrix = new ArrayFsList(new object[] { sx, 0d, 0d, sy, e, f });
        return CreateTransform(graphics, matrix);
    }

    private static object CreateColor(double r, double g, double b, double a)
    {
        var entries = new[]
        {
            KeyValuePair.Create("type", (object)"color"),
            KeyValuePair.Create("space", (object)"srgb"),
            KeyValuePair.Create("r", (object)r),
            KeyValuePair.Create("g", (object)g),
            KeyValuePair.Create("b", (object)b),
            KeyValuePair.Create("a", (object)a),
        };
        return new SimpleKeyValueCollection(null, entries);
    }

    private static object ColorRgb(object rawR, object rawG, object rawB)
    {
        var r = NormalizeNumber(rawR);
        var g = NormalizeNumber(rawG);
        var b = NormalizeNumber(rawB);
        if (double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.color.rgb: expected numbers r, g, b");
        }

        return CreateColor(r, g, b, 1);
    }

    private static object ColorRgba(object rawR, object rawG, object rawB, object rawA)
    {
        var r = NormalizeNumber(rawR);
        var g = NormalizeNumber(rawG);
        var b = NormalizeNumber(rawB);
        var a = NormalizeNumber(rawA);
        if (double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b) || double.IsNaN(a))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.color.rgba: expected numbers r, g, b, a");
        }

        return CreateColor(r, g, b, a);
    }

    private static object ColorHex(object rawHex)
    {
        var parsed = ParseColor(rawHex, "fd.color.hex");
        return parsed;
    }

    private static object ColorParse(object rawValue)
    {
        return ParseColor(rawValue, "fd.color.parse");
    }

    private static object ColorAlpha(object rawValue, object rawAlpha)
    {
        var parsed = ParseColor(rawValue, "fd.color.alpha");
        if (parsed is FsError)
        {
            return parsed;
        }

        var alpha = NormalizeNumber(rawAlpha);
        if (double.IsNaN(alpha))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.color.alpha: expected alpha number");
        }

        var color = (KeyValueCollection)parsed;
        var r = NormalizeNumber(color.Get("r"));
        var g = NormalizeNumber(color.Get("g"));
        var b = NormalizeNumber(color.Get("b"));
        return CreateColor(r, g, b, alpha);
    }

    private static object ColorMulAlpha(object rawValue, object rawAlpha)
    {
        var parsed = ParseColor(rawValue, "fd.color.mulAlpha");
        if (parsed is FsError)
        {
            return parsed;
        }

        var factor = NormalizeNumber(rawAlpha);
        if (double.IsNaN(factor))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.color.mulAlpha: expected alpha multiplier number");
        }

        var color = (KeyValueCollection)parsed;
        var r = NormalizeNumber(color.Get("r"));
        var g = NormalizeNumber(color.Get("g"));
        var b = NormalizeNumber(color.Get("b"));
        var a = NormalizeNumber(color.Get("a"));
        return CreateColor(r, g, b, a * factor);
    }

    private static object ParseColor(object raw, string functionName)
    {
        if (raw is KeyValueCollection collection)
        {
            var type = collection.Get("type")?.ToString()?.Trim();
            if (string.Equals(type, "color", StringComparison.OrdinalIgnoreCase))
            {
                var space = collection.Get("space")?.ToString()?.Trim();
                if (!string.Equals(space, "srgb", StringComparison.OrdinalIgnoreCase))
                {
                    return new FsError(FsError.ERROR_TYPE_MISMATCH, $"{functionName}: expected srgb color");
                }

                var r = NormalizeNumber(collection.Get("r"));
                var g = NormalizeNumber(collection.Get("g"));
                var b = NormalizeNumber(collection.Get("b"));
                var a = NormalizeNumber(collection.Get("a"));
                if (double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b) || double.IsNaN(a))
                {
                    return new FsError(FsError.ERROR_TYPE_MISMATCH, $"{functionName}: expected fd.color.* value");
                }

                return CreateColor(r, g, b, a);
            }
        }

        var text = raw?.ToString()?.Trim() ?? string.Empty;
        if (!text.StartsWith("#", StringComparison.Ordinal))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, $"{functionName}: expected hex color string '#RRGGBB'");
        }

        var hex = text.Substring(1);
        if (hex.Length is not (3 or 4 or 6 or 8))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, $"{functionName}: expected hex color string '#RGB', '#RGBA', '#RRGGBB', or '#RRGGBBAA'");
        }

        if (!hex.All(IsHexDigit))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, $"{functionName}: expected hex color string '#RGB', '#RGBA', '#RRGGBB', or '#RRGGBBAA'");
        }

        if (hex.Length == 3 || hex.Length == 4)
        {
            var r = ExpandNibble(hex[0]);
            var g = ExpandNibble(hex[1]);
            var b = ExpandNibble(hex[2]);
            var a = hex.Length == 4 ? ExpandNibble(hex[3]) / 255d : 1d;
            return CreateColor(r, g, b, a);
        }

        var rr = ReadPair(hex, 0);
        var gg = ReadPair(hex, 2);
        var bb = ReadPair(hex, 4);
        var aa = hex.Length == 8 ? ReadPair(hex, 6) / 255d : 1d;
        return CreateColor(rr, gg, bb, aa);
    }

    private static bool IsHexDigit(char c)
    {
        return c is >= '0' and <= '9' || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    }

    private static double ExpandNibble(char c)
    {
        var value = Convert.ToInt32(c.ToString(), 16);
        return value * 17d;
    }

    private static double ReadPair(string hex, int start)
    {
        return Convert.ToInt32(hex.Substring(start, 2), 16);
    }

    

    private static object? BoundingBox(object graphics)
    {
        var accumulator = new BoundsAccumulator();
        var error = AppendBounds(graphics, Matrix.Identity, ref accumulator);
        if (error != null)
        {
            return error;
        }

        if (!accumulator.HasValue)
        {
            return null;
        }

        var left = accumulator.MinX;
        var bottom = accumulator.MinY;
        var right = accumulator.MaxX;
        var top = accumulator.MaxY;
        return new SimpleKeyValueCollection(null, new[]
        {
            KeyValuePair.Create("left", (object)left),
            KeyValuePair.Create("bottom", (object)bottom),
            KeyValuePair.Create("right", (object)right),
            KeyValuePair.Create("top", (object)top),
            KeyValuePair.Create("width", (object)(right - left)),
            KeyValuePair.Create("height", (object)(top - bottom))
        });
    }

    private static FsError? AppendBounds(object? value, Matrix transform, ref BoundsAccumulator accumulator)
    {
        if (value == null)
        {
            return null;
        }

        if (value is FsError fsError)
        {
            return fsError;
        }

        if (value is KeyValueCollection collection)
        {
            return AppendBoundsFromCollection(collection, transform, ref accumulator);
        }

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            foreach (var item in enumerable)
            {
                var err = AppendBounds(item, transform, ref accumulator);
                if (err != null)
                {
                    return err;
                }
            }

            return null;
        }

        return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: expected graphics (primitive or list)");
    }

    private static FsError? AppendBoundsFromCollection(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var typeValue = collection.Get("type");
        if (typeValue == null)
        {
            var graphicsValue = collection.Get("graphics");
            if (graphicsValue != null)
            {
                return AppendBounds(graphicsValue, transform, ref accumulator);
            }

            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: expected graphics object (missing 'type' or 'graphics')");
        }

        var typeText = typeValue.ToString() ?? string.Empty;
        var type = typeText.Trim().ToLowerInvariant();
        if (type == "transofrm")
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: unknown primitive type 'transofrm' (did you mean 'transform'?)");
        }

        return type switch
        {
            "line" => AppendLineBounds(collection, transform, ref accumulator),
            "rect" or "rectangle" => AppendRectBounds(collection, transform, ref accumulator),
            "circle" => AppendCircleBounds(collection, transform, ref accumulator),
            "ellipse" => AppendEllipseBounds(collection, transform, ref accumulator),
            "polygon" => AppendPointsBounds(collection, "polygon", transform, ref accumulator),
            "polyline" => AppendPointsBounds(collection, "polyline", transform, ref accumulator),
            "text" => AppendTextBounds(collection, transform, ref accumulator),
            "debug" => null,
            "path" => new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: 'path' is not supported yet"),
            "transform" => AppendTransformBounds(collection, transform, ref accumulator),
            _ => AppendUnknownBounds(collection, typeText, transform, ref accumulator)
        };
    }

    private static FsError? AppendTransformBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var matrixValue = collection.Get("matrix");
        if (matrixValue == null)
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: transform missing 'matrix'");
        }

        if (!TryReadMatrix(matrixValue, out var localMatrix))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: transform.matrix must be [a, b, c, d, e, f]");
        }

        var graphicsValue = collection.Get("graphics");
        if (graphicsValue == null)
        {
            return null;
        }

        return AppendBounds(graphicsValue, transform.Multiply(localMatrix), ref accumulator);
    }

    private static FsError? AppendUnknownBounds(KeyValueCollection collection, string typeText, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var graphicsValue = collection.Get("graphics");
        if (graphicsValue != null)
        {
            return AppendBounds(graphicsValue, transform, ref accumulator);
        }

        var trimmed = typeText.Trim();
        return new FsError(FsError.ERROR_TYPE_MISMATCH, $"fd.boundingbox: unsupported primitive type '{trimmed}'");
    }

    private static FsError? AppendLineBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var fromValue = collection.Get("from");
        if (fromValue == null || !TryReadPoint(fromValue, out var from))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: line.from must be [x, y]");
        }

        var toValue = collection.Get("to");
        if (toValue == null || !TryReadPoint(toValue, out var to))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: line.to must be [x, y]");
        }

        accumulator.Include(transform.Transform(from));
        accumulator.Include(transform.Transform(to));
        ExpandStrokeBounds(collection, transform, ref accumulator);
        return null;
    }

    private static FsError? AppendRectBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var positionValue = collection.Get("position");
        if (positionValue == null || !TryReadPoint(positionValue, out var position))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: rect.position must be [x, y]");
        }

        var sizeValue = collection.Get("size");
        if (sizeValue == null || !TryReadPoint(sizeValue, out var size))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: rect.size must be [width, height]");
        }

        var x = position.X;
        var y = position.Y;
        var w = size.X;
        var h = size.Y;
        accumulator.Include(transform.Transform(new Point(x, y)));
        accumulator.Include(transform.Transform(new Point(x + w, y)));
        accumulator.Include(transform.Transform(new Point(x + w, y + h)));
        accumulator.Include(transform.Transform(new Point(x, y + h)));
        ExpandStrokeBounds(collection, transform, ref accumulator);
        return null;
    }

    private static FsError? AppendCircleBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var centerValue = collection.Get("center");
        if (centerValue == null || !TryReadPoint(centerValue, out var center))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: circle.center must be [x, y]");
        }

        var radiusValue = collection.Get("radius");
        var radius = NormalizeNumber(radiusValue);
        if (double.IsNaN(radius))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: circle.radius must be a number");
        }

        var worldCenter = transform.Transform(center);
        var dx = Math.Abs(radius) * Math.Sqrt(transform.A * transform.A + transform.C * transform.C);
        var dy = Math.Abs(radius) * Math.Sqrt(transform.B * transform.B + transform.D * transform.D);
        accumulator.Include(worldCenter.X - dx, worldCenter.Y - dy, worldCenter.X + dx, worldCenter.Y + dy);
        ExpandStrokeBounds(collection, transform, ref accumulator);
        return null;
    }

    private static FsError? AppendEllipseBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var centerValue = collection.Get("center");
        if (centerValue == null || !TryReadPoint(centerValue, out var center))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: ellipse.center must be [x, y]");
        }

        var rawRx = collection.Get("radiusx") ?? collection.Get("rx");
        var rawRy = collection.Get("radiusy") ?? collection.Get("ry");
        var rx = NormalizeNumber(rawRx);
        var ry = NormalizeNumber(rawRy);
        if (double.IsNaN(rx) || double.IsNaN(ry))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: ellipse.radiusX/radiusY must be numbers");
        }

        var worldCenter = transform.Transform(center);
        var dx = Math.Sqrt(Math.Pow(transform.A * rx, 2) + Math.Pow(transform.C * ry, 2));
        var dy = Math.Sqrt(Math.Pow(transform.B * rx, 2) + Math.Pow(transform.D * ry, 2));
        accumulator.Include(worldCenter.X - dx, worldCenter.Y - dy, worldCenter.X + dx, worldCenter.Y + dy);
        ExpandStrokeBounds(collection, transform, ref accumulator);
        return null;
    }

    private static FsError? AppendPointsBounds(KeyValueCollection collection, string kind, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var pointsValue = collection.Get("points");
        if (pointsValue is not IEnumerable points)
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, $"fd.boundingbox: {kind}.points must be a list of [x, y]");
        }

        var foundAny = false;
        foreach (var pointValue in points)
        {
            if (!TryReadPoint(pointValue, out var point))
            {
                return new FsError(FsError.ERROR_TYPE_MISMATCH, $"fd.boundingbox: {kind}.points must be a list of [x, y]");
            }

            accumulator.Include(transform.Transform(point));
            foundAny = true;
        }

        if (!foundAny)
        {
            return null;
        }

        ExpandStrokeBounds(collection, transform, ref accumulator);
        return null;
    }

    private static FsError? AppendTextBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var positionValue = collection.Get("position");
        if (positionValue == null || !TryReadPoint(positionValue, out var position))
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: text.position must be [x, y]");
        }

        var rawText = collection.Get("text");
        var text = rawText?.ToString() ?? string.Empty;
        var fontSize = NormalizeFontSize(collection.Get("fontsize"));
        var font = collection.Get("font")?.ToString();

        try
        {
            var align = collection.Get("align")?.ToString() ?? "left";
            var typeface = FontEngine.Default.ResolveTypeface(font);
            var mapper = FontEngine.Default.ResolveGlyphMapper(font);
            var scale = fontSize / typeface.UnitsPerEm;
            var ascent = typeface.Ascender * scale;
            var descent = -typeface.Descender * scale;
            var lineHeight = (ascent + descent) * 1.2;
            var normalizedAlign = align.Trim().ToLowerInvariant();
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (var i = 0; i < lines.Length; i++)
            {
                var glyphIndices = lines[i].EnumerateRunes().Select(r => mapper.Lookup(r.Value)).ToArray();
                var lineWidth = glyphIndices.Sum(g => typeface.GetHAdvanceWidthFromGlyphIndex(g) * scale);
                var penX = normalizedAlign switch
                {
                    "center" => position.X - lineWidth / 2,
                    "right" => position.X - lineWidth,
                    _ => position.X
                };
                var baselineY = position.Y - i * lineHeight;

                foreach (var glyphIndex in glyphIndices)
                {
                    var glyph = typeface.GetGlyphByIndex(glyphIndex);
                    if (glyph.Bounds.XMin != glyph.Bounds.XMax || glyph.Bounds.YMin != glyph.Bounds.YMax)
                    {
                        var minX = penX + glyph.Bounds.XMin * scale;
                        var maxX = penX + glyph.Bounds.XMax * scale;
                        var minY = baselineY + glyph.Bounds.YMin * scale;
                        var maxY = baselineY + glyph.Bounds.YMax * scale;
                        accumulator.Include(transform.Transform(new Point(minX, minY)));
                        accumulator.Include(transform.Transform(new Point(maxX, minY)));
                        accumulator.Include(transform.Transform(new Point(maxX, maxY)));
                        accumulator.Include(transform.Transform(new Point(minX, maxY)));
                    }

                    penX += typeface.GetHAdvanceWidthFromGlyphIndex(glyphIndex) * scale;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return new FsError(FsError.ERROR_TYPE_MISMATCH, $"fd.boundingbox: {ex.Message}");
        }
    }

    private static void ExpandStrokeBounds(KeyValueCollection collection, Matrix transform, ref BoundsAccumulator accumulator)
    {
        var rawWidth = collection.Get("width");
        var width = rawWidth == null ? 0.25 : NormalizeNumber(rawWidth);
        if (double.IsNaN(width) || width == 0)
        {
            return;
        }

        var radius = Math.Abs(width) / 2;
        var dx = radius * Math.Sqrt(transform.A * transform.A + transform.C * transform.C);
        var dy = radius * Math.Sqrt(transform.B * transform.B + transform.D * transform.D);
        accumulator.Expand(dx, dy);
    }

    private static bool TryReadMatrix(object raw, out Matrix matrix)
    {
        if (raw is IEnumerable list && raw is not string && raw is not byte[])
        {
            var items = new List<object?>();
            foreach (var item in list)
            {
                items.Add(item);
            }

            if (items.Count == 6)
            {
                var a = NormalizeNumber(items[0]);
                var b = NormalizeNumber(items[1]);
                var c = NormalizeNumber(items[2]);
                var d = NormalizeNumber(items[3]);
                var e = NormalizeNumber(items[4]);
                var f = NormalizeNumber(items[5]);
                if (!double.IsNaN(a) && !double.IsNaN(b) && !double.IsNaN(c) && !double.IsNaN(d) && !double.IsNaN(e) && !double.IsNaN(f))
                {
                    matrix = new Matrix(a, b, c, d, e, f);
                    return true;
                }
            }
        }

        matrix = default;
        return false;
    }

    private static object CreateTransform(object graphics, ArrayFsList matrix)
    {
        return new SimpleKeyValueCollection(null, new[]
        {
            KeyValuePair.Create("type", (object)"transform"),
            KeyValuePair.Create("matrix", (object)matrix),
            KeyValuePair.Create("graphics", graphics)
        });
    }

    private static bool TryReadPoint(object value, out Point point)
    {
        if (value is IEnumerable<object> list)
        {
            var items = list.ToArray();
            if (items.Length >= 2)
            {
                var x = NormalizeNumber(items[0]);
                var y = NormalizeNumber(items[1]);
                if (!double.IsNaN(x) && !double.IsNaN(y))
                {
                    point = new Point(x, y);
                    return true;
                }
            }
        }

        point = default;
        return false;
    }

    private static double NormalizeNumber(object? raw)
    {
        return raw switch
        {
            null => double.NaN,
            double d when !double.IsNaN(d) => d,
            int i => i,
            long l => l,
            _ => double.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN
        };
    }

    private static double NormalizeFontSize(object raw)
    {
        return raw switch
        {
            double d when !double.IsNaN(d) => d,
            int i => i,
            long l => l,
            _ => 12d
        };
    }

}

internal sealed record Metrics(
    double Width,
    double Height,
    double LineHeight,
    double[] Lines,
    double Ascent,
    double Descent,
    double Baseline,
    double AvgCharWidth)
{
    public KeyValuePair<string, object>[] ToDictionary()
    {
        return new[]
        {
            KeyValuePair.Create("width", (object)Width),
            KeyValuePair.Create("height", (object)Height),
            KeyValuePair.Create("lineHeight", (object)LineHeight),
            KeyValuePair.Create("lines", (object)new ArrayFsList(Lines.Select(line => (object)line).ToArray())),
            KeyValuePair.Create("ascent", (object)Ascent),
            KeyValuePair.Create("descent", (object)Descent),
            KeyValuePair.Create("baseline", (object)Baseline),
            KeyValuePair.Create("avgCharWidth", (object)AvgCharWidth)
        };
    }
}

internal sealed class ValueConverter
{
    public object? ToPlain(object value)
    {
        var dataType = Engine.GetFsDataType(value);
        switch (dataType)
        {
            case FSDataType.Null:
                return null;
            case FSDataType.Boolean:
            case FSDataType.Integer:
            case FSDataType.Float:
            case FSDataType.BigInteger:
            case FSDataType.Guid:
            case FSDataType.String:
                return value;
            case FSDataType.ByteArray:
                return Convert.ToBase64String((byte[])value);
            case FSDataType.List:
                return ToPlainList((FsList)value);
            case FSDataType.KeyValueCollection:
                return ToPlainKvc((KeyValueCollection)value);
            case FSDataType.Function:
                return "<function>";
            case FSDataType.Error:
                return FormatError((FsError)value);
            default:
                return value;
        }
    }

    private List<object?> ToPlainList(FsList list)
    {
        var result = new List<object?>();
        foreach (var item in list)
        {
            result.Add(ToPlain(item));
        }

        return result;
    }

    private Dictionary<string, object?> ToPlainKvc(KeyValueCollection collection)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in collection.GetAll())
        {
            result[pair.Key] = ToPlain(pair.Value);
        }

        return result;
    }

    private static object FormatError(FsError error)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["errorType"] = error.ErrorType,
            ["errorMessage"] = error.ErrorMessage
        };
        if (error.ErrorData != null)
        {
            payload["errorData"] = error.ErrorData;
        }

        return payload;
    }
}

internal sealed class TraceCollector
{
    private readonly TraceOptions _options;
    private readonly ValueConverter _converter;
    private readonly TraceEntry _root;
    private readonly Stack<TraceEntry> _stack;
    private readonly string? _filter;

    private TraceCollector(TraceOptions options, ValueConverter converter)
    {
        _options = options;
        _converter = converter;
        _root = new TraceEntry { Path = string.Empty };
        _stack = new Stack<TraceEntry>();
        _stack.Push(_root);
        _filter = NormalizeFilter(options.Filter);
    }

    public static TraceCollector? Create(TraceOptions? options, ValueConverter converter)
    {
        if (options == null || !options.Enabled)
        {
            return null;
        }

        return new TraceCollector(options, converter);
    }

    public object? EntryHook(string path, Engine.TraceInfo info)
    {
        if (!_options.StepInto && _stack.Count > 1)
        {
            return null;
        }

        var node = CreateNode(path, info);
        _stack.Push(node);
        return node;
    }

    public void ExitHook(string path, Engine.TraceInfo info, object entryState)
    {
        if (entryState is not TraceEntry node)
        {
            return;
        }

        ApplyInfo(node, info);
        _stack.Pop();
        var parent = _stack.Peek();
        parent.Children.Add(node);
    }

    public List<TraceEntry> Export()
    {
        var cloned = CloneNodes(_root.Children);
        if (_filter == null)
        {
            return cloned;
        }

        return FilterNodes(cloned, _filter);
    }

    private TraceEntry CreateNode(string path, Engine.TraceInfo info)
    {
        var node = new TraceEntry
        {
            Path = path ?? string.Empty
        };
        ApplyInfo(node, info);
        return node;
    }

    private void ApplyInfo(TraceEntry target, Engine.TraceInfo info)
    {
        target.StartIndex = info.StartIndex;
        target.StartLine = info.StartLine;
        target.StartColumn = info.StartColumn;
        target.EndIndex = info.EndIndex;
        target.EndLine = info.EndLine;
        target.EndColumn = info.EndColumn;
        target.Snippet = info.Snippet;

        var formatted = FormatResult(info.Result);
        target.ResultKind = formatted.Kind;
        if (formatted.Preview != null)
        {
            target.ResultPreview = formatted.Preview;
        }
    }

    private TraceResult FormatResult(object value)
    {
        if (value is FsError error)
        {
            return new TraceResult("error", FormatError(error));
        }

        var kind = DetectResultKind(value);
        if (kind == "error")
        {
            return new TraceResult(kind, FormatError(value));
        }

        if (kind == "atomic")
        {
            return new TraceResult(kind, FormatAtomic(value));
        }

        return new TraceResult(kind, null);
    }

    private static string DetectResultKind(object value)
    {
        if (value == null)
        {
            return "atomic";
        }

        var dataType = Engine.GetFsDataType(value);
        return dataType switch
        {
            FSDataType.Boolean => "atomic",
            FSDataType.Integer => "atomic",
            FSDataType.Float => "atomic",
            FSDataType.BigInteger => "atomic",
            FSDataType.Guid => "atomic",
            FSDataType.String => "atomic",
            FSDataType.ByteArray => "atomic",
            FSDataType.List => "list",
            FSDataType.KeyValueCollection => "kvc",
            FSDataType.Function => "function",
            FSDataType.Error => "error",
            _ => "object"
        };
    }

    private string FormatError(object value)
    {
        if (value is FsError error)
        {
            var dataPreview = error.ErrorData != null
                ? FormatAtomic(_converter.ToPlain(error.ErrorData) ?? error.ErrorData)
                : null;
            if (!string.IsNullOrEmpty(error.ErrorMessage) && dataPreview != null)
            {
                return $"{error.ErrorType}: {error.ErrorMessage} (data: {dataPreview})";
            }

            if (!string.IsNullOrEmpty(error.ErrorMessage))
            {
                return $"{error.ErrorType}: {error.ErrorMessage}";
            }

            if (dataPreview != null)
            {
                return $"{error.ErrorType} (data: {dataPreview})";
            }

            return error.ErrorType ?? "error";
        }

        return FormatAtomic(value);
    }

    private static string FormatAtomic(object value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string text)
        {
            var trimmed = text.Trim();
            return trimmed.Length > 120 ? $"{trimmed[..117]}..." : trimmed;
        }

        if (value is byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static List<TraceEntry> CloneNodes(IEnumerable<TraceEntry> nodes)
    {
        var list = new List<TraceEntry>();
        foreach (var node in nodes)
        {
            var clone = new TraceEntry
            {
                Path = node.Path,
                StartLine = node.StartLine,
                StartColumn = node.StartColumn,
                EndLine = node.EndLine,
                EndColumn = node.EndColumn,
                StartIndex = node.StartIndex,
                EndIndex = node.EndIndex,
                Snippet = node.Snippet,
                ResultKind = node.ResultKind,
                ResultPreview = node.ResultPreview,
                Children = CloneNodes(node.Children)
            };
            list.Add(clone);
        }

        return list;
    }

    private static List<TraceEntry> FilterNodes(IEnumerable<TraceEntry> nodes, string filter)
    {
        var list = new List<TraceEntry>();
        foreach (var node in nodes)
        {
            var filteredChildren = FilterNodes(node.Children, filter);
            if (Matches(node, filter) || filteredChildren.Count > 0)
            {
                var clone = new TraceEntry
                {
                    Path = node.Path,
                    StartLine = node.StartLine,
                    StartColumn = node.StartColumn,
                    EndLine = node.EndLine,
                    EndColumn = node.EndColumn,
                    StartIndex = node.StartIndex,
                    EndIndex = node.EndIndex,
                    Snippet = node.Snippet,
                    ResultKind = node.ResultKind,
                    ResultPreview = node.ResultPreview,
                    Children = filteredChildren
                };
                list.Add(clone);
            }
        }

        return list;
    }

    private static bool Matches(TraceEntry entry, string filter)
    {
        var haystack = $"{entry.Path ?? string.Empty} {entry.Snippet ?? string.Empty}".ToLowerInvariant();
        return haystack.Contains(filter);
    }

    private static string? NormalizeFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        return filter.Trim().ToLowerInvariant();
    }

    private sealed record TraceResult(string Kind, string? Preview);
}

internal sealed class SceneInterpretation
{
    public SceneInterpretation(List<object> graphics, object? view, List<string> warnings, object? step, IFsFunction? stepFunction)
    {
        Graphics = graphics;
        View = view;
        Warnings = warnings;
        Step = step;
        StepFunction = stepFunction;
    }

    public List<object> Graphics { get; }
    public object? View { get; }
    public List<string> Warnings { get; }
    public object? Step { get; }
    [JsonIgnore]
    public IFsFunction? StepFunction { get; }
}

internal static class GraphicsInterpreter
{
    private static readonly HashSet<string> BuiltInTypes =
        new(new[] { "line", "rect", "rectangle", "circle", "ellipse", "polygon", "polyline", "path", "text", "debug", "transform", "group" }, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> StrokedTypes =
        new(new[] { "line", "rect", "rectangle", "circle", "ellipse", "polygon", "polyline", "path" }, StringComparer.OrdinalIgnoreCase);

    public static SceneInterpretation Interpret(object typedRoot, ValueConverter converter)
    {
        var warnings = new List<string>();
        if (typedRoot == null)
        {
            return new SceneInterpretation(new List<object>(), null, warnings, null, null);
        }

        var extraction = ExtractContentRoot(typedRoot, warnings, converter);
        var graphicsTree = extraction.Content != null
            ? NormalizeNode(extraction.Content, warnings, converter, extraction.ContentPath)
            : null;
        var graphics = graphicsTree switch
        {
            List<object> list => list,
            null => new List<object>(),
            object single => new List<object> { single }
        };

        var view = extraction.View != null ? converter.ToPlain(extraction.View) : null;
        var stepFunction = extraction.Step as IFsFunction;
        var stepMarker = stepFunction != null ? "<step>" : extraction.Step;
        return new SceneInterpretation(graphics, view, warnings, stepMarker, stepFunction);
    }

    private static (object? Content, object? View, object? Step, List<string> ContentPath) ExtractContentRoot(
        object typed,
        List<string> warnings,
        ValueConverter converter)
    {
        if (typed is not KeyValueCollection kvc)
        {
            return (typed, null, null, new List<string>());
        }

        var map = BuildEntryMap(kvc);
        map.TryGetValue("view", out var viewEntry);
        map.TryGetValue("graphics", out var graphicsEntry);
        map.TryGetValue("step", out var stepEntry);

        var view = viewEntry?.Value;
        var graphics = graphicsEntry?.Value;

        if (view != null)
        {
            if (graphics == null)
            {
                warnings.Add("View specified without graphics payload.");
                return (null, view, stepEntry?.Value, new List<string> { "view" });
            }

            return (graphics, view, stepEntry?.Value, new List<string> { "graphics" });
        }

        if (graphics != null)
        {
            return (graphics, null, stepEntry?.Value, new List<string> { "graphics" });
        }

        return (typed, null, stepEntry?.Value, new List<string>());
    }

    private static object? NormalizeNode(object value, List<string> warnings, ValueConverter converter, List<string> path)
    {
        if (value == null)
        {
            return null;
        }

        object normalized;
        try
        {
            normalized = Engine.NormalizeDataType(value);
        }
        catch
        {
            warnings.Add($"Skipping unsupported value '{value}'");
            return null;
        }

        var dataType = Engine.GetFsDataType(normalized);
        if (dataType == FSDataType.List)
        {
            return NormalizeList((FsList)normalized, warnings, converter, path);
        }

        if (dataType == FSDataType.KeyValueCollection)
        {
            return NormalizeKvc((KeyValueCollection)normalized, warnings, converter, path);
        }

        warnings.Add($"Skipping unsupported value '{normalized}'");
        return null;
    }

    private static List<object> NormalizeList(FsList list, List<string> warnings, ValueConverter converter, List<string> path)
    {
        var result = new List<object>();
        var index = 0;
        foreach (var item in list)
        {
            var normalized = NormalizeNode(item, warnings, converter, path.Append(index.ToString()).ToList());
            if (normalized != null)
            {
                result.Add(normalized);
            }

            index += 1;
        }

        return result;
    }

    private static object? NormalizeKvc(KeyValueCollection collection, List<string> warnings, ValueConverter converter, List<string> path)
    {
        var entries = BuildEntryMap(collection);
        entries.TryGetValue("type", out var typeEntry);
        entries.TryGetValue("graphics", out var graphicsEntry);

        if (typeEntry == null && graphicsEntry != null)
        {
            return NormalizeNode(graphicsEntry.Value, warnings, converter, path.Append("graphics").ToList());
        }

        if (typeEntry == null)
        {
            warnings.Add("Skipping object without type or graphics information");
            return null;
        }

        var typeText = converter.ToPlain(typeEntry.Value)?.ToString() ?? string.Empty;
        if (string.Equals(typeText, "transofrm", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Unknown primitive type 'transofrm' (did you mean 'transform'?)");
            return null;
        }
        if (string.IsNullOrWhiteSpace(typeText))
        {
            warnings.Add("Skipping primitive with empty type");
            return null;
        }

        var normalizedGraphics = graphicsEntry != null
            ? NormalizeNode(graphicsEntry.Value, warnings, converter, path.Append("graphics").ToList())
            : null;

        if (BuiltInTypes.Contains(typeText))
        {
            var props = CollectProperties(entries, converter, path, "type", "graphics");
            if (normalizedGraphics != null)
            {
                props["graphics"] = normalizedGraphics;
            }

            if (StrokedTypes.Contains(typeText) && !props.ContainsKey("stroke"))
            {
                props["stroke"] = "#38bdf8";
            }

            props["type"] = typeText.ToLowerInvariant();
            return props;
        }

        if (normalizedGraphics == null)
        {
            warnings.Add($"Unknown primitive type '{typeText}' without nested graphics");
            return null;
        }

        var customProps = CollectProperties(entries, converter, path, "type", "graphics");
        var hasOpacity = customProps.Remove("opacity", out var opacity);
        var hasBlendMode = customProps.Remove("blendMode", out var blendMode);
        var graphicsList = normalizedGraphics is List<object> list ? list : new List<object> { normalizedGraphics };
        var custom = new Dictionary<string, object?>
        {
            ["type"] = "custom",
            ["name"] = typeText,
            ["graphics"] = graphicsList,
            ["props"] = customProps
        };
        if (hasOpacity)
        {
            custom["opacity"] = opacity;
        }

        if (hasBlendMode)
        {
            custom["blendMode"] = blendMode;
        }

        return custom;
    }

    private static Dictionary<string, object?> CollectProperties(
        Dictionary<string, KvcEntry> entries,
        ValueConverter converter,
        List<string> path,
        params string[] exclude)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var excluded = new HashSet<string>(exclude.Select(x => x.ToLowerInvariant()));
        foreach (var entry in entries.Values)
        {
            if (excluded.Contains(entry.Key))
            {
                continue;
            }

            result[entry.OriginalName] = converter.ToPlain(entry.Value);
        }

        return result;
    }

    private static Dictionary<string, KvcEntry> BuildEntryMap(KeyValueCollection collection)
    {
        var map = new Dictionary<string, KvcEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in collection.GetAll())
        {
            var key = pair.Key.ToLowerInvariant();
            map[key] = new KvcEntry(pair.Key, pair.Value);
        }

        return map;
    }

    private sealed record KvcEntry(string OriginalName, object Value)
    {
        public string Key => OriginalName.ToLowerInvariant();
    }
}

internal static class SvgRenderer
{
    private static readonly double[] DefaultViewSize = { 1920, 1080 };

    private static string? PaintToCss(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        if (value is IDictionary<string, object?> map)
        {
            var type = Get(map, "type")?.ToString()?.Trim() ?? string.Empty;
            if (string.Equals(type, "color", StringComparison.OrdinalIgnoreCase))
            {
                var space = Get(map, "space")?.ToString()?.Trim();
                if (!string.Equals(space, "srgb", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Unsupported color space (expected srgb)");
                }

                var r = ToDouble(Get(map, "r"), double.NaN);
                var g = ToDouble(Get(map, "g"), double.NaN);
                var b = ToDouble(Get(map, "b"), double.NaN);
                var a = ToDouble(Get(map, "a"), double.NaN);
                if (double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b) || double.IsNaN(a))
                {
                    throw new InvalidOperationException("Invalid srgb color value (expected numbers r,g,b,a)");
                }

                return FormattableString.Invariant($"rgba({r}, {g}, {b}, {a})");
            }
        }

        throw new InvalidOperationException("Unsupported paint value");
    }

    private static string FormatOpacity(IDictionary<string, object?> node)
    {
        var raw = Get(node, "opacity");
        if (raw == null)
        {
            return string.Empty;
        }

        var opacity = ToDouble(raw, double.NaN);
        if (double.IsNaN(opacity))
        {
            throw new InvalidOperationException("opacity must be a finite number");
        }

        return FormattableString.Invariant($" opacity=\"{opacity}\"");
    }

    private static string FormatBlendMode(IDictionary<string, object?> node)
    {
        var raw = Get(node, "blendMode");
        if (raw == null)
        {
            return string.Empty;
        }

        var blendMode = raw.ToString()?.Trim() ?? string.Empty;
        if (blendMode.Length == 0 || string.Equals(blendMode, "source-over", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(blendMode, "normal", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return $" style=\"mix-blend-mode: {EncodeAttribute(blendMode)};\"";
    }

    public static string Render(SceneInterpretation scene)
    {
        if (scene.Graphics == null)
        {
            return string.Empty;
        }

        var viewBox = ResolveViewBox(scene.View);
        var body = string.Concat(scene.Graphics.Select(node => RenderNode(node, viewBox, 0)));
        var transform = FormatRootTransform(viewBox);
        var content = transform.Length > 0 ? $"<g transform=\"{transform}\">{body}</g>" : body;
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{viewBox.Width}\" height=\"{viewBox.Height}\" viewBox=\"0 0 {viewBox.Width} {viewBox.Height}\" fill=\"none\">{content}</svg>";
    }

    private static string RenderNode(object node, ViewBox viewBox, int depth)
    {
        if (node == null)
        {
            return string.Empty;
        }

        if (node is IEnumerable<object> list)
        {
            var inner = string.Concat(list.Select(child => RenderNode(child, viewBox, depth + 1)));
            return $"<g data-layer=\"{depth}\">{inner}</g>";
        }

        if (node is not IDictionary<string, object?> map)
        {
            return string.Empty;
        }

        var type = Get(map, "type")?.ToString()?.ToLowerInvariant() ?? string.Empty;
        return type switch
        {
            "group" => RenderGroup(map, viewBox, depth),
            "line" => RenderLine(map),
            "rect" or "rectangle" => RenderRect(map),
            "circle" => RenderCircle(map),
            "ellipse" => RenderEllipse(map),
            "polygon" => RenderPolygon(map),
            "polyline" => RenderPolyline(map),
            "path" => RenderPath(map),
            "text" => RenderText(map),
            "transform" => RenderTransform(map, viewBox, depth),
            "custom" => RenderCustom(map, viewBox, depth),
            _ => string.Empty
        };
    }

    private static string RenderGroup(IDictionary<string, object?> map, ViewBox viewBox, int depth)
    {
        if (!map.TryGetValue("graphics", out var graphics) || graphics == null)
        {
            return string.Empty;
        }

        var inner = RenderNode(graphics, viewBox, depth + 1);
        return $"<g{FormatOpacity(map)}{FormatBlendMode(map)}>{inner}</g>";
    }

    private static string RenderTransform(IDictionary<string, object?> map, ViewBox viewBox, int depth)
    {
        if (!map.TryGetValue("graphics", out var graphics) || graphics == null)
        {
            return string.Empty;
        }

        var matrix = ReadMatrix(Get(map, "matrix"));
        var formatted = string.Join(' ', matrix.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        var inner = RenderNode(graphics, viewBox, depth + 1);
        return $"<g transform=\"matrix({formatted})\"{FormatOpacity(map)}{FormatBlendMode(map)}>{inner}</g>";
    }

    private static string RenderCustom(IDictionary<string, object?> map, ViewBox viewBox, int depth)
    {
        if (!map.TryGetValue("graphics", out var graphics) || graphics == null)
        {
            return string.Empty;
        }

        var name = Get(map, "name")?.ToString() ?? "custom";
        var props = Get(map, "props") as IDictionary<string, object?> ?? new Dictionary<string, object?>();
        var attributes = string.Concat(props.Select(kv => $" data-{EncodeAttribute(kv.Key)}=\"{EncodeAttribute(kv.Value)}\""));
        var inner = RenderNode(graphics, viewBox, depth + 1);
        return $"<g data-custom=\"{EncodeAttribute(name)}\"{attributes}{FormatOpacity(map)}{FormatBlendMode(map)}>{inner}</g>";
    }

    private static string RenderLine(IDictionary<string, object?> node)
    {
        var from = ToPoint(Get(node, "from"));
        var to = ToPoint(Get(node, "to"));
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        var dash = Get(node, "dash") as IEnumerable<object>;
        var dashAttr = dash != null ? $" stroke-dasharray=\"{string.Join(' ', dash)}\"" : string.Empty;
        return $"<line x1=\"{from.X}\" y1=\"{from.Y}\" x2=\"{to.X}\" y2=\"{to.Y}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{dashAttr}{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderRect(IDictionary<string, object?> node)
    {
        var position = ToPoint(Get(node, "position"));
        var size = ToPoint(Get(node, "size"));
        if (size.Equals(default(Point)))
        {
            size = new Point(1, 1);
        }
        var fill = PaintToCss(Get(node, "fill")) ?? "none";
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<rect x=\"{position.X}\" y=\"{position.Y}\" width=\"{size.X}\" height=\"{size.Y}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderCircle(IDictionary<string, object?> node)
    {
        var center = ToPoint(Get(node, "center"));
        var radius = ToDouble(Get(node, "radius"), 1);
        var fill = PaintToCss(Get(node, "fill")) ?? "none";
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<circle cx=\"{center.X}\" cy=\"{center.Y}\" r=\"{radius}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderEllipse(IDictionary<string, object?> node)
    {
        var center = ToPoint(Get(node, "center"));
        var rx = ToDouble(Get(node, "radiusX") ?? Get(node, "rx"), 1);
        var ry = ToDouble(Get(node, "radiusY") ?? Get(node, "ry"), 1);
        var fill = PaintToCss(Get(node, "fill")) ?? "none";
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<ellipse cx=\"{center.X}\" cy=\"{center.Y}\" rx=\"{rx}\" ry=\"{ry}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderPolygon(IDictionary<string, object?> node)
    {
        if (Get(node, "points") is not IEnumerable<object> points)
        {
            return string.Empty;
        }

        var formatted = string.Join(' ', points.Select(p => ToPoint(p)).Select(p => $"{p.X},{p.Y}"));
        var fill = PaintToCss(Get(node, "fill")) ?? "none";
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<polygon points=\"{formatted}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderPolyline(IDictionary<string, object?> node)
    {
        if (Get(node, "points") is not IEnumerable<object> points)
        {
            return string.Empty;
        }

        var formatted = string.Join(' ', points.Select(p => ToPoint(p)).Select(p => $"{p.X},{p.Y}"));
        var fill = PaintToCss(Get(node, "fill")) ?? "none";
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<polyline points=\"{formatted}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderPath(IDictionary<string, object?> node)
    {
        if (!node.TryGetValue("d", out var d) || d == null)
        {
            return string.Empty;
        }

        var fill = PaintToCss(Get(node, "fill")) ?? "none";
        var stroke = PaintToCss(Get(node, "stroke")) ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<path d=\"{EncodeAttribute(d)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static string RenderText(IDictionary<string, object?> node)
    {
        var text = Get(node, "text")?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var position = ToPoint(Get(node, "position"));
        var fontSize = ToDouble(Get(node, "fontSize"), 12);
        var fill = PaintToCss(Get(node, "color")) ?? PaintToCss(Get(node, "fill")) ?? "#e2e8f0";
        var align = Get(node, "align")?.ToString() ?? "left";
        var font = Get(node, "font")?.ToString();

        var built = FontEngine.Default.BuildTextPath(text, fontSize, font, align, position.X, position.Y);
        if (string.IsNullOrWhiteSpace(built.PathData))
        {
            return string.Empty;
        }

        return $"<path d=\"{EncodeAttribute(built.PathData)}\" fill=\"{fill}\" stroke=\"none\" stroke-width=\"0\"{FormatOpacity(node)}{FormatBlendMode(node)} />";
    }

    private static object? Get(IDictionary<string, object?> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value : null;
    }

    private static double[] ReadMatrix(object? value)
    {
        if (value is not IEnumerable<object> list)
        {
            throw new InvalidOperationException("transform.matrix must be [a, b, c, d, e, f]");
        }

        var items = list.ToArray();
        if (items.Length != 6)
        {
            throw new InvalidOperationException("transform.matrix must be [a, b, c, d, e, f]");
        }

        var matrix = new double[6];
        for (var i = 0; i < matrix.Length; i++)
        {
            var parsed = ToDouble(items[i], double.NaN);
            if (double.IsNaN(parsed))
            {
                throw new InvalidOperationException("transform.matrix must contain only numbers");
            }

            matrix[i] = parsed;
        }

        return matrix;
    }

    private static ViewBox ResolveViewBox(object? view)
    {
        if (view is IEnumerable<object> list)
        {
            var values = list.ToArray();
            if (values.Length >= 2 && double.TryParse(values[0]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var width) &&
                double.TryParse(values[1]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var height) &&
                width > 0 && height > 0)
            {
                return new ViewBox(0, 0, width, height);
            }
        }

        if (view is IDictionary<string, object?> map)
        {
            if (TryReadCoordinate(map, "left", out var left) &&
                TryReadCoordinate(map, "bottom", out var bottom) &&
                TryReadCoordinate(map, "right", out var right) &&
                TryReadCoordinate(map, "top", out var top) &&
                right > left &&
                top > bottom)
            {
                return new ViewBox(left, bottom, right - left, top - bottom);
            }
        }

        return new ViewBox(0, 0, DefaultViewSize[0], DefaultViewSize[1]);
    }

    private static bool TryReadCoordinate(IDictionary<string, object?> map, string key, out double value)
    {
        if (map.TryGetValue(key, out var candidate) &&
            double.TryParse(candidate?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string FormatRootTransform(ViewBox viewBox)
    {
        return $"translate(0 {viewBox.Height}) scale(1 -1) translate({-viewBox.Left} {-viewBox.Bottom})";
    }

    private static Point ToPoint(object? value)
    {
        if (value is IEnumerable<object> list)
        {
            var arr = list.ToArray();
            var x = ToDouble(arr.FirstOrDefault(), 0);
            var y = ToDouble(arr.Length > 1 ? arr[1] : null, 0);
            return new Point(x, y);
        }

        return new Point(0, 0);
    }

    private static double ToDouble(object? value, double fallback)
    {
        if (value == null)
        {
            return fallback;
        }

        if (value is double d)
        {
            return d;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return l;
        }

        return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static string EncodeAttribute(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return text.Replace("\"", "&quot;");
    }

    private readonly record struct Point(double X, double Y);

    private readonly record struct ViewBox(double Left, double Bottom, double Width, double Height)
    {
        public double Right => Left + Width;
        public double Top => Bottom + Height;
    }
}

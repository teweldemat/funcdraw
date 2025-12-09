using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using FuncScript;
using FuncScript.Model;
using FuncScript.Package;

namespace FuncDraw.Net;

internal sealed class FuncDrawOptions
{
    public bool IncludeSvg { get; init; }
    public IDictionary<string, Func<object>>? ValueHooks { get; init; }
    public Func<string, double, Metrics>? MeasureText { get; init; }
}

internal sealed class SceneResult
{
    public SceneResult(
        List<object> graphics,
        object? view,
        List<string> warnings,
        SceneInterpretation raw,
        Dictionary<string, HookUsage>? valueHooks,
        string? svg)
    {
        Graphics = graphics;
        View = view;
        Warnings = warnings;
        Raw = raw;
        ValueHooks = valueHooks;
        Svg = svg;
    }

    public List<object> Graphics { get; }
    public object? View { get; }
    public List<string> Warnings { get; }
    public SceneInterpretation Raw { get; }
    public Dictionary<string, HookUsage>? ValueHooks { get; }
    public string? Svg { get; }
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
        var fdContext = FdContext.Create(options.MeasureText);
        var baseProvider = new DefaultFsDataProvider();
        var provider = new FuncDrawProvider(fdContext, hooks, baseProvider);
        var typedRoot = PackageLoader.LoadPackage(resolver, provider);
        var converter = new ValueConverter();
        var interpretation = GraphicsInterpreter.Interpret(typedRoot, converter);
        var svg = options.IncludeSvg ? SvgRenderer.Render(interpretation) : null;
        return new SceneResult(
            interpretation.Graphics,
            interpretation.View,
            interpretation.Warnings,
            interpretation,
            hooks?.Summarize(),
            svg);
    }
}

internal sealed class FuncDrawProvider : KeyValueCollection
{
    private readonly Dictionary<string, object> _entries;
    private readonly ValueHookSet? _hooks;
    private readonly KeyValueCollection _fallback;

    public FuncDrawProvider(KeyValueCollection fdContext, ValueHookSet? hooks, KeyValueCollection fallback)
    {
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _hooks = hooks;
        _entries = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
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

        if (_hooks != null && _hooks.Contains(key))
        {
            return true;
        }

        return hierarchy && _fallback.IsDefined(key, hierarchy);
    }

    public IList<KeyValuePair<string, object>> GetAll()
    {
        var list = new List<KeyValuePair<string, object>>();
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

    public static ValueHookSet? Create(IDictionary<string, Func<object>>? hooks)
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

    public bool Contains(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return _entries.ContainsKey(name);
    }

    public object GetValue(string name)
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
    private object _value = null!;

    public ValueHookEntry(string name, Func<object> factory)
    {
        Name = name;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        NormalizedName = name.ToLowerInvariant();
    }

    public string Name { get; }
    public string NormalizedName { get; }
    public Func<object> Factory { get; }
    public bool Used { get; private set; }

    public object GetValue()
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
    public static KeyValueCollection Create(Func<string, double, Metrics>? measureText)
    {
        var metrics = measureText ?? DefaultMeasureText;
        var measureDelegate = new Func<object, object, object>((text, size) => Measure(metrics, text, size));
        var fdEntries = new[]
        {
            KeyValuePair.Create("measureText", (object)Engine.NormalizeDataType(measureDelegate))
        };
        return new SimpleKeyValueCollection(null, fdEntries);
    }

    private static object Measure(Func<string, double, Metrics> measure, object rawText, object rawSize)
    {
        var text = rawText?.ToString() ?? string.Empty;
        var size = NormalizeFontSize(rawSize);
        var metrics = measure(text, size);
        var pairs = metrics.ToDictionary();
        return new SimpleKeyValueCollection(null, pairs);
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

    private static Metrics DefaultMeasureText(string text, double size)
    {
        var length = text?.Length ?? 0;
        var width = length * size * 0.6;
        var lineHeight = size * 1.2;
        var ascent = size;
        var descent = lineHeight - ascent;
        return new Metrics(
            width,
            lineHeight,
            ascent,
            descent,
            ascent,
            size * 0.6);
    }
}

internal sealed record Metrics(
    double Width,
    double Height,
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
            KeyValuePair.Create("lineHeight", (object)Height),
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

internal sealed class SceneInterpretation
{
    public SceneInterpretation(List<object> graphics, object? view, List<string> warnings, object? step)
    {
        Graphics = graphics;
        View = view;
        Warnings = warnings;
        Step = step;
    }

    public List<object> Graphics { get; }
    public object? View { get; }
    public List<string> Warnings { get; }
    public object? Step { get; }
}

internal static class GraphicsInterpreter
{
    private static readonly HashSet<string> BuiltInTypes =
        new(new[] { "line", "rect", "rectangle", "circle", "ellipse", "polygon", "polyline", "path", "text", "debug" }, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> StrokedTypes =
        new(new[] { "line", "rect", "rectangle", "circle", "ellipse", "polygon", "polyline", "path" }, StringComparer.OrdinalIgnoreCase);

    public static SceneInterpretation Interpret(object typedRoot, ValueConverter converter)
    {
        var warnings = new List<string>();
        if (typedRoot == null)
        {
            return new SceneInterpretation(new List<object>(), null, warnings, null);
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
        return new SceneInterpretation(graphics, view, warnings, extraction.Step);
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
        var graphicsList = normalizedGraphics is List<object> list ? list : new List<object> { normalizedGraphics };
        return new Dictionary<string, object?>
        {
            ["type"] = "custom",
            ["name"] = typeText,
            ["graphics"] = graphicsList,
            ["props"] = customProps
        };
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
            "line" => RenderLine(map),
            "rect" or "rectangle" => RenderRect(map),
            "circle" => RenderCircle(map),
            "ellipse" => RenderEllipse(map),
            "polygon" => RenderPolygon(map),
            "polyline" => RenderPolyline(map),
            "path" => RenderPath(map),
            "text" => RenderText(map),
            "custom" => RenderCustom(map, viewBox, depth),
            _ => string.Empty
        };
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
        return $"<g data-custom=\"{EncodeAttribute(name)}\"{attributes}>{inner}</g>";
    }

    private static string RenderLine(IDictionary<string, object?> node)
    {
        var from = ToPoint(Get(node, "from"));
        var to = ToPoint(Get(node, "to"));
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        var dash = Get(node, "dash") as IEnumerable<object>;
        var dashAttr = dash != null ? $" stroke-dasharray=\"{string.Join(' ', dash)}\"" : string.Empty;
        return $"<line x1=\"{from.X}\" y1=\"{from.Y}\" x2=\"{to.X}\" y2=\"{to.Y}\" stroke=\"{stroke}\" stroke-width=\"{width}\"{dashAttr} />";
    }

    private static string RenderRect(IDictionary<string, object?> node)
    {
        var position = ToPoint(Get(node, "position"));
        var size = ToPoint(Get(node, "size"));
        if (size.Equals(default(Point)))
        {
            size = new Point(1, 1);
        }
        var fill = Get(node, "fill")?.ToString() ?? "none";
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<rect x=\"{position.X}\" y=\"{position.Y}\" width=\"{size.X}\" height=\"{size.Y}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />";
    }

    private static string RenderCircle(IDictionary<string, object?> node)
    {
        var center = ToPoint(Get(node, "center"));
        var radius = ToDouble(Get(node, "radius"), 1);
        var fill = Get(node, "fill")?.ToString() ?? "none";
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<circle cx=\"{center.X}\" cy=\"{center.Y}\" r=\"{radius}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />";
    }

    private static string RenderEllipse(IDictionary<string, object?> node)
    {
        var center = ToPoint(Get(node, "center"));
        var rx = ToDouble(Get(node, "radiusX") ?? Get(node, "rx"), 1);
        var ry = ToDouble(Get(node, "radiusY") ?? Get(node, "ry"), 1);
        var fill = Get(node, "fill")?.ToString() ?? "none";
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<ellipse cx=\"{center.X}\" cy=\"{center.Y}\" rx=\"{rx}\" ry=\"{ry}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />";
    }

    private static string RenderPolygon(IDictionary<string, object?> node)
    {
        if (Get(node, "points") is not IEnumerable<object> points)
        {
            return string.Empty;
        }

        var formatted = string.Join(' ', points.Select(p => ToPoint(p)).Select(p => $"{p.X},{p.Y}"));
        var fill = Get(node, "fill")?.ToString() ?? "none";
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<polygon points=\"{formatted}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />";
    }

    private static string RenderPolyline(IDictionary<string, object?> node)
    {
        if (Get(node, "points") is not IEnumerable<object> points)
        {
            return string.Empty;
        }

        var formatted = string.Join(' ', points.Select(p => ToPoint(p)).Select(p => $"{p.X},{p.Y}"));
        var fill = Get(node, "fill")?.ToString() ?? "none";
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<polyline points=\"{formatted}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />";
    }

    private static string RenderPath(IDictionary<string, object?> node)
    {
        if (!node.TryGetValue("d", out var d) || d == null)
        {
            return string.Empty;
        }

        var fill = Get(node, "fill")?.ToString() ?? "none";
        var stroke = Get(node, "stroke")?.ToString() ?? "#38bdf8";
        var width = Get(node, "width")?.ToString() ?? "0.25";
        return $"<path d=\"{EncodeAttribute(d)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />";
    }

    private static string RenderText(IDictionary<string, object?> node)
    {
        var text = Get(node, "text")?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var position = ToPoint(Get(node, "position"));
        var align = Get(node, "align")?.ToString()?.ToLowerInvariant() ?? "left";
        var fontSize = ToDouble(Get(node, "fontSize"), 12);
        var fill = Get(node, "color")?.ToString() ?? Get(node, "fill")?.ToString() ?? "#e2e8f0";
        var anchor = align switch
        {
            "center" => "middle",
            "right" => "end",
            _ => "start"
        };
        return $"<text x=\"{position.X}\" y=\"{position.Y}\" font-size=\"{fontSize}\" fill=\"{fill}\" text-anchor=\"{anchor}\">{EncodeAttribute(text)}</text>";
    }

    private static object? Get(IDictionary<string, object?> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value : null;
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

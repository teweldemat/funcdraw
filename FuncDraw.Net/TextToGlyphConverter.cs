using System.Globalization;

namespace FuncDraw.Net;

internal static class TextToGlyphConverter
{
    public static void Convert(SceneInterpretation interpretation)
    {
        var converted = ConvertNode(interpretation.Graphics);
        interpretation.Graphics.Clear();
        interpretation.Graphics.AddRange(converted);
    }

    private static List<object> ConvertNode(List<object> nodes)
    {
        var result = new List<object>(nodes.Count);
        foreach (var node in nodes)
        {
            var converted = ConvertAny(node);
            if (converted != null)
            {
                result.Add(converted);
            }
        }

        return result;
    }

    private static object? ConvertAny(object node)
    {
        if (node is List<object> list)
        {
            return ConvertNode(list);
        }

        if (node is IDictionary<string, object?> map)
        {
            var type = Get(map, "type")?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            if (type == "text")
            {
                return ConvertText(map);
            }

            if (map.TryGetValue("graphics", out var graphics) && graphics != null)
            {
                var converted = ConvertAny(graphics);
                map["graphics"] = converted;
            }

            return map;
        }

        return node;
    }

    private static object? ConvertText(IDictionary<string, object?> node)
    {
        var text = Get(node, "text")?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var position = ToPoint(Get(node, "position"));
        var align = Get(node, "align")?.ToString() ?? "left";
        var fontSize = ToDouble(Get(node, "fontSize"), 12);
        var fill = Get(node, "color") ?? Get(node, "fill") ?? "#e2e8f0";
        var font = Get(node, "font")?.ToString();

        var built = FontEngine.Default.BuildTextPath(text, fontSize, font, align, position.X, position.Y);
        if (string.IsNullOrWhiteSpace(built.PathData))
        {
            return null;
        }

        var path = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "path",
            ["d"] = built.PathData,
            ["fill"] = fill,
            ["stroke"] = "none",
            ["width"] = 0d
        };

        if (node.TryGetValue("opacity", out var opacity) && opacity != null)
        {
            path["opacity"] = opacity;
        }

        if (node.TryGetValue("blendMode", out var blendMode) && blendMode != null)
        {
            path["blendMode"] = blendMode;
        }

        return path;
    }

    private static object? Get(IDictionary<string, object?> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value : null;
    }

    private static (double X, double Y) ToPoint(object? value)
    {
        if (value is IEnumerable<object> list)
        {
            var arr = list.ToArray();
            var x = ToDouble(arr.FirstOrDefault(), 0);
            var y = ToDouble(arr.Length > 1 ? arr[1] : null, 0);
            return (x, y);
        }

        return (0, 0);
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
}

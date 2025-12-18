using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using Typography.OpenFont;

namespace FuncDraw.Net;

internal static class FontEngine
{
    private static readonly Lazy<FontCatalog> DefaultCatalog = new(() =>
    {
        var fontsDir = Path.Combine(AppContext.BaseDirectory, "fonts");
        return new FontCatalog(fontsDir, "Inter-Regular.ttf");
    });

    public static FontCatalog Default => DefaultCatalog.Value;
}

internal sealed class FontCatalog
{
    private readonly string _fontsDirectory;
    private readonly string _defaultFontFile;
    private readonly Dictionary<string, Typeface> _typefaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GlyphMapper> _glyphMappers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public FontCatalog(string fontsDirectory, string defaultFontFile)
    {
        _fontsDirectory = fontsDirectory ?? throw new ArgumentNullException(nameof(fontsDirectory));
        _defaultFontFile = defaultFontFile ?? throw new ArgumentNullException(nameof(defaultFontFile));

        if (!Directory.Exists(_fontsDirectory))
        {
            throw new InvalidOperationException($"Missing fonts directory '{_fontsDirectory}'.");
        }

        var defaultPath = Path.Combine(_fontsDirectory, _defaultFontFile);
        if (!File.Exists(defaultPath))
        {
            throw new InvalidOperationException($"Missing default font '{_defaultFontFile}' in '{_fontsDirectory}'.");
        }
    }

    public string FontsDirectory => _fontsDirectory;
    public string DefaultFontFile => _defaultFontFile;

    public Typeface ResolveTypeface(string? requestedFont)
    {
        var fontFile = ResolveFontFile(requestedFont);
        lock (_lock)
        {
            if (_typefaces.TryGetValue(fontFile, out var cached))
            {
                return cached;
            }

            var fullPath = Path.Combine(_fontsDirectory, fontFile);
            using var stream = File.OpenRead(fullPath);
            var reader = new OpenFontReader();
            var typeface = reader.Read(stream, ReadFlags.Full);
            _typefaces[fontFile] = typeface;
            _glyphMappers[fontFile] = GlyphMapper.Create(typeface);
            return typeface;
        }
    }

    public GlyphMapper ResolveGlyphMapper(string? requestedFont)
    {
        var fontFile = ResolveFontFile(requestedFont);
        lock (_lock)
        {
            if (_glyphMappers.TryGetValue(fontFile, out var cached))
            {
                return cached;
            }

            _ = ResolveTypeface(fontFile);
            return _glyphMappers[fontFile];
        }
    }

    public string ResolveFontFile(string? requestedFont)
    {
        if (string.IsNullOrWhiteSpace(requestedFont))
        {
            return _defaultFontFile;
        }

        var trimmed = requestedFont.Trim();
        if (trimmed.Contains('/') || trimmed.Contains('\\'))
        {
            throw new InvalidOperationException($"Font '{trimmed}' must be a file name from '{_fontsDirectory}', not a path.");
        }

        string? resolved = null;
        if (Path.HasExtension(trimmed))
        {
            resolved = trimmed;
        }
        else
        {
            var ttf = trimmed + ".ttf";
            var otf = trimmed + ".otf";
            if (File.Exists(Path.Combine(_fontsDirectory, ttf)))
            {
                resolved = ttf;
            }
            else if (File.Exists(Path.Combine(_fontsDirectory, otf)))
            {
                resolved = otf;
            }
        }

        if (resolved == null || !File.Exists(Path.Combine(_fontsDirectory, resolved)))
        {
            var available = Directory
                .EnumerateFiles(_fontsDirectory)
                .Select(Path.GetFileName)
                .Where(name => name != null && (name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
            throw new InvalidOperationException($"Unknown font '{trimmed}'. Available fonts: {string.Join(", ", available!)}");
        }

        return resolved;
    }

    public Metrics MeasureText(string text, double fontSize, string? requestedFont)
    {
        var typeface = ResolveTypeface(requestedFont);
        var mapper = ResolveGlyphMapper(requestedFont);
        var scale = fontSize / typeface.UnitsPerEm;
        var lines = (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        var lineWidths = new double[lines.Length];
        var maxWidth = 0d;
        for (var i = 0; i < lines.Length; i++)
        {
            var width = 0d;
            foreach (var rune in lines[i].EnumerateRunes())
            {
                var glyphIndex = mapper.Lookup(rune.Value);
                var advance = typeface.GetHAdvanceWidthFromGlyphIndex(glyphIndex);
                width += advance * scale;
            }

            lineWidths[i] = width;
            maxWidth = Math.Max(maxWidth, width);
        }

        var ascent = typeface.Ascender * scale;
        var descent = -typeface.Descender * scale;
        var lineHeight = (ascent + descent) * 1.2;
        var height = lineHeight * (lines.Length == 0 ? 1 : lines.Length);
        var avgCharWidth = typeface.UnitsPerEm * 0.6 * scale;
        return new Metrics(maxWidth, height, lineHeight, lineWidths, ascent, descent, ascent, avgCharWidth);
    }

    public TextPathResult BuildTextPath(string text, double fontSize, string? requestedFont, string align, double x, double y)
    {
        var typeface = ResolveTypeface(requestedFont);
        var mapper = ResolveGlyphMapper(requestedFont);
        var scale = fontSize / typeface.UnitsPerEm;
        var ascent = typeface.Ascender * scale;
        var descent = -typeface.Descender * scale;
        var lineHeight = (ascent + descent) * 1.2;

        var lines = (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var lineGlyphs = new List<ushort[]>(lines.Length);
        var lineWidths = new double[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var runes = lines[i].EnumerateRunes().ToArray();
            var glyphIndices = new ushort[runes.Length];
            var width = 0d;
            for (var j = 0; j < runes.Length; j++)
            {
                var glyphIndex = mapper.Lookup(runes[j].Value);
                glyphIndices[j] = glyphIndex;
                width += typeface.GetHAdvanceWidthFromGlyphIndex(glyphIndex) * scale;
            }

            lineGlyphs.Add(glyphIndices);
            lineWidths[i] = width;
        }

        var normalizedAlign = (align ?? "left").Trim().ToLowerInvariant();
        var builder = new SvgPathBuilder();
        var bounds = new BoundsAccumulator();

        for (var i = 0; i < lines.Length; i++)
        {
            var baselineY = y - i * lineHeight;
            var lineWidth = lineWidths[i];
            var penX = normalizedAlign switch
            {
                "center" => x - lineWidth / 2,
                "right" => x - lineWidth,
                _ => x
            };

            foreach (var glyphIndex in lineGlyphs[i])
            {
                var glyph = typeface.GetGlyphByIndex(glyphIndex);
                if (glyph.GlyphPoints.Length > 0 && glyph.EndPoints.Length > 0)
                {
                    builder.SetOffset(penX, baselineY);
                    IGlyphReaderExtensions.Read(builder, glyph.GlyphPoints, glyph.EndPoints, (float)scale);
                }

                if (glyph.Bounds.XMin != glyph.Bounds.XMax || glyph.Bounds.YMin != glyph.Bounds.YMax)
                {
                    var minX = penX + glyph.Bounds.XMin * scale;
                    var maxX = penX + glyph.Bounds.XMax * scale;
                    var minY = baselineY + glyph.Bounds.YMin * scale;
                    var maxY = baselineY + glyph.Bounds.YMax * scale;
                    bounds.Include(minX, minY, maxX, maxY);
                }

                penX += typeface.GetHAdvanceWidthFromGlyphIndex(glyphIndex) * scale;
            }
        }

        var maxWidth = lineWidths.Length > 0 ? lineWidths.Max() : 0d;
        var height = lineHeight * (lines.Length == 0 ? 1 : lines.Length);
        var avgCharWidth = typeface.UnitsPerEm * 0.6 * scale;
        return new TextPathResult(builder.ToString(), bounds, new Metrics(maxWidth, height, lineHeight, lineWidths, ascent, descent, ascent, avgCharWidth));
    }

    public TextBoundsResult ComputeTextBounds(string text, double fontSize, string? requestedFont, string align, double x, double y)
    {
        var typeface = ResolveTypeface(requestedFont);
        var mapper = ResolveGlyphMapper(requestedFont);
        var scale = fontSize / typeface.UnitsPerEm;
        var ascent = typeface.Ascender * scale;
        var descent = -typeface.Descender * scale;
        var lineHeight = (ascent + descent) * 1.2;
        var lines = (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var normalizedAlign = (align ?? "left").Trim().ToLowerInvariant();

        var bounds = new BoundsAccumulator();
        var lineWidths = new double[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            var glyphIndices = lines[i].EnumerateRunes().Select(r => mapper.Lookup(r.Value)).ToArray();
            var lineWidth = glyphIndices.Sum(g => typeface.GetHAdvanceWidthFromGlyphIndex(g) * scale);
            lineWidths[i] = lineWidth;
            var penX = normalizedAlign switch
            {
                "center" => x - lineWidth / 2,
                "right" => x - lineWidth,
                _ => x
            };
            var baselineY = y - i * lineHeight;

            foreach (var glyphIndex in glyphIndices)
            {
                var glyph = typeface.GetGlyphByIndex(glyphIndex);
                if (glyph.Bounds.XMin != glyph.Bounds.XMax || glyph.Bounds.YMin != glyph.Bounds.YMax)
                {
                    bounds.Include(
                        penX + glyph.Bounds.XMin * scale,
                        baselineY + glyph.Bounds.YMin * scale,
                        penX + glyph.Bounds.XMax * scale,
                        baselineY + glyph.Bounds.YMax * scale);
                }

                penX += typeface.GetHAdvanceWidthFromGlyphIndex(glyphIndex) * scale;
            }
        }

        var maxWidth = lineWidths.Length > 0 ? lineWidths.Max() : 0d;
        var height = lineHeight * (lines.Length == 0 ? 1 : lines.Length);
        var avgCharWidth = typeface.UnitsPerEm * 0.6 * scale;
        return new TextBoundsResult(bounds, new Metrics(maxWidth, height, lineHeight, lineWidths, ascent, descent, ascent, avgCharWidth));
    }

    internal readonly record struct TextPathResult(string PathData, BoundsAccumulator Bounds, Metrics Metrics);
    internal readonly record struct TextBoundsResult(BoundsAccumulator Bounds, Metrics Metrics);

    internal struct BoundsAccumulator
    {
        public bool HasValue;
        public double MinX;
        public double MinY;
        public double MaxX;
        public double MaxY;

        public void Include(double minX, double minY, double maxX, double maxY)
        {
            if (!HasValue)
            {
                HasValue = true;
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
                return;
            }

            MinX = Math.Min(MinX, minX);
            MinY = Math.Min(MinY, minY);
            MaxX = Math.Max(MaxX, maxX);
            MaxY = Math.Max(MaxY, maxY);
        }

        public void Include(double x, double y)
        {
            Include(x, y, x, y);
        }
    }

    internal sealed class GlyphMapper
    {
        private readonly object[] _charMaps;
        private readonly MethodInfo[] _lookupMethods;
        private readonly Dictionary<int, ushort> _cache = new();

        private GlyphMapper(object[] charMaps, MethodInfo[] lookupMethods)
        {
            _charMaps = charMaps;
            _lookupMethods = lookupMethods;
        }

        public static GlyphMapper Create(Typeface typeface)
        {
            var cmapField = typeof(Typeface).GetField("<CmapTable>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            var cmap = cmapField!.GetValue(typeface)!;
            var charMapsField = cmap.GetType().GetField("_charMaps", BindingFlags.NonPublic | BindingFlags.Instance);
            var charMapsEnumerable = (IEnumerable)charMapsField!.GetValue(cmap)!;
            var charMaps = charMapsEnumerable.Cast<object>().ToArray();
            var methods = charMaps
                .Select(map => map.GetType().GetMethod("CharacterToGlyphIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!)
                .ToArray();
            return new GlyphMapper(charMaps, methods);
        }

        public ushort Lookup(int codepoint)
        {
            if (_cache.TryGetValue(codepoint, out var cached))
            {
                return cached;
            }

            ushort result = 0;
            for (var i = 0; i < _charMaps.Length; i++)
            {
                var glyph = (ushort)_lookupMethods[i].Invoke(_charMaps[i], new object[] { codepoint })!;
                if (glyph != 0)
                {
                    result = glyph;
                    break;
                }
            }

            _cache[codepoint] = result;
            return result;
        }
    }

    private sealed class SvgPathBuilder : IGlyphTranslator
    {
        private readonly StringBuilder _builder = new();
        private double _offsetX;
        private double _offsetY;

        public void SetOffset(double x, double y)
        {
            _offsetX = x;
            _offsetY = y;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public void BeginRead(int contourCount) { }

        public void EndRead() { }

        public void MoveTo(float x0, float y0)
        {
            AppendCommand('M', x0, y0);
        }

        public void LineTo(float x1, float y1)
        {
            AppendCommand('L', x1, y1);
        }

        public void Curve3(float x1, float y1, float x2, float y2)
        {
            AppendCommand('Q', x1, y1, x2, y2);
        }

        public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            AppendCommand('C', x1, y1, x2, y2, x3, y3);
        }

        public void CloseContour()
        {
            _builder.Append('Z');
        }

        private void AppendCommand(char command, params float[] values)
        {
            _builder.Append(command);
            for (var i = 0; i < values.Length; i += 2)
            {
                var x = values[i] + _offsetX;
                var y = values[i + 1] + _offsetY;
                _builder.Append(FormatNumber(x));
                _builder.Append(' ');
                _builder.Append(FormatNumber(y));
                if (i + 2 < values.Length)
                {
                    _builder.Append(' ');
                }
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}

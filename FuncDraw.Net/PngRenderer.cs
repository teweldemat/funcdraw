using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;
using Svg.Skia;

namespace FuncDraw.Net;

internal static class PngRenderer
{
    private static readonly Regex WidthRegex = new("width=\"(?<value>[0-9.+-eE]+)\"", RegexOptions.Compiled);
    private static readonly Regex HeightRegex = new("height=\"(?<value>[0-9.+-eE]+)\"", RegexOptions.Compiled);

    public static void WriteSvgToPng(string? svg, string outputPath)
    {
        if (svg == null)
        {
            throw new InvalidOperationException("Expected SVG output for PNG rendering.");
        }

        var width = ParseSvgDimension(svg, WidthRegex, "width");
        var height = ParseSvgDimension(svg, HeightRegex, "height");

        var skSvg = new SKSvg();
        using var svgStream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
        var picture = skSvg.Load(svgStream);
        if (picture == null)
        {
            throw new InvalidOperationException("Failed to load SVG into Skia renderer.");
        }

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var bounds = picture.CullRect;
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.Scale(width / bounds.Width, height / bounds.Height);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        using var file = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        data.SaveTo(file);
    }

    private static int ParseSvgDimension(string svg, Regex regex, string name)
    {
        var match = regex.Match(svg);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Expected SVG '{name}' attribute.");
        }

        var raw = match.Groups["value"].Value;
        var value = double.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
        return (int)Math.Ceiling(value);
    }
}

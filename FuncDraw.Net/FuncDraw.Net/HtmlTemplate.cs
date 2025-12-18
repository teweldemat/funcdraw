using System;
using System.IO;
using System.Reflection;

namespace FuncDraw.Net;

internal static class HtmlTemplate
{
    private static readonly Lazy<string> Cached = new(BuildContent);

    public static string Content => Cached.Value;

    private static string BuildContent()
    {
        return string.Concat(
            ReadPart("FuncDraw.Net.Template.head.html"),
            ReadPart("FuncDraw.Net.Template.script.js"),
            ReadPart("FuncDraw.Net.Template.tail.html"));
    }

    private static string ReadPart(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Missing embedded template part '{resourceName}'");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

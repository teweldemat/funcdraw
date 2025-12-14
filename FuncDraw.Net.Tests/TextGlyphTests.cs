using System;
using System.Collections.Generic;
using System.IO;
using FuncDraw.Net;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class TextGlyphTests
{
    [Test]
    public void TextPrimitive_IsConvertedToPath()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics:
  {
    type: ""text"";
    position: [1, 1];
    text: ""Hello"";
    fontSize: 12;
    color: ""#fff"";
  };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(includeSvg: true);

            Assert.That(scene.Graphics, Has.Count.EqualTo(1));
            var node = (IDictionary<string, object>)scene.Graphics[0];
            Assert.That(Convert.ToString(node["type"]), Is.EqualTo("path"));
            Assert.That(Convert.ToString(node["d"]), Is.Not.Empty);

            Assert.That(scene.Svg, Does.Contain("<path"));
            Assert.That(scene.Svg, Does.Not.Contain("<text"));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Test]
    public void TextPrimitive_FontMustExistInFontsFolder()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics:
  {
    type: ""text"";
    position: [1, 1];
    text: ""Hello"";
    fontSize: 12;
    font: ""Nope.ttf"";
    color: ""#fff"";
  };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var ex = Assert.Throws<InvalidOperationException>(() => service.Evaluate(includeSvg: false));
            Assert.That(ex!.Message, Does.Contain("Unknown font"));
            Assert.That(ex.Message, Does.Contain("Nope.ttf"));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static FuncDrawEvalService CreateService(string root, string expression)
    {
        var resolver = new ArtResolver(root);
        return new FuncDrawEvalService(
            resolver,
            expression,
            Array.Empty<(string Name, Func<object> Hook)>(),
            Array.Empty<Action<object>>());
    }

    private static string CreateTempArtProject(string fileName, string content)
    {
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-text-glyph-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }
}

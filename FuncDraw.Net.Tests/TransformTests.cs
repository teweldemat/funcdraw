using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FuncDraw.Net;
using FuncScript.Model;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class TransformTests
{
    [Test]
    public void FdTranslate_WrapsGraphicsInTransformPrimitive()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics: fd.translate({ type: ""line""; from: [0, 0]; to: [1, 1]; }, 2, 3);
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            Assert.That(scene.Graphics, Has.Count.EqualTo(1));

            var transform = (IDictionary<string, object>)scene.Graphics[0];
            Assert.That(transform["type"], Is.EqualTo("transform"));

            var matrix = ((IEnumerable<object>)transform["matrix"]).Select(Convert.ToDouble).ToArray();
            CollectionAssert.AreEqual(new[] { 1d, 0d, 0d, 1d, 2d, 3d }, matrix);
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
    public void FdScale_WrapsGraphicsInTransformPrimitive()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics: fd.scale({ type: ""line""; from: [0, 0]; to: [1, 1]; }, [1, 2], 2, 3);
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            Assert.That(scene.Graphics, Has.Count.EqualTo(1));

            var transform = (IDictionary<string, object>)scene.Graphics[0];
            Assert.That(transform["type"], Is.EqualTo("transform"));

            var matrix = ((IEnumerable<object>)transform["matrix"]).Select(Convert.ToDouble).ToArray();
            CollectionAssert.AreEqual(new[] { 2d, 0d, 0d, 3d, -1d, -4d }, matrix);
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
    public void TransformPrimitive_RendersSvgMatrixGroup()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics: { type: ""transform""; matrix: [1, 0, 0, 1, 2, 3]; graphics: { type: ""rect""; position: [1, 1]; size: [2, 2]; fill: ""#fff""; }; };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(true);
            Assert.That(scene.Svg, Does.Contain("transform=\"matrix(1 0 0 1 2 3)\""));
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
    public void Transofrm_IsRejected()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics: { type: ""transofrm""; matrix: [1, 0, 0, 1, 0, 0]; graphics: { type: ""line""; from: [0, 0]; to: [1, 1]; }; };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            Assert.That(scene.Graphics, Is.Empty);
            Assert.That(scene.Warnings, Has.Count.EqualTo(1));
            Assert.That(scene.Warnings[0], Does.Contain("transofrm"));
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
    public void FdTraslate_IsRejected()
    {
        var evalScript = @"
(inState) =>
{
  view: { left:0; bottom:0; right:10; top:10; };
  graphics: fd.traslate({ type: ""line""; from: [0, 0]; to: [1, 1]; }, 4, 5);
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            Assert.That(scene.Graphics, Is.Empty);
            Assert.That(scene.Warnings, Has.Count.EqualTo(1));
            Assert.That(scene.Warnings[0], Does.Contain("fd.traslate"));
            Assert.That(scene.Warnings[0], Does.Contain("fd.translate"));
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
            Array.Empty<Action<object>>(),
            DefaultMeasureString);
    }

    private static object DefaultMeasureString(string text)
    {
        var size = 12d;
        var length = text?.Length ?? 0;
        var width = length * size * 0.6;
        var lineHeight = size * 1.2;
        var ascent = size;
        var descent = lineHeight - ascent;
        var metrics = new Metrics(
            width,
            lineHeight,
            ascent,
            descent,
            ascent,
            size * 0.6);
        return new SimpleKeyValueCollection(null, metrics.ToDictionary());
    }

    private static string CreateTempArtProject(string fileName, string content)
    {
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-transform-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }
}

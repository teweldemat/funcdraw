using System;
using System.Collections.Generic;
using System.IO;
using FuncDraw.Net;
using FuncScript.Model;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class BoundingBoxTests
{
    [Test]
    public void BoundingBox_AccountsForRotationTransform()
    {
        var evalScript = @"
(inState) =>
{
  shape:
  {
    type: ""rect"";
    position: [0, 0];
    size: [2, 1];
    width: 0;
    stroke: ""#000"";
    fill: ""#000"";
  };
  rotated: fd.rotate(shape, [0, 0], math.Pi / 2);
  bbox: fd.boundingbox(rotated);
  eval { view: bbox; graphics: [shape]; };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            var view = (IDictionary<string, object>)scene.View!;

            Assert.That(Convert.ToDouble(view["left"]), Is.EqualTo(-1d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["bottom"]), Is.EqualTo(0d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["right"]), Is.EqualTo(0d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["top"]), Is.EqualTo(2d).Within(1e-6));
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
    public void BoundingBox_AccountsForEllipseRotation()
    {
        var evalScript = @"
(inState) =>
{
  shape:
  {
    type: ""ellipse"";
    center: [0, 0];
    radiusX: 2;
    radiusY: 1;
    width: 0;
    stroke: ""#000"";
  };
  rotated: fd.rotate(shape, [0, 0], math.Pi / 2);
  bbox: fd.boundingbox(rotated);
  eval { view: bbox; graphics: [shape]; };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            var view = (IDictionary<string, object>)scene.View!;

            Assert.That(Convert.ToDouble(view["left"]), Is.EqualTo(-1d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["bottom"]), Is.EqualTo(-2d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["right"]), Is.EqualTo(1d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["top"]), Is.EqualTo(2d).Within(1e-6));
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
    public void BoundingBox_WalksNestedGraphicsShapes()
    {
        var evalScript = @"
(inState) =>
{
  left:
  {
    type: ""circle"";
    center: [0, 0];
    radius: 1;
    width: 0;
    stroke: ""#000"";
  };
  right:
  {
    type: ""circle"";
    center: [0, 0];
    radius: 1;
    width: 0;
    stroke: ""#000"";
  };
  moved: fd.translate(right, 10, 0);
  bbox: fd.boundingbox([left, moved]);
  eval { view: bbox; graphics: [left]; };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = CreateService(tempRoot, "art.scene");
            var scene = service.Evaluate(false);
            var view = (IDictionary<string, object>)scene.View!;

            Assert.That(Convert.ToDouble(view["left"]), Is.EqualTo(-1d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["bottom"]), Is.EqualTo(-1d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["right"]), Is.EqualTo(11d).Within(1e-6));
            Assert.That(Convert.ToDouble(view["top"]), Is.EqualTo(1d).Within(1e-6));
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
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-bbox-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }
}


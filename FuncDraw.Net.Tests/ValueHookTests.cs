using System;
using System.IO;
using System.Linq;
using FuncDraw.Net;
using FuncScript.Model;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class ValueHookTests
{
    [Test]
    public void SceneReceivesClockHookOnDemand()
    {
        var evalScript = @"
eval
{
  graphics:
  [
    {
      type: ""text"";
      position: [0,0];
      text: clock;
      color: ""#fff"";
      fontSize: 10;
    }
  ];
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var hooks = new System.Collections.Generic.Dictionary<string, Func<object?>>
            {
                ["clock"] = () => "12:34:56"
            };

            var service = CreateService(tempRoot, "art.scene", hooks);

            service.Reset();
            var response = service.Evaluate(false);
            Assert.That(response.Graphics, Has.Count.EqualTo(1));
            var textNode = response.Graphics[0] as System.Collections.Generic.IDictionary<string, object>;
            Assert.That(textNode, Is.Not.Null);
            Assert.That(Convert.ToString(textNode!["text"]), Is.EqualTo("12:34:56"));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static FuncDrawEvalService CreateService(string root, string expression, System.Collections.Generic.IDictionary<string, Func<object?>> hooks)
    {
        var resolver = new ArtResolver(root);
        var hookList = hooks.Select(pair => (pair.Key, new Func<object>(() => pair.Value!()!)));
        return new FuncDrawEvalService(
            resolver,
            expression,
            hookList,
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
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-hook-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }

    [Test]
    public void WebClientTimeControlsRequireTimeHookUsed()
    {
        var assembly = typeof(FuncDrawEvalService).Assembly;
        using var stream = assembly.GetManifestResourceStream("FuncDraw.Net.Template.script.js");
        Assert.That(stream, Is.Not.Null);
        using var reader = new StreamReader(stream!);
        var script = reader.ReadToEnd();

        Assert.That(script, Does.Contain("timeHook && timeHook.used"));
        Assert.That(script, Does.Not.Contain("used === false"));
        Assert.That(script, Does.Not.Contain("timeHook.used || timeHook.used === false"));
    }
}

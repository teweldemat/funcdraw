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
      type: ""line"";
      from: [0, 0];
      to: [Len(clock), 0];
      stroke: ""#fff"";
      width: 1;
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
            var lineNode = response.Graphics[0] as System.Collections.Generic.IDictionary<string, object>;
            Assert.That(lineNode, Is.Not.Null);
            Assert.That(Convert.ToString(lineNode!["type"]), Is.EqualTo("line"));
            var to = (System.Collections.Generic.IEnumerable<object>)lineNode["to"];
            Assert.That(Convert.ToDouble(to.First()), Is.EqualTo(8d));
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
            Array.Empty<Action<object>>());
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

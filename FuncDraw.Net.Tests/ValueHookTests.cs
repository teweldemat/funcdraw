using System;
using System.IO;
using FuncDraw.Net;
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

            var service = new SceneService(tempRoot, expressionOverride: "art.scene", valueHooks: hooks);

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

    private static string CreateTempArtProject(string fileName, string content)
    {
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-hook-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }
}

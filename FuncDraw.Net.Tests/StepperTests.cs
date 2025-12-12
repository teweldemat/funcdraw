using System;
using System.IO;
using FuncDraw.Net;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class StepperTests
{
    [Test]
    public void SceneService_StepsStateAcrossEvents()
    {
        var evalScript = @"
(inState) =>
{
  graphics:
  {
    type: ""line"";
    from: [0, 0];
    to: [0, 0];
  };
  step: (evt) =>
  {
    nextState: (inState??0) + 1;
    eval
    {
      state: nextState;
      events: [];
    };
  };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = new SceneService(tempRoot, expressionOverride: "art.scene");
            service.Reset();
            var warmup = service.Evaluate(false);
            Assert.That(warmup.Raw.StepFunction, Is.Not.Null, "Step function should be exposed from the scene");

            Assert.That(PushEvent(service, null), Is.EqualTo(1d));
            Assert.That(PushEvent(service, null), Is.EqualTo(2d));
            Assert.That(PushEvent(service, null), Is.EqualTo(3d));
            Assert.That(PushEvent(service, null), Is.EqualTo(4d));
            Assert.That(PushEvent(service, null), Is.EqualTo(5d));
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
    public void StepFunction_IncrementsAndDecrementsOnEventNames()
    {
        var evalScript = @"
(inState) =>
{
  graphics:
  {
    type: ""line"";
    from: [0, 0];
    to: [0, 0];
  };
  step: (evt) =>
  {
    delta: if evt = ""increase"" then 1 else if evt = ""decrease"" then -1 else 0;
    nextState: (inState??0) + delta;
    eval
    {
      state: nextState;
      events: [];
    };
  };
};
";

        var tempRoot = CreateTempArtProject("scene", evalScript);
        try
        {
            var service = new SceneService(tempRoot, expressionOverride: "art.scene");
            service.Reset();
            var warmup = service.Evaluate(false);
            Assert.That(warmup.Raw.StepFunction, Is.Not.Null);

            Assert.That(PushEvent(service, "increase"), Is.EqualTo(1d));
            Assert.That(PushEvent(service, "increase"), Is.EqualTo(2d));
            Assert.That(PushEvent(service, "noop"), Is.EqualTo(2d));
            Assert.That(PushEvent(service, "decrease"), Is.EqualTo(1d));
            Assert.That(PushEvent(service, "decrease"), Is.EqualTo(0d));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static double PushEvent(SceneService service, object? evt)
    {
        var response = service.PushEvent(new object?[] { evt }, false);
        return Convert.ToDouble(response.State);
    }

    private static string CreateTempArtProject(string fileName, string content)
    {
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-stepper-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }
}

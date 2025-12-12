using System;
using System.IO;
using FuncDraw.Net;
using FuncScript.Model;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class StepperTests
{
    [Test]
    public void  SceneService_StepsStateAcrossEvents()
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
	            var service = CreateService(tempRoot, "art.scene");
	            service.Reset();
	            var warmup = service.Evaluate(false);
	            Assert.That(warmup.Raw.StepFunction, Is.Not.Null, "Step function should be exposed from the scene");

	            Assert.That(PushEvent(service, null), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(1d));
	            Assert.That(PushEvent(service, null), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(2d));
	            Assert.That(PushEvent(service, null), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(3d));
	            Assert.That(PushEvent(service, null), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(4d));
	            Assert.That(PushEvent(service, null), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(5d));
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
	    eval if delta = 0 then null else
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
            var service = CreateService(tempRoot, "art.scene");
	            service.Reset();
	            var warmup = service.Evaluate(false);
	            Assert.That(warmup.Raw.StepFunction, Is.Not.Null);

	            Assert.That(PushEvent(service, "increase"), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(1d));
	            Assert.That(PushEvent(service, "increase"), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(2d));

	            Assert.That(PushEvent(service, "noop"), Is.Null);
	            Assert.That(GetState(service), Is.EqualTo(2d));

	            Assert.That(PushEvent(service, "decrease"), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(1d));
	            Assert.That(PushEvent(service, "decrease"), Is.Not.Null);
	            Assert.That(GetState(service), Is.EqualTo(0d));
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

	    private static SceneResult? PushEvent(FuncDrawEvalService service, object? evt)
	    {
	        return service.PushEvent(evt, false);
	    }

	    private static double GetState(FuncDrawEvalService service)
	    {
	        return Convert.ToDouble(service.State);
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
        var root = Path.Combine(Path.GetTempPath(), "funcdraw-stepper-" + Guid.NewGuid().ToString("N"));
        var artDir = Path.Combine(root, "art");
        Directory.CreateDirectory(artDir);
        File.WriteAllText(Path.Combine(artDir, $"{fileName}.fs"), content);
        return root;
    }
}

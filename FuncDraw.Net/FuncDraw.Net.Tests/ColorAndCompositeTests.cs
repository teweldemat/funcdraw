using System;
using System.Collections.Generic;
using System.Linq;
using FuncDraw.Net;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
public class ColorAndCompositeTests
{
    private static void AssertSrgb(
        IDictionary<string, object> color,
        double r,
        double g,
        double b,
        double a)
    {
        Assert.That(Convert.ToString(color["type"]), Is.EqualTo("color"));
        Assert.That(Convert.ToString(color["space"]), Is.EqualTo("srgb"));
        Assert.That(Convert.ToDouble(color["r"]), Is.EqualTo(r).Within(1e-12));
        Assert.That(Convert.ToDouble(color["g"]), Is.EqualTo(g).Within(1e-12));
        Assert.That(Convert.ToDouble(color["b"]), Is.EqualTo(b).Within(1e-12));
        Assert.That(Convert.ToDouble(color["a"]), Is.EqualTo(a).Within(1e-12));
    }

    [Test]
    public void ColorAlphaAndGroupPrimitive_AppearInRawAndSvg()
    {
        var resolver = new MockResolver();
        resolver.AddExpression(new[] { "eval" }, @"
{
  view:[10,10];
  graphics:
  [
    {
      type:""group"";
      opacity:0.5;
      blendMode:""multiply"";
      graphics:
      [
        {
          type:""rect"";
          position:[0,0];
          size:[10,10];
          fill:fd.color.alpha(""#93c5fd"", 0.25);
          stroke:""none"";
          width:0;
        }
      ];
    }
  ];
}
");

        var result = FuncDrawRuntime.LoadGraphics(
            resolver,
            new FuncDrawOptions
            {
                IncludeSvg = true
            });

        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Graphics, Has.Count.EqualTo(1));

        var group = (IDictionary<string, object>)result.Graphics[0];
        Assert.That(Convert.ToString(group["type"]), Is.EqualTo("group"));
        Assert.That(Convert.ToDouble(group["opacity"]), Is.EqualTo(0.5d));
        Assert.That(Convert.ToString(group["blendMode"]), Is.EqualTo("multiply"));

        var children = ((IEnumerable<object>)group["graphics"]).ToArray();
        Assert.That(children, Has.Length.EqualTo(1));
        var rect = (IDictionary<string, object>)children[0];
        Assert.That(Convert.ToString(rect["type"]), Is.EqualTo("rect"));

        var fill = (IDictionary<string, object>)rect["fill"];
        Assert.That(Convert.ToString(fill["type"]), Is.EqualTo("color"));
        Assert.That(Convert.ToString(fill["space"]), Is.EqualTo("srgb"));
        Assert.That(Convert.ToDouble(fill["r"]), Is.EqualTo(147d));
        Assert.That(Convert.ToDouble(fill["g"]), Is.EqualTo(197d));
        Assert.That(Convert.ToDouble(fill["b"]), Is.EqualTo(253d));
        Assert.That(Convert.ToDouble(fill["a"]), Is.EqualTo(0.25d));

        Assert.That(result.Svg, Does.Contain("opacity=\"0.5\""));
        Assert.That(result.Svg, Does.Contain("mix-blend-mode: multiply"));
        Assert.That(result.Svg, Does.Contain("fill=\"rgba(147, 197, 253, 0.25)\""));
    }

    [Test]
    public void ColorHex_ParsesShortAndLongForms()
    {
        var resolver = new MockResolver();
        resolver.AddExpression(new[] { "eval" }, @"
{
  view:[10,10];
  graphics:
  [
    { type:""rect""; name:""rgb3""; position:[0,0]; size:[1,1]; fill:fd.color.hex(""#abc""); stroke:""none""; width:0; };
    { type:""rect""; name:""rgba4""; position:[0,0]; size:[1,1]; fill:fd.color.hex(""#abcd""); stroke:""none""; width:0; };
    { type:""rect""; name:""rgb6""; position:[0,0]; size:[1,1]; fill:fd.color.hex(""#aabbcc""); stroke:""none""; width:0; };
    { type:""rect""; name:""rgba8""; position:[0,0]; size:[1,1]; fill:fd.color.hex(""#aabbccdd""); stroke:""none""; width:0; };
  ];
}
");

        var result = FuncDrawRuntime.LoadGraphics(
            resolver,
            new FuncDrawOptions
            {
                IncludeSvg = false
            });

        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Graphics, Has.Count.EqualTo(4));

        var byName = result.Graphics
            .Cast<IDictionary<string, object>>()
            .ToDictionary(node => Convert.ToString(node["name"]) ?? string.Empty, node => node);

        AssertSrgb((IDictionary<string, object>)byName["rgb3"]["fill"], 170, 187, 204, 1);
        AssertSrgb((IDictionary<string, object>)byName["rgb6"]["fill"], 170, 187, 204, 1);
        AssertSrgb((IDictionary<string, object>)byName["rgba4"]["fill"], 170, 187, 204, 221d / 255d);
        AssertSrgb((IDictionary<string, object>)byName["rgba8"]["fill"], 170, 187, 204, 221d / 255d);
    }

    [Test]
    public void CustomPrimitive_LiftsCompositingFields()
    {
        var resolver = new MockResolver();
        resolver.AddExpression(new[] { "eval" }, @"
{
  view:[10,10];
  graphics:
  [
    {
      type:""heatmap"";
      opacity:0.5;
      blendMode:""multiply"";
      palette:""thermal"";
      graphics:
      [
        { type:""rect""; position:[0,0]; size:[10,10]; fill:""#93c5fd""; stroke:""none""; width:0; }
      ];
    }
  ];
}
");

        var result = FuncDrawRuntime.LoadGraphics(
            resolver,
            new FuncDrawOptions
            {
                IncludeSvg = true
            });

        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Graphics, Has.Count.EqualTo(1));

        var custom = (IDictionary<string, object>)result.Graphics[0];
        Assert.That(Convert.ToString(custom["type"]), Is.EqualTo("custom"));
        Assert.That(Convert.ToString(custom["name"]), Is.EqualTo("heatmap"));
        Assert.That(Convert.ToDouble(custom["opacity"]), Is.EqualTo(0.5d));
        Assert.That(Convert.ToString(custom["blendMode"]), Is.EqualTo("multiply"));

        var props = (IDictionary<string, object>)custom["props"];
        Assert.That(props.ContainsKey("opacity"), Is.False);
        Assert.That(props.ContainsKey("blendMode"), Is.False);

        Assert.That(result.Svg, Does.Contain("data-custom=\"heatmap\""));
        Assert.That(result.Svg, Does.Contain("opacity=\"0.5\""));
        Assert.That(result.Svg, Does.Contain("mix-blend-mode: multiply"));
    }
}

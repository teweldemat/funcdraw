using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FuncDraw.Net;
using FuncScript;
using FuncScript.Core;
using FuncScript.Model;
using FuncScript.Package;
using NUnit.Framework;
using System.Text;

namespace FuncDraw.Net.Tests;

[TestFixture]
    public class FuncDrawRuntimeTests
    {
        [Test]
        public void ArtResolver_IgnoresOperatorLikeSegments()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
            var testLibRoot = Path.Combine(repoRoot, "examples", "testlib");
            var resolver = new ArtResolver(testLibRoot);

            var root = PackageLoader.LoadPackage(resolver);
            Assert.That(root, Is.AssignableTo<KeyValueCollection>());

            var bugexp = ((KeyValueCollection)root).Get("bugexp");
            Assert.That(bugexp, Is.AssignableTo<KeyValueCollection>(), "bugexp should be a module with evaluated exports");

            var angle = ((KeyValueCollection)bugexp).Get("angle");
            Assert.That(angle, Is.TypeOf<double>());
            Assert.That((double)angle, Is.EqualTo(System.Math.PI / 2).Within(1e-6));
        }

        [Test]
    public void ModuleWithEvalChildIsEvaluatedWhenAccessedAsPackageMember()
    {
        var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "cartoon", "character", "eval" }, @"
(anchor, height) =>
{
  type: ""line"";
  from: anchor;
  to: [anchor[0], anchor[1] + height];
  stroke: ""#38bdf8"";
  width: 0.35;
}");

            var rootResolver = new MockResolver();
            rootResolver.AddExpression(new[] { "scene" }, @"
{
  view:{ left:-5; bottom:-5; right:15; top:20; };
  character: package(""lib"").cartoon.character([0,0], 10);
  graphics: character;
}");
            rootResolver.AddExpression(new[] { "eval" }, "scene");
            rootResolver.AddPackage("lib", libResolver);

            var result = FuncDrawRuntime.LoadGraphics(
                rootResolver,
                new FuncDrawOptions
                {
                    IncludeSvg = false
                });

            Assert.That(result.Warnings, Is.Empty, "Expected nested module with eval to evaluate, not error");
            Assert.That(result.Graphics, Has.Count.EqualTo(1));
            var line = (IDictionary<string, object>)result.Graphics[0];
            CollectionAssert.AreEqual(new[] { 0d, 0d }, ExtractNumbers(line["from"]));
            CollectionAssert.AreEqual(new[] { 0d, 10d }, ExtractNumbers(line["to"]));
            Assert.That(line["type"], Is.EqualTo("line"));
        Assert.That(line["stroke"], Is.EqualTo("#38bdf8"));
    }

    [Test]
    public void CharacterMergesDefaultMeasurementsAndAppliesPalette()
    {
        var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "cartoon", "character", "defaultMeasurements" }, @"
{
  height: 10;
  leftHand: [-4, 0];
  rightHand: [4, 0];
  leftLeg: [-2, -4];
  rightLeg: [2, -4];
}");
        libResolver.AddExpression(new[] { "cartoon", "character", "eval" }, @"
(anchor, measurements, palette) =>
{
  defaults: defaultMeasurements;
  m:
  {
    height: measurements.height ?? defaults.height;
    leftHand: measurements.leftHand ?? defaults.leftHand;
    rightHand: measurements.rightHand ?? defaults.rightHand;
    leftLeg: measurements.leftLeg ?? defaults.leftLeg;
    rightLeg: measurements.rightLeg ?? defaults.rightLeg;
  };

  line:
  {
    type: ""line"";
    from: anchor;
    to: [anchor[0], anchor[1] + m.height];
    stroke: palette.body;
    width: 0.35;
  };

  leftHand:
  {
    type: ""line"";
    from: [anchor[0], anchor[1] + m.height];
    to: [anchor[0] + m.leftHand[0], anchor[1] + m.height + m.leftHand[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  rightHand:
  {
    type: ""line"";
    from: [anchor[0], anchor[1] + m.height];
    to: [anchor[0] + m.rightHand[0], anchor[1] + m.height + m.rightHand[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  leftLeg:
  {
    type: ""line"";
    from: anchor;
    to: [anchor[0] + m.leftLeg[0], anchor[1] + m.leftLeg[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  rightLeg:
  {
    type: ""line"";
    from: anchor;
    to: [anchor[0] + m.rightLeg[0], anchor[1] + m.rightLeg[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  eval [line, leftHand, rightHand, leftLeg, rightLeg];
}");

        var rootResolver = new MockResolver();
        rootResolver.AddExpression(new[] { "eval" }, @"
{
  palette: { body: ""#111111""; limb: ""#222222""; };
  character: package(""lib"").cartoon.character([0,0], { height: 8; rightHand: [3,1]; }, palette);
  eval { view: { left:0; bottom:0; right:10; top:12; }; graphics: character; };
}");
        rootResolver.AddPackage("lib", libResolver);

        var result = FuncDrawRuntime.LoadGraphics(
            rootResolver,
            new FuncDrawOptions
            {
                IncludeSvg = false
            });

        Assert.That(result.Warnings, Is.Empty, "Expected character to render without warnings");
        Assert.That(result.Graphics, Has.Count.EqualTo(5), "Character should emit body plus four limbs");

        var body = (IDictionary<string, object>)result.Graphics[0];
        var leftHand = (IDictionary<string, object>)result.Graphics[1];
        var rightHand = (IDictionary<string, object>)result.Graphics[2];
        var leftLeg = (IDictionary<string, object>)result.Graphics[3];
        var rightLeg = (IDictionary<string, object>)result.Graphics[4];

        CollectionAssert.AreEqual(new[] { 0d, 0d }, ExtractNumbers(body["from"]));
        CollectionAssert.AreEqual(new[] { 0d, 8d }, ExtractNumbers(body["to"]));
        Assert.That(body["stroke"], Is.EqualTo("#111111"));

        CollectionAssert.AreEqual(new[] { 0d, 8d }, ExtractNumbers(leftHand["from"]));
        CollectionAssert.AreEqual(new[] { -4d, 8d }, ExtractNumbers(leftHand["to"]));
        CollectionAssert.AreEqual(new[] { 0d, 8d }, ExtractNumbers(rightHand["from"]));
        CollectionAssert.AreEqual(new[] { 3d, 9d }, ExtractNumbers(rightHand["to"]));

        CollectionAssert.AreEqual(new[] { 0d, 0d }, ExtractNumbers(leftLeg["from"]));
        CollectionAssert.AreEqual(new[] { -2d, -4d }, ExtractNumbers(leftLeg["to"]));
        CollectionAssert.AreEqual(new[] { 0d, 0d }, ExtractNumbers(rightLeg["from"]));
        CollectionAssert.AreEqual(new[] { 2d, -4d }, ExtractNumbers(rightLeg["to"]));

        Assert.That(leftHand["stroke"], Is.EqualTo("#222222"));
        Assert.That(rightHand["stroke"], Is.EqualTo("#222222"));
        Assert.That(leftLeg["stroke"], Is.EqualTo("#222222"));
        Assert.That(rightLeg["stroke"], Is.EqualTo("#222222"));
    }

    [Test]
    public void PackageMemberAccessOnPackageResolverReturnsFunctionResult()
    {
    var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "square" }, @"
(center, sideLength, style)=>
{
  centerPoint:center ?? [0,0];
  side:sideLength ?? 10;
  options:style ?? {};
  halfSide:side / 2;
  fillColor:options.fill ?? ""#38bdf8"";
  strokeColor:options.stroke ?? ""#0f172a"";
  strokeWidth:options.width ?? 0.5;
  centerX:centerPoint[0] ?? 0;
  centerY:centerPoint[1] ?? 0;
  eval
  {
    type:""rect"";
    position:[centerX - halfSide, centerY - halfSide];
    size:[side, side];
    fill:fillColor;
    stroke:strokeColor;
    width:strokeWidth;
  }
}");

        var rootResolver = new MockResolver();
        rootResolver.AddExpression(new[] { "eval" }, "scene");
        rootResolver.AddExpression(new[] { "scene" }, @"
{
  lib:package(""lib"");
  square:lib.square([10,20], 8, { fill:""#123456""; stroke:""#abcdef""; width:0.75; });
  graphics:[ square ];
}");
        rootResolver.AddPackage("lib", libResolver);

        var result = FuncDrawRuntime.LoadGraphics(
            rootResolver,
            new FuncDrawOptions
            {
                IncludeSvg = false
            });

        Assert.That(result.Warnings, Is.Empty, "Unexpected warnings were emitted from package member access");
        Assert.That(result.Graphics, Has.Count.EqualTo(1), "Expected exactly one graphic from package member call");

        var rect = AssertIsRect(result.Graphics[0]);
        var position = ExtractNumbers(rect["position"]);
        var size = ExtractNumbers(rect["size"]);

        Assert.That(rect["type"], Is.EqualTo("rect"));
        Assert.That(rect["fill"], Is.EqualTo("#123456"));
        Assert.That(rect["stroke"], Is.EqualTo("#abcdef"));
        Assert.That(rect["width"], Is.EqualTo(0.75));
        Assert.That(position[0], Is.EqualTo(6d));  // 10 - 8/2
        Assert.That(position[1], Is.EqualTo(16d)); // 20 - 8/2
        Assert.That(size[0], Is.EqualTo(8d));
        Assert.That(size[1], Is.EqualTo(8d));
    }

    [Test]
    public void PackageMemberAccessWithNestedResolverMatchesImportTestShape()
    {
        var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "square" }, @"
(center, sideLength, style)=>
{
  centerPoint:center ?? [0,0];
  side:sideLength ?? 10;
  options:style ?? {};
  halfSide:side / 2;
  fillColor:options.fill ?? ""#38bdf8"";
  strokeColor:options.stroke ?? ""#0f172a"";
  strokeWidth:options.width ?? 0.5;
  centerX:centerPoint[0] ?? 0;
  centerY:centerPoint[1] ?? 0;
  eval
  {
    type:""rect"";
    position:[centerX - halfSide, centerY - halfSide];
    size:[side, side];
    fill:fillColor;
    stroke:strokeColor;
    width:strokeWidth;
  }
}");

        var rootResolver = new MockResolver();
        rootResolver.AddExpression(new[] { "eval" }, "scene");
        rootResolver.AddExpression(new[] { "scene" }, @"
{
  view:{ left:-50; bottom:-10; right:50; top:40; };
  lib:package(""lib"");
  square:lib.square;
  graphics:[
    lib.square([10,30], 8, { fill:""#123456""; stroke:""#abcdef""; width:0.75; }),
    square([30,30], 8, { fill:""#fedcba""; stroke:""#654321""; width:0.6; })
  ];
}");
        rootResolver.AddPackage("lib", libResolver);

        var result = FuncDrawRuntime.LoadGraphics(
            rootResolver,
            new FuncDrawOptions
            {
                IncludeSvg = false
            });

        Assert.That(result.Warnings, Is.Empty, "Expected package functions to resolve without type mismatches");
        Assert.That(result.Graphics, Has.Count.EqualTo(2), "Both calls to lib.square should render");
        Assert.That(((IDictionary<string, object>)result.Graphics[0])["fill"], Is.EqualTo("#123456"));
        Assert.That(((IDictionary<string, object>)result.Graphics[1])["fill"], Is.EqualTo("#fedcba"));
    }

    [Test]
    public void PackageLoaderReturnsNestedPackageForMockResolver()
    {
        var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "square" }, "x => x + 1");

        var rootResolver = new MockResolver();
        rootResolver.AddExpression(new[] { "eval" }, "lib.square(3)");
        rootResolver.AddExpression(new[] { "lib" }, "package(\"lib\")");
        rootResolver.AddPackage("lib", libResolver);

        var package = PackageLoader.LoadPackage(rootResolver);
        Assert.That(package, Is.Not.InstanceOf<FsError>(), "eval expression should run without errors");
        Assert.That(package, Is.EqualTo(4), "lib.square(3) should evaluate through nested package");
    }

    [Test]
    public void PackageLoaderEvaluatesPiDivisionInNestedPackage()
    {
        var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "bugexp" }, @"
{
  piOverTwo: math.Pi / 2;
  eval
  {
    angle: piOverTwo;
  };
}");

        var rootResolver = new MockResolver();
        rootResolver.AddExpression(new[] { "eval" }, @"package(""lib"").bugexp.piOverTwo");
        rootResolver.AddPackage("lib", libResolver);

        var result = PackageLoader.LoadPackage(rootResolver);

        Assert.That(result, Is.Null, "Expected hidden intermediate member to be null");
    }

    [Test]
    public void MultiplyUsesAllOperandsWhenMixingNumericTypes()
    {
        var result = Engine.Evaluate(new DefaultFsDataProvider(), "-5 * 0 * 0.5");
        Assert.That(Convert.ToDouble(result), Is.EqualTo(0d));
    }

    [Test]
    public void SkeletonMergeKeepsHandLengths()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var defaultMeasurements = File.ReadAllText(Path.Combine(repoRoot, "examples", "testlib", "art", "cartoon", "character", "defaultMeasurements.fs"));
        var skeletonEval = File.ReadAllText(Path.Combine(repoRoot, "examples", "testlib", "art", "cartoon", "character", "skeleton", "eval.fs"));

        var libResolver = new MockResolver();
        libResolver.AddExpression(new[] { "cartoon", "character", "defaultMeasurements" }, defaultMeasurements);
        libResolver.AddExpression(new[] { "cartoon", "character", "skeleton", "eval" }, skeletonEval);

        var rootResolver = new MockResolver();
        rootResolver.AddExpression(new[] { "eval" }, @"
{
  dm: package(""lib"").cartoon.character.defaultMeasurements;
  anchor: [-3, 0];
  base:
  {
    leftLeg: { end: [-3, -14]; };
    rightLeg: { end: [3, -14]; };
  };

  baseProfile: dm + base;
  sideProfile: baseProfile + { leftLeg: baseProfile.leftLeg + { sign: 1; }; rightLeg: baseProfile.rightLeg + { sign: 1; }; };
  animated: sideProfile +
  {
    leftHand: { end: dm.leftHand.end; upper: dm.leftHand.upper; lower: dm.leftHand.lower; sign: dm.leftHand.sign; };
    rightHand: { end: dm.rightHand.end; upper: dm.rightHand.upper; lower: dm.rightHand.lower; sign: dm.rightHand.sign; };
  };

  geometry: package(""lib"").cartoon.character.skeleton.build(anchor, animated);

  distance: (from, to) =>
  {
    dx: to[0] - from[0];
    dy: to[1] - from[1];
    eval math.Sqrt(dx * dx + dy * dy);
  };

  upper: distance(geometry.leftHand.from, geometry.leftHand.joint);
  lower: distance(geometry.leftHand.joint, geometry.leftHand.to);

  eval { upper; lower; };
}");
        rootResolver.AddPackage("lib", libResolver);

        var result = PackageLoader.LoadPackage(rootResolver);
        Assert.That(result, Is.AssignableTo<KeyValueCollection>(), "Expected package load to succeed");
        var root = (KeyValueCollection)result;

        var upper = Convert.ToDouble(root.Get("upper"));
        var lower = Convert.ToDouble(root.Get("lower"));
        Assert.That(upper, Is.EqualTo(8d).Within(1e-4));
        Assert.That(lower, Is.EqualTo(8d).Within(1e-4));
    }

    private static double[] ExtractNumbers(object value)
    {
        Assert.That(value, Is.InstanceOf<System.Collections.IEnumerable>(), "Expected sequence value");
        var enumerable = (System.Collections.IEnumerable)value;
        return enumerable.Cast<object>().Select(Convert.ToDouble).ToArray();
    }

    private static IDictionary<string, object> AssertIsRect(object node)
    {
        Assert.That(node, Is.InstanceOf<IDictionary<string, object>>(), "Graphic node should be a dictionary");
        var dict = (IDictionary<string, object>)node;
        Assert.That(dict.ContainsKey("position"), Is.True);
        Assert.That(dict.ContainsKey("size"), Is.True);
        Assert.That(dict.ContainsKey("fill"), Is.True);
        Assert.That(dict.ContainsKey("stroke"), Is.True);
        Assert.That(dict.ContainsKey("width"), Is.True);
        return dict;
    }
}

internal sealed class MockResolver : IFsPackageResolver
{
    private sealed class Node
    {
        public Dictionary<string, Node> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? Expression { get; set; }
    }

    private readonly Node _root = new();
    private readonly Dictionary<string, MockResolver> _packages = new(StringComparer.OrdinalIgnoreCase);

    public string WatchPath => ".";

    public void AddExpression(IReadOnlyList<string> path, string expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var node = ResolveOrCreate(Normalize(path));
        node.Expression = expression;
    }

    public void AddPackage(string name, MockResolver resolver)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Package name is required.", nameof(name));
        }

        _packages[name] = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public IEnumerable<PackageNodeDescriptor> ListChildren(IReadOnlyList<string> path)
    {
        var node = ResolveNode(Normalize(path));
        if (node == null)
        {
            return Array.Empty<PackageNodeDescriptor>();
        }

        return node.Children.Keys.Select(child => new PackageNodeDescriptor(child));
    }

    public PackageExpressionDescriptor? GetExpression(IReadOnlyList<string> path)
    {
        var node = ResolveNode(Normalize(path));
        if (node == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(node.Expression))
        {
            return null;
        }

        return new PackageExpressionDescriptor(node.Expression, PackageLanguages.FuncScript);
    }

    public IFsPackageResolver? Package(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _packages.TryGetValue(name, out var resolver) ? resolver : null;
    }

    private Node ResolveOrCreate(IReadOnlyList<string> path)
    {
        var current = _root;
        foreach (var segment in path)
        {
            if (!current.Children.TryGetValue(segment, out var child))
            {
                child = new Node();
                current.Children[segment] = child;
            }

            current = child;
        }

        return current;
    }

    private Node? ResolveNode(IReadOnlyList<string> path)
    {
        var current = _root;
        foreach (var segment in path)
        {
            if (!current.Children.TryGetValue(segment, out var child))
            {
                return null;
            }

            current = child;
        }

        return current;
    }

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string>? path)
    {
        if (path == null)
        {
            return Array.Empty<string>();
        }

        return path
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
    }
}

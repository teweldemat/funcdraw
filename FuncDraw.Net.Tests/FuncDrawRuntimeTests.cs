using System;
using System.Collections.Generic;
using System.Linq;
using FuncDraw.Net;
using FuncScript.Core;
using FuncScript.Model;
using FuncScript.Package;
using NUnit.Framework;

namespace FuncDraw.Net.Tests;

[TestFixture]
    public class FuncDrawRuntimeTests
    {
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

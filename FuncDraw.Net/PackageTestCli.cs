using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using FuncScript.Package;
using FuncScript.Model;
using global::FuncScript;
using System.Text.Json;

namespace FuncDraw.Net;

internal static class PackageTestCli
{
    private const int MaxFailuresToShow = 10;

    public static int Run(string projectRoot)
    {
        Console.WriteLine("FuncDraw.Net test mode");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resolver = new ArtResolver(projectRoot);
            var result = PackageTestRunner.TestPackage(resolver);
            var summary = result.Summary;

            DumpDebug(result, resolver);

            if (summary.Scripts == 0)
            {
                Console.WriteLine("No FuncScript test pairs found in the loaded package.");
                return 0;
            }

            var failures = CollectTestFailures(result.Tests);
            if (summary.Failed == 0)
            {
                Console.WriteLine(
                    $"All {summary.Cases} case(s) passed across {summary.Scripts} script(s) in {stopwatch.ElapsedMilliseconds}ms.");
                return 0;
            }

            Console.Error.WriteLine(
                $"FuncScript package tests failed ({summary.Failed}/{summary.Cases} case(s) across {summary.Scripts} script(s)).");

            foreach (var failure in failures.Take(MaxFailuresToShow))
            {
                Console.Error.WriteLine(FormatFailureMessage(failure));
                if (!string.IsNullOrWhiteSpace(failure.StackTrace))
                {
                    Console.Error.WriteLine(IndentMultiline(failure.StackTrace, 4));
                }
            }

            if (failures.Count > MaxFailuresToShow)
            {
                Console.Error.WriteLine(
                    $"...and {failures.Count - MaxFailuresToShow} more failure(s) not shown (limit {MaxFailuresToShow}).");
            }

            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[funcdraw.net] Failed to run FuncScript package tests: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                Console.Error.WriteLine(IndentMultiline(ex.StackTrace, 2));
            }
            return 1;
        }
    }

    private static List<TestFailure> CollectTestFailures(IReadOnlyList<PackageTestRunner.PackageTestEntry> tests)
    {
        var failures = new List<TestFailure>();
        foreach (var entry in tests)
        {
            foreach (var suite in entry.Result.Suites)
            {
                foreach (var caseResult in suite.Cases)
                {
                    if (caseResult.Passed)
                    {
                        continue;
                    }

                    failures.Add(new TestFailure(
                        entry.Path,
                        entry.TestPath,
                        string.IsNullOrWhiteSpace(suite.Name) ? suite.Id : suite.Name,
                        caseResult.Index,
                        caseResult.Error));
                }
            }
        }

        return failures;
    }

    private static string FormatFailureMessage(TestFailure failure)
    {
        var segments = new List<string>();
        if (!string.IsNullOrWhiteSpace(failure.ScriptPath))
        {
            segments.Add(failure.ScriptPath);
        }

        if (!string.IsNullOrWhiteSpace(failure.TestPath) &&
            !string.Equals(failure.TestPath, failure.ScriptPath, StringComparison.OrdinalIgnoreCase))
        {
            segments.Add($"test: {failure.TestPath}");
        }

        if (!string.IsNullOrWhiteSpace(failure.SuiteName))
        {
            segments.Add($"suite: {failure.SuiteName}");
        }

        segments.Add($"case #{failure.CaseIndex}");
        var location = string.Join(" · ", segments);
        var message = FormatCaseErrorMessage(failure.Error);
        if (string.IsNullOrWhiteSpace(message))
        {
            return $"- {location} failed";
        }

        return $"- {location} failed: {message}";
    }

    private static string FormatCaseErrorMessage(PackageTestRunner.PackageTestCaseError? error)
    {
        if (error == null)
        {
            return string.Empty;
        }

        var detailsText = error.Details != null ? $" (details: {SafeStringify(error.Details)})" : string.Empty;
        if (!string.IsNullOrWhiteSpace(error.Message))
        {
            if (!string.IsNullOrWhiteSpace(error.Type))
            {
                return $"{error.Type}: {error.Message}{detailsText}";
            }

            return $"{error.Message}{detailsText}";
        }

        return string.IsNullOrWhiteSpace(error.Type) ? detailsText.Trim() : $"{error.Type}{detailsText}";
    }

    private static string SafeStringify(object value)
    {
        try
        {
            return value is string text ? text : JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    private static string IndentMultiline(string text, int spaces)
    {
        var padding = new string(' ', spaces);
        return string.Join(Environment.NewLine, text.Split('\n').Select(line => padding + line.TrimEnd('\r')));
    }

    private static void DumpDebug(PackageTestRunner.PackageTestResult result, ArtResolver resolver)
    {
        var interesting = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cartoon/stickman/multiStepProfile",
            "cartoon/stickman/skeleton/build"
        };
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        KeyValueCollection? runProvider = null;
        try
        {
            var baseProvider = PackageLoader.CreatePackageProvider(resolver);
            var packageRoot = PackageLoader.LoadPackage(resolver, baseProvider);
            var bindings = new SimpleKeyValueCollection(baseProvider, new[]
            {
                KeyValuePair.Create("__fs_package", Engine.NormalizeDataType(packageRoot))
            });
            runProvider = new KvcProvider(bindings, baseProvider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[debug] Failed to build debug provider: {ex.Message}");
        }

        foreach (var entry in result.Tests)
        {
            if (!interesting.Contains(entry.Path))
            {
                continue;
            }

            Console.WriteLine($"[debug] {entry.Path} ({entry.TestPath})");
            foreach (var suite in entry.Result.Suites)
            {
                foreach (var caseResult in suite.Cases)
                {
                    Console.WriteLine($"  suite \"{suite.Name}\" case {caseResult.Index} passed={caseResult.Passed}");
                    if (caseResult.Error != null)
                    {
                        Console.WriteLine($"    error: {FormatCaseErrorMessage(caseResult.Error)}");
                    }

                    Console.WriteLine($"    expr: {JsonSerializer.Serialize(caseResult.ExpressionResult, options)}");
                    Console.WriteLine($"    assert: {JsonSerializer.Serialize(caseResult.AssertionResult, options)}");
                }

                if (runProvider != null)
                {
                    if (string.Equals(entry.Path, "cartoon/stickman/skeleton/build", StringComparison.OrdinalIgnoreCase))
                    {
                        PrintDebugEvaluation(runProvider, DebugExpressions.SkeletonBuild, "skeleton build");
                    }
                    else if (string.Equals(entry.Path, "cartoon/stickman/multiStepProfile", StringComparison.OrdinalIgnoreCase))
                    {
                        PrintDebugEvaluation(runProvider, DebugExpressions.MultiStepProfile, "multiStepProfile");
                    }
                }
            }
        }
    }

    private static void PrintDebugEvaluation(KeyValueCollection provider, string expression, string label)
    {
        try
        {
            var value = Engine.Evaluate(provider, expression);
            var buffer = new StringBuilder();
            Engine.Format(buffer, value);
            Console.WriteLine($"    eval {label}: {buffer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    eval {label} failed: {ex.Message}");
        }
    }

    private static class DebugExpressions
    {
        public const string SkeletonBuild = @"
{
  normalizeFn:__fs_package[""cartoon""][""stickman""][""skeleton""][""normalize""];
  buildFn:__fs_package[""cartoon""][""stickman""][""skeleton""][""build""];
  normalized:normalizeFn({});
  result:buildFn(normalized);
  eval { normalized:normalized; result:result };
}";

        public const string MultiStepProfile = @"
{
  builder:__fs_package[""cartoon""][""stickman""][""multiStepProfile""];
  args:{
    initialPosition:[0, 18.6];
    initialMeasurements:{
      torso:{ direction:""right""; height:20; width:2 };
      head:{ direction:""right""; verticalExtent:5 };
      hands:{
        left:{ effectorCoordinate:[-7.8, 4.7]; upperLength:8; lowerLength:8 };
        right:{ effectorCoordinate:[7.8, 4.7]; upperLength:8; lowerLength:8 };
      };
      legs:{
        left:{ upperLength:12.4; lowerLength:11.6; effectorCoordinate:[-4, -18.6] };
        right:{ upperLength:12.4; lowerLength:11.6; effectorCoordinate:[4, -18.6] };
      };
    };
    displacement:30;
    progress:0.4;
    strideLength:12;
    handSwing:{ enabled:true; mode:""mirror""; amplitude:14; lift:1.8; forwardOffset:0 };
    direction:""right"";
  };

  first:builder(args);
  second:builder(args);

  eval { args:args; first:first; second:second };
}";
    }

    private sealed record TestFailure(
        string ScriptPath,
        string TestPath,
        string SuiteName,
        int CaseIndex,
        PackageTestRunner.PackageTestCaseError? Error)
    {
        public string? StackTrace => Error?.Stack;
    }
}

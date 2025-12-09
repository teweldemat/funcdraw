using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FuncScript.Package;

namespace FuncDraw.Net;

internal sealed class ArtResolver : IFsPackageResolver
{
    private static readonly string[] SupportedExtensions = { ".fs", ".js" };

    private readonly string _projectRoot;
    private readonly string _artRoot;
    private readonly NodeModuleFinder _moduleFinder;

    public ArtResolver(string projectRoot, string artFolderName = "art")
    {
        _projectRoot = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
        _artRoot = Path.Combine(_projectRoot, artFolderName ?? "art");
        _moduleFinder = new NodeModuleFinder(_projectRoot);
        if (!Directory.Exists(_artRoot))
        {
            throw new InvalidOperationException($"Art folder '{_artRoot}' not found.");
        }
    }

    public string WatchPath => _artRoot;

    public IEnumerable<PackageNodeDescriptor> ListChildren(IReadOnlyList<string> path)
    {
        if (path != null && path.Any(segment => segment == "."))
        {
            return Array.Empty<PackageNodeDescriptor>();
        }

        var targetDir = ResolveDirectory(path);
        if (targetDir == null)
        {
            return Array.Empty<PackageNodeDescriptor>();
        }

        return Directory.EnumerateFileSystemEntries(targetDir)
            .Select(entry => new FileInfo(entry))
            .Where(info => info.Exists)
            .SelectMany(info =>
            {
                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    return new[] { info.Name };
                }

                var extension = info.Extension.ToLowerInvariant();
                if (SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return new[] { Path.GetFileNameWithoutExtension(info.Name) };
                }

                return Array.Empty<string>();
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new PackageNodeDescriptor(name));
    }

    public PackageExpressionDescriptor? GetExpression(IReadOnlyList<string> path)
    {
        if (path != null && path.Any(segment => segment == "."))
        {
            return null;
        }

        if (path == null || path.Count == 0)
        {
            return null;
        }

        var resolvedFile = ResolveFile(path);
        if (resolvedFile.FullPath == null)
        {
            return null;
        }
        var content = File.ReadAllText(resolvedFile.FullPath);
        if (resolvedFile.Extension == ".js")
        {
            return new PackageExpressionDescriptor(content, PackageLanguages.JavaScript);
        }

        return new PackageExpressionDescriptor(content, PackageLanguages.FuncScript);
    }

    public IFsPackageResolver? Package(string name)
    {
        var packageRoot = _moduleFinder.FindPackageRoot(name);
        if (packageRoot == null)
        {
            throw new InvalidOperationException($"Package '{name}' was not found under node_modules.");
        }

        return new ArtResolver(packageRoot, "art");
    }

    private string? ResolveDirectory(IReadOnlyList<string>? pathSegments)
    {
        var segments = pathSegments == null || pathSegments.Count == 0
            ? Array.Empty<string>()
            : pathSegments.ToArray();
        var target = Path.Combine(new[] { _artRoot }.Concat(segments).ToArray());
        return Directory.Exists(target) ? target : null;
    }

    private ResolvedFile ResolveFile(IReadOnlyList<string> pathSegments)
    {
        var normalized = pathSegments.ToArray();
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException("Expression path cannot be empty.");
        }

        var directory = normalized.Length > 1 ? ResolveDirectory(normalized.Take(normalized.Length - 1).ToArray()) : _artRoot;
        if (directory == null || !Directory.Exists(directory))
        {
            return new ResolvedFile(null, null);
        }
        var baseName = normalized[^1];

        if (baseName == ".")
        {
            return new ResolvedFile(null, null);
        }

        var fsPath = Path.Combine(directory, baseName + ".fs");
        if (File.Exists(fsPath))
        {
            return new ResolvedFile(fsPath, ".fs");
        }

        var jsPath = Path.Combine(directory, baseName + ".js");
        if (File.Exists(jsPath))
        {
            return new ResolvedFile(jsPath, ".js");
        }

        return new ResolvedFile(null, null);
    }

    private readonly record struct ResolvedFile(string? FullPath, string? Extension);
}

internal sealed class NodeModuleFinder
{
    private readonly string _projectRoot;

    public NodeModuleFinder(string projectRoot)
    {
        _projectRoot = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
    }

    public string? FindPackageRoot(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            throw new ArgumentException("Package name is required.", nameof(rawName));
        }

        if (rawName.StartsWith(".", StringComparison.Ordinal) || rawName.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Package name '{rawName}' must be a bare package identifier.");
        }

        var name = rawName.Replace("\\", "/").Trim();
        var segments = name.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || (segments[0].StartsWith("@", StringComparison.Ordinal) && segments.Length != 2))
        {
            throw new InvalidOperationException($"Package name '{rawName}' is not a supported format.");
        }

        var packagePathSegments = segments.ToArray();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(Path.Combine(_projectRoot, "node_modules"));

        while (queue.Count > 0)
        {
            var nodeModules = queue.Dequeue();
            var resolvedNodeModules = Path.GetFullPath(nodeModules);
            if (!visited.Add(resolvedNodeModules))
            {
                continue;
            }

            if (!Directory.Exists(resolvedNodeModules))
            {
                continue;
            }

            var candidate = Path.Combine(new[] { resolvedNodeModules }.Concat(packagePathSegments).ToArray());
            if (Directory.Exists(candidate) && HasArtFolder(candidate))
            {
                return candidate;
            }

            foreach (var child in Directory.EnumerateDirectories(resolvedNodeModules))
            {
                var nested = Path.Combine(child, "node_modules");
                queue.Enqueue(nested);
            }
        }

        var ancestor = _projectRoot;
        while (!string.IsNullOrEmpty(ancestor))
        {
            var candidate = Path.Combine(new[] { ancestor }.Concat(packagePathSegments).ToArray());
            if (Directory.Exists(candidate) && HasArtFolder(candidate))
            {
                return candidate;
            }

            if (packagePathSegments.Length == 2 && packagePathSegments[0].StartsWith("@", StringComparison.Ordinal))
            {
                var unscoped = Path.Combine(ancestor, packagePathSegments[1]);
                if (Directory.Exists(unscoped) && HasArtFolder(unscoped))
                {
                    return unscoped;
                }
            }

            ancestor = Directory.GetParent(ancestor)?.FullName;
        }

        return null;
    }

    private static bool HasArtFolder(string packageRoot)
    {
        return Directory.Exists(Path.Combine(packageRoot, "art"));
    }
}

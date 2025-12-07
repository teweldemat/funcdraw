'use strict';

const fs = require('fs');
const path = require('path');

const dirEntriesCache = new Map();
const statCache = new Map();
const expressionCache = new Map();
const resolvedFileCache = new Map();

function createArtResolver(rootDir, artFolderName = 'art', options = {}) {
  const artRoot = path.resolve(rootDir, artFolderName);
  if (!fs.existsSync(artRoot) || !fs.statSync(artRoot).isDirectory()) {
    return null;
  }

  const moduleSearchPaths = buildModuleSearchPaths(rootDir, options.modulePaths);
  const moduleCache = new Map();

  const resolver = {
    listChildren(pathSegments = []) {
      const dirPath = resolveDirectory(artRoot, pathSegments);
      if (!dirPath) {
        return [];
      }
      if (dirEntriesCache.has(dirPath)) {
        return dirEntriesCache.get(dirPath);
      }
      const entries = fs.readdirSync(dirPath, { withFileTypes: true });
      const names = [];
      for (const entry of entries) {
        if (entry.isDirectory()) {
          names.push(entry.name);
        } else if (entry.isFile()) {
          const ext = path.extname(entry.name).toLowerCase();
          if (ext === '.fs' || ext === '.js') {
            names.push(stripExtension(entry.name));
          }
        }
      }
      dirEntriesCache.set(dirPath, names);
      return names;
    },
    getExpression(pathSegments = []) {
      const file = resolveFile(artRoot, pathSegments);
      if (!file) {
        return null;
      }
      if (file.ext === '.fs') {
        return loadTextExpression(file.fullPath, 'funcscript');
      }
      if (file.ext === '.js') {
        return loadTextExpression(file.fullPath, 'javascript');
      }
      return null;
    },
    package(name) {
      return importNodeModule({
        name,
        cache: moduleCache,
        modulePaths: moduleSearchPaths
      });
    }
  };

  return {
    resolver,
    watchPath: artRoot
  };
}

function resolveDirectory(root, segments) {
  const target = path.join(root, ...segments);
  const cached = statCache.get(target);
  if (cached) {
    return cached.isDir ? target : null;
  }
  if (!fs.existsSync(target)) {
    statCache.set(target, { isDir: false, isFile: false });
    return null;
  }
  const stat = fs.statSync(target);
  const entry = { isDir: stat.isDirectory(), isFile: stat.isFile() };
  statCache.set(target, entry);
  return entry.isDir ? target : null;
}

function resolveFile(root, segments) {
  if (!Array.isArray(segments) || segments.length === 0) {
    return null;
  }
  const cacheKey = `${root}::${segments.join('/')}`;
  if (resolvedFileCache.has(cacheKey)) {
    return resolvedFileCache.get(cacheKey);
  }
  const dirSegments = segments.slice(0, -1);
  const baseName = segments[segments.length - 1];
  const dirPath = path.join(root, ...dirSegments);
  if (!resolveDirectory(root, dirSegments)) {
    resolvedFileCache.set(cacheKey, null);
    return null;
  }
  const fsPath = path.join(dirPath, `${baseName}.fs`);
  if (isFile(fsPath)) {
    const resolved = { fullPath: fsPath, ext: '.fs' };
    resolvedFileCache.set(cacheKey, resolved);
    return resolved;
  }
  const jsPath = path.join(dirPath, `${baseName}.js`);
  if (isFile(jsPath)) {
    const resolved = { fullPath: jsPath, ext: '.js' };
    resolvedFileCache.set(cacheKey, resolved);
    return resolved;
  }
  resolvedFileCache.set(cacheKey, null);
  return null;
}

function isFile(target) {
  const cached = statCache.get(target);
  if (cached) {
    return cached.isFile;
  }
  if (!fs.existsSync(target)) {
    statCache.set(target, { isDir: false, isFile: false });
    return false;
  }
  const stat = fs.statSync(target);
  const entry = { isDir: stat.isDirectory(), isFile: stat.isFile() };
  statCache.set(target, entry);
  return entry.isFile;
}

function stripExtension(name) {
  return name.replace(/\.[^.]+$/, '');
}

function loadTextExpression(filePath, language) {
  const cached = expressionCache.get(filePath);
  if (cached) {
    return cached;
  }
  const text = fs.readFileSync(filePath, 'utf8');
  const payload = {
    expression: text,
    language
  };
  expressionCache.set(filePath, payload);
  return payload;
}

module.exports = {
  createArtResolver
};

function buildModuleSearchPaths(rootDir, extraPaths) {
  const paths = [];
  if (typeof rootDir === 'string' && rootDir.length > 0) {
    paths.push(rootDir);
  }
  if (Array.isArray(extraPaths)) {
    for (const entry of extraPaths) {
      if (typeof entry === 'string' && entry.trim().length > 0) {
        paths.push(entry);
      }
    }
  }
  const unique = [];
  const seen = new Set();
  for (const target of paths) {
    const normalized = path.resolve(target);
    if (seen.has(normalized)) {
      continue;
    }
    seen.add(normalized);
    unique.push(normalized);
  }
  return unique;
}

function importNodeModule({ name, cache, modulePaths }) {
  if (name == null) {
    return null;
  }
  const packageName = parsePackageName(name);
  if (!packageName) {
    return null;
  }
  let moduleResolver = cache.get(packageName);
  if (!moduleResolver) {
    moduleResolver = loadModuleResolver(packageName, modulePaths);
    if (!moduleResolver) {
      return null;
    }
    cache.set(packageName, moduleResolver);
  }
  return moduleResolver;
}

function loadModuleResolver(packageName, modulePaths) {
  const resolvedRoot = resolvePackageRoot(packageName, modulePaths);
  if (!resolvedRoot) {
    return null;
  }
  const moduleResolver = createArtResolver(resolvedRoot, 'art', {
    modulePaths
  });
  if (!moduleResolver) {
    throw new Error(`Package '${packageName}' does not expose an art/ directory for FuncDraw`);
  }
  return moduleResolver.resolver;
}

function resolvePackageRoot(packageName, modulePaths) {
  const searchPaths = Array.isArray(modulePaths) && modulePaths.length > 0 ? modulePaths : undefined;
  try {
    const manifestPath = require.resolve(path.join(packageName, 'package.json'), {
      paths: searchPaths
    });
    return path.dirname(manifestPath);
  } catch {
    return null;
  }
}

function parsePackageName(input) {
  if (input == null) {
    return null;
  }
  const trimmed = String(input).trim();
  if (!trimmed || trimmed.startsWith('.') || trimmed.startsWith('/')) {
    return null;
  }
  const normalized = trimmed.replace(/\\/g, '/');
  const segments = normalized
    .split('/')
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0 && segment !== '.' && segment !== '..');
  if (segments.length === 0) {
    return null;
  }
  if (normalized.startsWith('@')) {
    if (segments.length !== 2) {
      throw new Error(`Package name '${normalized}' must refer to a single package (nested paths are not supported).`);
    }
    return `${segments[0]}/${segments[1]}`;
  }
  if (segments.length !== 1) {
    throw new Error(`Package name '${normalized}' must refer to a single package (nested paths are not supported).`);
  }
  return segments[0];
}

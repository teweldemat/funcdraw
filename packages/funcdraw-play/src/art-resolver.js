'use strict';

const fs = require('fs');
const path = require('path');
function createArtResolver(rootDir, artFolderName = 'art') {
  const artRoot = path.resolve(rootDir, artFolderName);
  if (!fs.existsSync(artRoot) || !fs.statSync(artRoot).isDirectory()) {
    return null;
  }

  const resolver = {
    listChildren(pathSegments = []) {
      const dirPath = resolveDirectory(artRoot, pathSegments);
      if (!dirPath) {
        return [];
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
      return names;
    },
    getExpression(pathSegments = []) {
      const file = resolveFile(artRoot, pathSegments);
      if (!file) {
        return null;
      }
      if (file.ext === '.fs') {
        const text = fs.readFileSync(file.fullPath, 'utf8');
        return {
          expression: text,
          language: 'funcscript'
        };
      }
      if (file.ext === '.js') {
        return loadJsExpression(file.fullPath);
      }
      return null;
    },
    import(name) {
      if (name == null) {
        return null;
      }
      const segments = String(name)
        .split(/[\\/]+/)
        .map((segment) => segment.trim())
        .filter(Boolean);
      if (segments.length === 0) {
        return null;
      }
      return createScopedResolver(resolver, segments);
    }
  };

  return {
    resolver,
    watchPath: artRoot
  };
}

function resolveDirectory(root, segments) {
  const target = path.join(root, ...segments);
  if (!fs.existsSync(target)) {
    return null;
  }
  const stat = fs.statSync(target);
  return stat.isDirectory() ? target : null;
}

function resolveFile(root, segments) {
  if (!Array.isArray(segments) || segments.length === 0) {
    return null;
  }
  const dirSegments = segments.slice(0, -1);
  const baseName = segments[segments.length - 1];
  const dirPath = path.join(root, ...dirSegments);
  if (!fs.existsSync(dirPath) || !fs.statSync(dirPath).isDirectory()) {
    return null;
  }
  const fsPath = path.join(dirPath, `${baseName}.fs`);
  if (fs.existsSync(fsPath) && fs.statSync(fsPath).isFile()) {
    return { fullPath: fsPath, ext: '.fs' };
  }
  const jsPath = path.join(dirPath, `${baseName}.js`);
  if (fs.existsSync(jsPath) && fs.statSync(jsPath).isFile()) {
    return { fullPath: jsPath, ext: '.js' };
  }
  return null;
}

function stripExtension(name) {
  return name.replace(/\.[^.]+$/, '');
}

function createScopedResolver(baseResolver, prefixSegments) {
  const basePath = Array.isArray(prefixSegments) ? prefixSegments : [];
  return {
    listChildren(pathSegments = []) {
      const segments = basePath.concat(Array.isArray(pathSegments) ? pathSegments : []);
      return baseResolver.listChildren(segments);
    },
    getExpression(pathSegments = []) {
      const segments = basePath.concat(Array.isArray(pathSegments) ? pathSegments : []);
      return baseResolver.getExpression(segments);
    },
    import(name) {
      if (name == null) {
        return null;
      }
      const extra = String(name)
        .split(/[\\/]+/)
        .map((segment) => segment.trim())
        .filter(Boolean);
      if (extra.length === 0) {
        return null;
      }
      return createScopedResolver(baseResolver, basePath.concat(extra));
    }
  };
}

function loadJsExpression(filePath) {
  delete require.cache[filePath];
  let mod = require(filePath);
  if (mod && typeof mod === 'object' && 'default' in mod) {
    mod = mod.default;
  }
  if (typeof mod === 'function') {
    mod = mod();
  }
  if (typeof mod === 'string') {
    return {
      expression: mod,
      language: 'funcscript'
    };
  }
  if (mod && typeof mod === 'object') {
    if (typeof mod.expression === 'string') {
      return {
        expression: mod.expression,
        language: mod.language || 'funcscript'
      };
    }
    if (typeof mod.getExpression === 'function') {
      const result = mod.getExpression();
      if (typeof result === 'string') {
        return {
          expression: result,
          language: 'funcscript'
        };
      }
      if (result && typeof result === 'object' && typeof result.expression === 'string') {
        return {
          expression: result.expression,
          language: result.language || 'funcscript'
        };
      }
    }
  }
  throw new Error(`Unsupported export in ${filePath}`);
}

module.exports = {
  createArtResolver
};

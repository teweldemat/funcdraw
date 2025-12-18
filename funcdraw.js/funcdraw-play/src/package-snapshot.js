'use strict';

function buildResolverSnapshot(resolver) {
  if (!resolver || typeof resolver.listChildren !== 'function' || typeof resolver.getExpression !== 'function') {
    throw new Error('buildResolverSnapshot: expected a FuncScript package resolver');
  }

  const children = {};
  const expressions = {};
  const visited = new Set();

  const normalizePathKey = (segments) => (Array.isArray(segments) && segments.length > 0 ? segments.join('/') : '');
  const recordExpression = (key, expressionDescriptor) => {
    if (expressionDescriptor === null || expressionDescriptor === undefined) {
      return;
    }
    if (typeof expressionDescriptor === 'string') {
      expressions[key] = { expression: expressionDescriptor, language: 'funcscript' };
      return;
    }
    if (typeof expressionDescriptor === 'object') {
      const expression = expressionDescriptor.expression ?? expressionDescriptor.code ?? expressionDescriptor.Expression;
      if (expression === null || expression === undefined) {
        throw new Error(`Snapshot expression missing source at '${key || '<root>'}'`);
      }
      const language = expressionDescriptor.language ?? expressionDescriptor.lang ?? expressionDescriptor.Language ?? 'funcscript';
      expressions[key] = { expression, language };
      return;
    }
    throw new Error(`Snapshot expression must be string/object at '${key || '<root>'}'`);
  };

  const visit = (segments) => {
    const key = normalizePathKey(segments);
    if (visited.has(key)) {
      return;
    }
    visited.add(key);

    const listed = resolver.listChildren(segments) || [];
    const names = [];
    for (const entry of listed) {
      if (typeof entry === 'string') {
        if (entry.trim().length > 0) {
          names.push(entry.trim());
        }
        continue;
      }
      if (entry && typeof entry === 'object' && typeof entry.name === 'string') {
        if (entry.name.trim().length > 0) {
          names.push(entry.name.trim());
        }
      }
    }
    children[key] = names;

    recordExpression(key, resolver.getExpression(segments));

    for (const child of names) {
      visit(segments.concat([child]));
    }
  };

  visit([]);

  return {
    children,
    expressions
  };
}

function collectPackageNamesFromSnapshot(snapshot) {
  const expressions = snapshot && snapshot.expressions ? snapshot.expressions : {};
  const names = new Set();
  const pattern = /\bpackage\s*\(\s*(['"])([^'"]+)\1\s*\)/g;

  for (const descriptor of Object.values(expressions)) {
    const source = descriptor && descriptor.expression != null ? String(descriptor.expression) : '';
    let match;
    while ((match = pattern.exec(source))) {
      const name = match[2] ? match[2].trim() : '';
      if (name) {
        names.add(name);
      }
    }
  }

  return names;
}

function buildBootstrapPayload({ resolver, sourceDescription }) {
  const rootSnapshot = buildResolverSnapshot(resolver);
  const packages = {};

  const queue = [];
  const seen = new Set();
  for (const name of collectPackageNamesFromSnapshot(rootSnapshot)) {
    seen.add(name);
    queue.push(name);
  }

  const resolvePackage = (baseResolver, packageName) => {
    if (!baseResolver || typeof baseResolver.package !== 'function') {
      throw new Error(`Package '${packageName}' requested but resolver does not implement package()`);
    }
    return baseResolver.package(packageName);
  };

  while (queue.length > 0) {
    const packageName = queue.shift();
    const pkgResolver = resolvePackage(resolver, packageName);
    if (!pkgResolver) {
      throw new Error(`Package '${packageName}' could not be resolved`);
    }
    const pkgSnapshot = buildResolverSnapshot(pkgResolver);
    packages[packageName] = pkgSnapshot;

    for (const nestedName of collectPackageNamesFromSnapshot(pkgSnapshot)) {
      if (!seen.has(nestedName)) {
        seen.add(nestedName);
        queue.push(nestedName);
      }
    }
  }

  return {
    mode: 'browser',
    sourceDescription: sourceDescription || null,
    snapshot: {
      root: rootSnapshot,
      packages
    }
  };
}

module.exports = {
  buildResolverSnapshot,
  buildBootstrapPayload
};


'use strict';

const path = require('path');
const { createResolverFromExpression } = require('./resolver');
const { createArtResolver } = require('./art-resolver');

const SAMPLE_EXPRESSION = `
{
  view:{
    left:0;
    bottom:0;
    right:24;
    top:24;
  };
  graphics:[
    {
      type:"line";
      from:[0,0];
      to:[24,24];
      stroke:"#38bdf8";
      width:0.5;
    },
    {
      type:"text";
      text:"FuncDraw";
      position:[4,6];
      fontSize:4;
      color:"#e2e8f0";
    }
  ];
}
`;

async function loadUserConfig(cwd) {
  const artResolver = createArtResolver(cwd);
  if (artResolver) {
    const entrySegments = selectDefaultEntry(artResolver.resolver);
    if (entrySegments) {
      const relativeArtPath = path.relative(cwd, artResolver.watchPath) || artResolver.watchPath;
      const entryLabel = entrySegments.join('/');
      return {
        resolver: createEntryResolver(artResolver.resolver, entrySegments),
        options: {},
        configPath: null,
        watchPaths: [artResolver.watchPath],
        sourceDescription: `art directory (${relativeArtPath}) · entry ${entryLabel}`
      };
    }
  }
  return {
    resolver: createResolverFromExpression(SAMPLE_EXPRESSION),
    options: {},
    configPath: null,
    watchPaths: [],
    sourceDescription: 'inline sample FuncScript expression'
  };
}

module.exports = {
  loadUserConfig,
  SAMPLE_EXPRESSION
};

function selectDefaultEntry(resolver) {
  const rootChildren = safeListChildren(resolver, []);
  const preferred = ['scene', 'index', 'main'];
  for (const name of preferred) {
    if (rootChildren.includes(name) && resolver.getExpression([name])) {
      return [name];
    }
  }
  for (const name of rootChildren) {
    if (resolver.getExpression([name])) {
      return [name];
    }
  }
  return null;
}

function safeListChildren(resolver, segments) {
  try {
    return resolver.listChildren(segments) || [];
  } catch {
    return [];
  }
}

function createEntryResolver(baseResolver, entrySegments) {
  const normalizedEntry = Array.isArray(entrySegments)
    ? entrySegments
        .map((segment) => (typeof segment === 'string' ? segment.trim() : segment))
        .filter((segment) => typeof segment === 'string' && segment.length > 0)
    : [];
  if (normalizedEntry.length === 0) {
    return baseResolver;
  }

  const entryAlias = 'eval';
  const entryAliasLower = entryAlias.toLowerCase();
  const entryImportKey = normalizedEntry.join('/');

  const isAliasPath = (segments) =>
    Array.isArray(segments) &&
    segments.length === 1 &&
    typeof segments[0] === 'string' &&
    segments[0].toLowerCase() === entryAliasLower;

  return {
    listChildren(pathSegments = []) {
      if (!Array.isArray(pathSegments) || pathSegments.length === 0) {
        const baseChildren = safeListChildren(baseResolver, []);
        const names = new Set(baseChildren);
        names.add(entryAlias);
        return Array.from(names);
      }
      if (isAliasPath(pathSegments)) {
        return [];
      }
      const children = baseResolver.listChildren(pathSegments);
      return Array.isArray(children) ? children : [];
    },
    getExpression(pathSegments = []) {
      if (!Array.isArray(pathSegments) || pathSegments.length === 0) {
        return null;
      }
      if (isAliasPath(pathSegments)) {
        return baseResolver.getExpression(normalizedEntry);
      }
      return baseResolver.getExpression(pathSegments);
    },
    import(name) {
      if (name == null || (typeof name === 'string' && name.trim() === '')) {
        return baseResolver.import(entryImportKey);
      }
      return baseResolver.import(name);
    }
  };
}

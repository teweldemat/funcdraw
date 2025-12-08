'use strict';

const path = require('path');
const { createResolverFromExpression } = require('./resolver');
const { createArtResolver, clearArtResolverCache } = require('./art-resolver');

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

async function loadUserConfig(cwd, options = {}) {
  clearArtResolverCache();
  const expressionOverride = normalizeExpressionOverride(options.expression);
  const artResolver = createArtResolver(cwd);
  if (artResolver) {
    const relativeArtPath = path.relative(cwd, artResolver.watchPath) || artResolver.watchPath;
    const selectedExpression = expressionOverride || pickDefaultArtExpression(artResolver.resolver);
    const resolver = selectedExpression
      ? wrapResolverWithExpression(artResolver.resolver, selectedExpression)
      : artResolver.resolver;
    return {
      resolver,
      options: {},
      configPath: null,
      watchPaths: [artResolver.watchPath],
      sourceDescription: selectedExpression
        ? `art directory (${relativeArtPath}) via ${selectedExpression}`
        : `art directory (${relativeArtPath})`
    };
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

function normalizeExpressionOverride(value) {
  if (typeof value !== 'string') {
    return null;
  }
  const text = value.trim();
  return text.length > 0 ? text : null;
}

function wrapResolverWithExpression(baseResolver, expressionText) {
  if (!baseResolver || typeof baseResolver.listChildren !== 'function') {
    return baseResolver;
  }
  const expression = expressionText;
  const normalized = {
    listChildren(pathSegments = []) {
      if (!Array.isArray(pathSegments) || pathSegments.length === 0) {
        return ['eval', 'art'];
      }
      if (pathSegments[0] === 'art') {
        return baseResolver.listChildren(pathSegments.slice(1));
      }
      return [];
    },
    getExpression(pathSegments = []) {
      if (pathSegments.length === 1 && pathSegments[0] === 'eval') {
        return {
          expression,
          language: 'funcscript'
        };
      }
      if (pathSegments[0] === 'art') {
        if (pathSegments.length === 1) {
          return null;
        }
        return baseResolver.getExpression(pathSegments.slice(1));
      }
      return null;
    },
    package(name) {
      return typeof baseResolver.package === 'function' ? baseResolver.package(name) : null;
    }
  };
  return normalized;
}

function pickDefaultArtExpression(resolver) {
  const nameMap = collectRootEntries(resolver);
  const preferred = ['scene', 'main', 'eval'];
  for (const target of preferred) {
    const candidate = nameMap.get(target);
    if (candidate && resolver.getExpression([candidate])) {
      return `art.${candidate}`;
    }
  }
  for (const candidate of nameMap.values()) {
    if (resolver.getExpression([candidate])) {
      return `art.${candidate}`;
    }
  }
  return null;
}

function collectRootEntries(resolver) {
  const result = new Map();
  const entries = resolver.listChildren([]);
  for (const entry of entries) {
    const name = extractEntryName(entry);
    if (!name) {
      continue;
    }
    const lower = name.toLowerCase();
    if (!result.has(lower)) {
      result.set(lower, name);
    }
  }
  return result;
}

function extractEntryName(entry) {
  if (entry === null || entry === undefined) {
    return null;
  }
  if (typeof entry === 'string') {
    const trimmed = entry.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  if (typeof entry === 'object' && typeof entry.name === 'string') {
    const trimmed = entry.name.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  return null;
}

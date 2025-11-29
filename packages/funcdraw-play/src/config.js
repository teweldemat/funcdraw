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

async function loadUserConfig(cwd, options = {}) {
  const expressionOverride = normalizeExpressionOverride(options.expression);
  const artResolver = createArtResolver(cwd);
  if (artResolver) {
    const relativeArtPath = path.relative(cwd, artResolver.watchPath) || artResolver.watchPath;
    const resolver = expressionOverride
      ? wrapResolverWithExpression(artResolver.resolver, expressionOverride)
      : artResolver.resolver;
    return {
      resolver,
      options: {},
      configPath: null,
      watchPaths: [artResolver.watchPath],
      sourceDescription: expressionOverride
        ? `art directory (${relativeArtPath}) with --exp override`
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

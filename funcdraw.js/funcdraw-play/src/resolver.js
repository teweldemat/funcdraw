'use strict';

function isResolver(candidate) {
  return (
    candidate &&
    typeof candidate === 'object' &&
    typeof candidate.listChildren === 'function' &&
    typeof candidate.getExpression === 'function'
  );
}

function createResolverFromExpression(expression) {
  const source = typeof expression === 'string' ? expression : '';
  return {
    listChildren(path) {
      if (!Array.isArray(path) || path.length > 0) {
        return [];
      }
      return [];
    },
    getExpression(path) {
      if (Array.isArray(path) && path.length === 0) {
        return {
          expression: source,
          language: 'funcscript'
        };
      }
      return null;
    },
    package() {
      return null;
    }
  };
}

function normalizeResolver(input) {
  if (isResolver(input)) {
    return input;
  }
  if (typeof input === 'string') {
    return createResolverFromExpression(input);
  }
  return null;
}

module.exports = {
  isResolver,
  createResolverFromExpression,
  normalizeResolver
};

'use strict';

const BUILT_IN_PRIMITIVES = new Set([
  'line',
  'rect',
  'rectangle',
  'circle',
  'ellipse',
  'polygon',
  'polyline',
  'path',
  'text',
  'transform',
  'group',
  'debug'
]);

function isBuiltIn(typeName) {
  if (!typeName) {
    return false;
  }
  return BUILT_IN_PRIMITIVES.has(typeName.toLowerCase());
}

module.exports = {
  isBuiltIn
};

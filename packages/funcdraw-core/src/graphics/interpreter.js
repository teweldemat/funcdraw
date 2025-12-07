'use strict';

const { isPlainObject, omitKeys } = require('../utils');
const { isBuiltIn } = require('./primitive-types');
const { createStepFunctionWrapper } = require('./stepper');

function interpretGraphics({ plainRoot, engine, providerFactory, converter }) {
  const warnings = [];
  if (plainRoot == null) {
    return {
      graphics: [],
      step: null,
      warnings,
      raw: plainRoot,
      view: null
    };
  }

  const { content, metadata, view } = extractContentRoot(plainRoot, warnings);
  const graphicsTree = normalizeNode(content, warnings);
  const step = metadata.step && typeof metadata.step.evaluate === 'function'
    ? createStepFunctionWrapper(metadata.step, { engine, providerFactory, converter })
    : null;

  const graphics = Array.isArray(graphicsTree) ? graphicsTree : graphicsTree ? [graphicsTree] : [];

  return {
    graphics,
    step,
    warnings,
    view: view ?? null
  };
}

function extractContentRoot(value, warnings) {
  if (!isPlainObject(value)) {
    return { content: value, metadata: {}, view: null };
  }
  const metadata = {};
  let view = value.view ?? null;
  if (value.step) {
    metadata.step = value.step;
  }
  if (view !== null && view !== undefined) {
    if (value.graphics === undefined) {
      warnings.push('View specified without graphics payload.');
      return { content: [], metadata, view };
    }
    return {
      content: value.graphics,
      metadata,
      view
    };
  }
  if (value.graphics !== undefined) {
    return {
      content: value.graphics,
      metadata,
      view: null
    };
  }
  return {
    content: omitKeys(value, ['step', 'view']),
    metadata,
    view: null
  };
}

function normalizeNode(node, warnings) {
  if (node == null) {
    return null;
  }
  if (Array.isArray(node)) {
    const normalizedList = [];
    node.forEach((item) => {
      const normalized = normalizeNode(item, warnings);
      if (normalized != null) {
        normalizedList.push(normalized);
      }
    });
    return normalizedList;
  }
  if (!isPlainObject(node)) {
    warnings.push(`Skipping unsupported value '${node}'`);
    return null;
  }
  if (node.graphics && !node.type) {
    return normalizeNode(node.graphics, warnings);
  }
  if (!node.type) {
    warnings.push('Skipping object without type or graphics information');
    return null;
  }
  const typeName = String(node.type).trim();
  if (!typeName) {
    warnings.push('Skipping primitive with empty type');
    return null;
  }
  const lowerType = typeName.toLowerCase();

  if (isBuiltIn(lowerType)) {
    const normalized = {
      ...node,
      type: lowerType
    };
    applyPrimitiveDefaults(normalized, lowerType);
    return normalized;
  }

  if (node.graphics) {
    const normalizedGraphics = normalizeNode(node.graphics, warnings);
    if (!normalizedGraphics || (Array.isArray(normalizedGraphics) && normalizedGraphics.length === 0)) {
      warnings.push(`Custom primitive '${typeName}' is missing graphics primitives`);
      return null;
    }
    return {
      type: 'custom',
      name: typeName,
      graphics: Array.isArray(normalizedGraphics) ? normalizedGraphics : [normalizedGraphics],
      props: omitKeys(node, ['type', 'graphics'])
    };
  }

  warnings.push(`Unknown primitive type '${typeName}' without nested graphics`);
  return null;
}

module.exports = {
  interpretGraphics
};

const DEFAULT_STROKE = '#38bdf8';
const STROKED_TYPES = new Set(['line', 'rect', 'rectangle', 'circle', 'ellipse', 'polygon', 'polyline', 'path']);

function applyPrimitiveDefaults(node, type) {
  if (STROKED_TYPES.has(type) && !node.stroke) {
    node.stroke = DEFAULT_STROKE;
  }
}

'use strict';

const { isBuiltIn } = require('./primitive-types');
const { createStepFunctionWrapper } = require('./stepper');

function interpretGraphics({ typedRoot, engine, providerFactory, converter }) {
  const warnings = [];
  if (typedRoot == null) {
    return {
      graphics: [],
      step: null,
      warnings,
      view: null
    };
  }

  const extraction = extractContentRootTyped(typedRoot, warnings, engine, converter);
  const content = extraction.content;
  const metadata = extraction.metadata || {};
  const view = extraction.view;

  const contentPath = extraction.contentPath || [];
  const viewPath = extraction.viewPath || [];
  const graphicsTree = normalizeTypedNode(content, warnings, { engine, converter }, contentPath);
  const graphics = Array.isArray(graphicsTree) ? graphicsTree : graphicsTree ? [graphicsTree] : [];
  const plainView = view ? converter.toPlain(view, viewPath) : null;
  const stepValue = metadata.step && engine.valueOf ? engine.valueOf(metadata.step) : metadata.step;
  const step = stepValue && typeof stepValue.evaluate === 'function'
    ? createStepFunctionWrapper(stepValue, {
        engine,
        providerFactory,
        converter
      })
    : null;

  return {
    graphics,
    step,
    warnings,
    view: plainView ?? null
  };
}

function extractContentRootTyped(typed, warnings, engine, converter) {
  const { assertTyped, typeOf, FSDataType, valueOf } = engine;
  const root = assertTyped ? assertTyped(typed) : typed;
  const rootType = typeOf(root);
  if (rootType !== FSDataType.KeyValueCollection) {
    return { content: typed, metadata: {}, view: null, contentPath: [] };
  }

  const kvc = valueOf(root);
  const { map } = buildKvcEntryMap(kvc);
  const logAccess = converter && typeof converter.logAccess === 'function' ? converter.logAccess : null;

  const viewEntry = map.get('view');
  const graphicsEntry = map.get('graphics');
  const stepEntry = map.get('step');

  if (logAccess && viewEntry) {
    logAccess([], viewEntry.key, viewEntry.value);
  }
  if (logAccess && graphicsEntry) {
    logAccess([], graphicsEntry.key, graphicsEntry.value);
  }
  if (logAccess && stepEntry) {
    logAccess([], stepEntry.key, stepEntry.value);
  }

  const view = viewEntry ? viewEntry.value : null;
  const graphics = graphicsEntry ? graphicsEntry.value : null;
  const step = stepEntry ? stepEntry.value : null;

  if (view !== null && view !== undefined) {
    if (graphics === undefined || graphics === null) {
      warnings.push('View specified without graphics payload.');
      return { content: null, metadata: { step }, view, viewPath: ['view'] };
    }
    return {
      content: graphics,
      metadata: { step },
      view,
      contentPath: ['graphics'],
      viewPath: ['view']
    };
  }

  if (graphics !== undefined && graphics !== null) {
    return {
      content: graphics,
      metadata: { step },
      view: null,
      contentPath: ['graphics']
    };
  }

  return { content: typed, metadata: { step }, view: null, contentPath: [] };
}

function normalizeTypedNode(value, warnings, context, path = []) {
  if (value === null || value === undefined) {
    return null;
  }
  const { engine } = context;
  const { assertTyped, typeOf, valueOf, FSDataType } = engine;
  let typed;
  try {
    typed = assertTyped ? assertTyped(value) : value;
  } catch {
    warnings.push(`Skipping unsupported value '${formatUnsupportedValue(value)}'`);
    return null;
  }
  const dataType = typeOf(typed);

  if (dataType === FSDataType.List) {
    return normalizeTypedList(typed, warnings, context, path);
  }
  if (dataType === FSDataType.KeyValueCollection) {
    return normalizeTypedKvc(typed, warnings, context, path);
  }

  warnings.push(`Skipping unsupported value '${formatUnsupportedValue(valueOf ? valueOf(typed) : typed)}'`);
  return null;
}

function normalizeTypedList(typedList, warnings, context, path) {
  const { engine } = context;
  const list = engine.valueOf(typedList);
  const normalizedList = [];
  let index = 0;
  for (const item of list) {
    const normalized = normalizeTypedNode(item, warnings, context, path.concat(index));
    if (normalized != null) {
      normalizedList.push(normalized);
    }
    index += 1;
  }
  return normalizedList;
}

function normalizeTypedKvc(typedKvc, warnings, context, path) {
  const { engine, converter } = context;
  const { valueOf } = engine;
  const kvc = valueOf(typedKvc);
  const { entries, map } = buildKvcEntryMap(kvc);
  const logAccess = converter && typeof converter.logAccess === 'function' ? converter.logAccess : null;

  const typeEntry = map.get('type');
  const graphicsEntry = map.get('graphics');

  if (!typeEntry && graphicsEntry) {
    if (logAccess) {
      logAccess(path, graphicsEntry.key, graphicsEntry.value);
    }
    return normalizeTypedNode(graphicsEntry.value, warnings, context, path.concat(graphicsEntry.key));
  }

  if (!typeEntry) {
    warnings.push('Skipping object without type or graphics information');
    return null;
  }

  if (logAccess) {
    logAccess(path, typeEntry.key, typeEntry.value);
  }
  const typeText = convertTypedValue(typeEntry.value, context, path.concat(typeEntry.key));
  const typeName = typeText != null ? String(typeText).trim() : '';
  if (!typeName) {
    warnings.push('Skipping primitive with empty type');
    return null;
  }
  const lowerType = typeName.toLowerCase();

  let normalizedGraphics = null;
  if (graphicsEntry && graphicsEntry.value !== undefined && graphicsEntry.value !== null) {
    if (logAccess) {
      logAccess(path, graphicsEntry.key, graphicsEntry.value);
    }
    normalizedGraphics = normalizeTypedNode(
      graphicsEntry.value,
      warnings,
      context,
      path.concat(graphicsEntry.key)
    );
  }

  if (isBuiltIn(lowerType)) {
    const props = collectKvcProperties(entries, ['type', 'graphics'], context, path);
    if (normalizedGraphics !== null && normalizedGraphics !== undefined) {
      props.graphics = normalizedGraphics;
    }
    const normalized = {
      ...props,
      type: lowerType
    };
    applyPrimitiveDefaults(normalized, lowerType);
    return normalized;
  }

  if (normalizedGraphics !== null && normalizedGraphics !== undefined) {
    if (Array.isArray(normalizedGraphics) && normalizedGraphics.length === 0) {
      warnings.push(`Custom primitive '${typeName}' is missing graphics primitives`);
      return null;
    }
    const props = collectKvcProperties(entries, ['type', 'graphics'], context, path);
    return {
      type: 'custom',
      name: typeName,
      graphics: Array.isArray(normalizedGraphics) ? normalizedGraphics : [normalizedGraphics],
      props
    };
  }

  warnings.push(`Unknown primitive type '${typeName}' without nested graphics`);
  return null;
}

function collectKvcProperties(entries, excludedKeys, context, path) {
  const result = {};
  const exclude = new Set((excludedKeys || []).map((key) => String(key).toLowerCase()));
  const logAccess = context.converter && typeof context.converter.logAccess === 'function' ? context.converter.logAccess : null;

  for (const [rawKey, rawValue] of entries) {
    const lower = String(rawKey).toLowerCase();
    if (exclude.has(lower)) {
      continue;
    }
    if (logAccess) {
      logAccess(path, rawKey, rawValue);
    }
    result[rawKey] = convertTypedValue(rawValue, context, path.concat(rawKey));
  }

  return result;
}

function convertTypedValue(typedValue, context, path) {
  const { converter, engine } = context;
  if (converter && typeof converter.toPlain === 'function') {
    return converter.toPlain(typedValue, path);
  }
  return engine && typeof engine.valueOf === 'function' ? engine.valueOf(typedValue) : typedValue;
}

function buildKvcEntryMap(kvc) {
  const entries = typeof kvc.getAll === 'function' ? kvc.getAll() : [];
  const map = new Map();
  for (const [key, value] of entries) {
    map.set(String(key).toLowerCase(), { key, value });
  }
  return { entries, map };
}

function formatUnsupportedValue(value) {
  if (value === null || value === undefined) {
    return String(value);
  }
  if (typeof value === 'object' && value.constructor && value.constructor.name) {
    return value.constructor.name;
  }
  return String(value);
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

'use strict';

const funcscript = require('@tewelde/funcscript');
const { createFdContext } = require('./fd-context');
const { createValueConverter } = require('./typed-value');
const { interpretGraphics } = require('./graphics/interpreter');
const { loadFont } = require('./glyphs/font-loader');
const { createFontMeasure } = require('./glyphs/text-metrics');
const { renderSvg } = require('./output/svg-renderer');

function ensureResolver(resolver) {
  if (!resolver || typeof resolver.listChildren !== 'function' || typeof resolver.getExpression !== 'function') {
    throw new Error('FuncDraw requires a valid FuncScript package resolver');
  }
  return resolver;
}

function resolveOutputs(option) {
  if (!option) {
    return new Set(['raw']);
  }
  if (typeof option === 'string') {
    return new Set([option]);
  }
  if (Array.isArray(option) && option.length > 0) {
    return new Set(option);
  }
  return new Set(['raw']);
}

function createFdValue(engine, context) {
  const { SimpleKeyValueCollection } = engine;
  const entries = Object.entries(context).map(([key, value]) => [key, engine.normalize(value)]);
  const collection = new SimpleKeyValueCollection(null, entries);
  return engine.normalize(collection);
}

function loadGraphics(resolver, options = {}) {
  const engine = options.engine || funcscript;
  ensureResolver(resolver);
  const outputs = resolveOutputs(options.output);
  const fontInput = options.font || (options.fd ? options.fd.font : null);
  const font = loadFont(fontInput);
  const fontMeasure = createFontMeasure(font, options.textMetrics || {});
  const fdOptions = options.fd || {};
  const measureText = typeof fdOptions.measureText === 'function' ? fdOptions.measureText : fontMeasure;
  const fdContext = createFdContext({
    measureText,
    expose: fdOptions.expose
  });
  const typedFd = createFdValue(engine, fdContext);
  const valueHookEntries = createValueHookEntries(options.valueHooks);
  const converter = createValueConverter(engine, { logger: options.dumpLogger || null });
  const traceCollector = createTraceCollector(options.trace, converter, engine);
  const providerFactory = createProviderFactory(engine, typedFd, valueHookEntries);
  const provider = providerFactory();
  const traceHook = traceCollector ? traceCollector.hook : null;
  const traceEntryHook = traceCollector ? traceCollector.entryHook : null;
  const typedRoot = engine.loadPackage(resolver, provider, traceHook, traceEntryHook);
  const interpretation = interpretGraphics({
    typedRoot,
    engine,
    providerFactory,
    converter
  });

  const result = {};
  result.step = interpretation.step;
  result.view = interpretation.view;
  result.warnings = interpretation.warnings;
  if (outputs.has('raw')) {
    result.raw = interpretation;
  }
  if (outputs.has('svg')) {
    result.svg = renderSvg(interpretation, { font, measureText });
  }
  if (valueHookEntries) {
    result.valueHooks = summarizeValueHookUsage(valueHookEntries);
  }
  if (traceCollector) {
    result.trace = traceCollector.export();
  }
  return result;
}

function createProviderFactory(engine, typedFd, valueHooks) {
  if (!valueHooks) {
    return () => new engine.DefaultFsDataProvider({ fd: typedFd });
  }
  class ValueHookProvider extends engine.DefaultFsDataProvider {
    constructor(initialValues) {
      super(initialValues);
    }

    get(name) {
      const entry = resolveHookEntry(valueHooks, name);
      if (entry) {
        entry.used = true;
        if (!entry.hasValue) {
          entry.value = normalizeHookValue(engine, entry.hook());
          entry.hasValue = true;
        }
        return entry.value;
      }
      return super.get(name);
    }

    isDefined(name) {
      if (resolveHookEntry(valueHooks, name)) {
        return true;
      }
      return super.isDefined(name);
    }
  }

  return () => new ValueHookProvider({ fd: typedFd });
}

function resolveHookEntry(valueHooks, name) {
  if (!valueHooks || !name) {
    return null;
  }
  const key = String(name).toLowerCase();
  return valueHooks.get(key) || null;
}

function createValueHookEntries(option) {
  if (!option) {
    return null;
  }
  const entries = Array.isArray(option)
    ? option
    : typeof option === 'object'
      ? Object.entries(option)
      : null;
  if (!entries || entries.length === 0) {
    return null;
  }
  const hooks = new Map();
  for (const entry of entries) {
    const normalized = normalizeHookDescriptor(entry);
    if (!normalized) {
      continue;
    }
    hooks.set(normalized.key, {
      name: normalized.name,
      hook: normalized.hook,
      used: false,
      hasValue: false,
      value: null
    });
  }
  return hooks.size > 0 ? hooks : null;
}

function normalizeHookDescriptor(entry) {
  if (!entry) {
    return null;
  }
  let name = null;
  let hook = null;
  if (Array.isArray(entry) && entry.length >= 2) {
    [name, hook] = entry;
  } else if (typeof entry === 'object') {
    name = entry.name;
    hook = entry.hook || entry.value || entry.fn;
  }
  const normalizedName = typeof name === 'string' ? name.trim() : name != null ? String(name).trim() : '';
  if (!normalizedName || typeof hook !== 'function') {
    return null;
  }
  return {
    name: normalizedName,
    key: normalizedName.toLowerCase(),
    hook
  };
}

function summarizeValueHookUsage(valueHooks) {
  if (!valueHooks || valueHooks.size === 0) {
    return null;
  }
  const summary = {};
  for (const entry of valueHooks.values()) {
    summary[entry.name] = {
      used: Boolean(entry.used)
    };
  }
  return summary;
}

function normalizeHookValue(engine, value) {
  const assertTyped = engine.assertTyped || funcscript.assertTyped;
  try {
    return assertTyped(value);
  } catch {
    // fall through to normalize the plain structure
  }
  if (Array.isArray(value)) {
    const typedItems = value.map((item) => normalizeHookValue(engine, item));
    const listClass = engine.ArrayFsList || funcscript.ArrayFsList;
    if (typeof listClass === 'function') {
      const listInstance = new listClass(typedItems);
      return engine.normalize(listInstance);
    }
    throw new Error('Array value hooks require ArrayFsList support');
  }
  if (value && typeof value === 'object') {
    const simpleKvcClass = engine.SimpleKeyValueCollection || funcscript.SimpleKeyValueCollection;
    if (value instanceof simpleKvcClass) {
      return engine.normalize(value);
    }
    const collectionEntries = Object.entries(value).map(([key, inner]) => [
      key,
      normalizeHookValue(engine, inner)
    ]);
    const collection = new simpleKvcClass(collectionEntries);
    return engine.normalize(collection);
  }
  return engine.normalize(value);
}

function createTraceCollector(option, converter, engine) {
  const normalized = normalizeTraceOption(option);
  if (!normalized) {
    return null;
  }

  const filter = createTraceFilter(normalized.filter);
  const formatResult = createTraceResultFormatter(engine, converter);
  const root = createTraceNode('(root)', null, formatResult);
  const stack = [root];

  const entryHook = (path, info) => {
    const node = createTraceNode(path, info, formatResult);
    stack.push(node);
    return node;
  };
  entryHook.__fsStepInto = normalized.stepInto;

  const hook = (path, info, entryState) => {
    if (normalized.userHook) {
      try {
        normalized.userHook(path, info);
      } catch {
        // ignore user trace hook errors
      }
    }
    try {
      if (entryState) {
        stack.pop();
      }
      const node = entryState || stack.pop() || createTraceNode(path, info, formatResult);
      applyTraceInfo(node, info, formatResult);
      const parent = stack[stack.length - 1];
      parent.children.push(node);
      return node;
    } catch {
      // ignore trace serialization errors
    }
  };
  hook.__fsStepInto = normalized.stepInto;

  return {
    hook,
    entryHook,
    stepInto: normalized.stepInto,
    export() {
      const exported = root.children.map(cloneTraceNode);
      return filter ? filterTraceNodes(exported, filter) : exported;
    }
  };
}

function createTraceNode(path, info, formatResult) {
  const node = {
    path: formatTracePath(path),
    children: []
  };
  if (info) {
    applyTraceInfo(node, info, formatResult);
  }
  return node;
}

function applyTraceInfo(target, info, formatResult) {
  if (!info || typeof info !== 'object') {
    return;
  }

  const setIfFinite = (key, value) => {
    const num = Number(value);
    if (Number.isFinite(num)) {
      target[key] = num;
    }
  };

  setIfFinite('startLine', info.startLine);
  setIfFinite('startColumn', info.startColumn);
  setIfFinite('endLine', info.endLine);
  setIfFinite('endColumn', info.endColumn);
  setIfFinite('startIndex', info.startIndex);
  setIfFinite('endIndex', info.endIndex);

  if (info.snippet != null) {
    target.snippet = String(info.snippet);
  }
  if (Object.prototype.hasOwnProperty.call(info, 'result')) {
    const formatted = formatResult ? formatResult(info.result) : null;
    if (formatted) {
      target.resultKind = formatted.kind;
      delete target.resultPreview;
      if (formatted.preview != null) {
        target.resultPreview = formatted.preview;
      }
    }
  }
}

function cloneTraceNode(node) {
  return {
    ...node,
    children: Array.isArray(node.children) ? node.children.map(cloneTraceNode) : []
  };
}

function filterTraceNodes(nodes, filter) {
  const result = [];
  for (const node of nodes) {
    const filteredChildren = filterTraceNodes(node.children || [], filter);
    if (filter(node) || filteredChildren.length > 0) {
      result.push({
        ...node,
        children: filteredChildren
      });
    }
  }
  return result;
}

function normalizeTraceOption(option) {
  if (!option) {
    return null;
  }
  if (option === true) {
    return { enabled: true, stepInto: false, filter: null, userHook: null };
  }
  if (typeof option === 'function') {
    return { enabled: true, stepInto: false, filter: null, userHook: option };
  }
  if (Array.isArray(option)) {
    const cleaned = option.filter(
      (item) => item !== undefined && item !== null && String(item).trim() !== ''
    );
    if (cleaned.length === 0) {
      return option.length === 0 ? { enabled: true, stepInto: false, filter: null, userHook: null } : null;
    }
    const first = cleaned.length > 0 ? String(cleaned[0]).toLowerCase() : null;
    const stepInto = first === 'step-into';
    const filter = stepInto && cleaned.length > 1 ? String(cleaned[1]) : null;
    return { enabled: true, stepInto, filter, userHook: null };
  }
  if (typeof option === 'object') {
    return {
      enabled: option.enabled !== false,
      stepInto: Boolean(option.stepInto),
      filter: option.filter != null ? String(option.filter) : null,
      userHook: typeof option.hook === 'function' ? option.hook : null
    };
  }
  return { enabled: Boolean(option), stepInto: false, filter: null, userHook: null };
}

function createTraceFilter(filter) {
  if (!filter) {
    return null;
  }
  const needle = String(filter).toLowerCase();
  return (entry) => {
    const haystack = `${entry.path || ''} ${entry.snippet || ''}`.toLowerCase();
    return haystack.includes(needle);
  };
}

function createTraceResultFormatter(engine, converter) {
  return (value) => {
    const kind = detectResultKind(value, engine);
    if (kind !== 'atomic') {
      // Only expose result details for atomic values to avoid expensive serialization.
      return { kind };
    }
    return { kind, preview: formatAtomicValue(value) };
  };
}

function formatTracePath(path) {
  if (Array.isArray(path)) {
    return path.join('/');
  }
  if (path === null || path === undefined) {
    return '';
  }
  return String(path);
}

function detectResultKind(value, engine) {
  if (value === null || value === undefined) {
    return 'atomic';
  }
  const type = typeof value;
  if (type === 'string' || type === 'number' || type === 'boolean' || type === 'bigint') {
    return 'atomic';
  }
  if (isFsErrorValue(value)) {
    return 'error';
  }
  if (type === 'function') {
    return 'function';
  }
  if (isFsListValue(value, engine)) {
    return 'list';
  }
  if (isKvcValue(value, engine)) {
    return 'kvc';
  }
  if (value && typeof value.evaluate === 'function') {
    return 'function';
  }
  return 'object';
}

function isFsListValue(value, engine) {
  if (!value || typeof value !== 'object') {
    return false;
  }
  if (value.__fsKind === 'FsList') {
    return true;
  }
  if (engine && engine.ArrayFsList && value instanceof engine.ArrayFsList) {
    return true;
  }
  if (engine && engine.FsList && value instanceof engine.FsList) {
    return true;
  }
  return false;
}

function isKvcValue(value, engine) {
  if (!value || typeof value !== 'object') {
    return false;
  }
  if (value.__fsKind === 'KeyValueCollection') {
    return true;
  }
  if (engine && engine.KeyValueCollection && value instanceof engine.KeyValueCollection) {
    return true;
  }
  if (engine && engine.SimpleKeyValueCollection && value instanceof engine.SimpleKeyValueCollection) {
    return true;
  }
  return false;
}

function isFsErrorValue(value) {
  if (!value || typeof value !== 'object') {
    return false;
  }
  if (value.__fsKind === 'FsError') {
    return true;
  }
  if (value.errorType || value.errorMessage || value.errorData) {
    return true;
  }
  if (value.fsError && typeof value.fsError === 'object') {
    return true;
  }
  return false;
}

function formatAtomicValue(value) {
  if (value === undefined) {
    return 'undefined';
  }
  if (value === null) {
    return 'null';
  }
  if (typeof value === 'string') {
    const trimmed = value.trim();
    const max = 120;
    return trimmed.length > max ? `${trimmed.slice(0, max)}...` : trimmed;
  }
  return String(value);
}

module.exports = {
  loadGraphics
};

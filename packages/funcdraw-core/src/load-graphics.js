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
  const providerFactory = createProviderFactory(engine, typedFd, valueHookEntries);
  const provider = providerFactory();
  const typedRoot = engine.loadPackage(resolver, provider);
  const converter = createValueConverter(engine);
  const plainRoot = converter.toPlain(typedRoot);
  const interpretation = interpretGraphics({
    plainRoot,
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
  return result;
}

function createProviderFactory(engine, typedFd, valueHooks) {
  if (!valueHooks) {
    return () => new engine.DefaultFsDataProvider({ fd: typedFd });
  }
  const normalizeValue =
    typeof engine.normalize === 'function' ? engine.normalize.bind(engine) : (value) => value;

  class ValueHookProvider extends engine.DefaultFsDataProvider {
    constructor(initialValues) {
      super(initialValues);
    }

    get(name) {
      const entry = resolveHookEntry(valueHooks, name);
      if (entry) {
        entry.used = true;
        if (!entry.hasValue) {
          entry.value = normalizeValue(entry.hook());
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

module.exports = {
  loadGraphics
};

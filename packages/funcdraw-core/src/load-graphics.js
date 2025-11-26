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
  const providerFactory = () => new engine.DefaultFsDataProvider({ fd: typedFd });
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

  return result;
}

module.exports = {
  loadGraphics
};

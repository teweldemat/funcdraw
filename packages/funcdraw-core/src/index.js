'use strict';

const { loadGraphics } = require('./load-graphics');

function mergeOptions(baseOptions = {}, overrideOptions = {}) {
  if (!overrideOptions || typeof overrideOptions !== 'object') {
    return { ...baseOptions };
  }
  const merged = { ...baseOptions, ...overrideOptions };

  if (baseOptions.fd || overrideOptions.fd) {
    const baseFd = baseOptions.fd || {};
    const overrideFd = overrideOptions.fd || {};
    const baseExpose = baseFd.expose || {};
    const overrideExpose = overrideFd.expose || {};
    merged.fd = {
      ...baseFd,
      ...overrideFd,
      expose: { ...baseExpose, ...overrideExpose }
    };
  }

  if (baseOptions.textMetrics || overrideOptions.textMetrics) {
    merged.textMetrics = {
      ...(baseOptions.textMetrics || {}),
      ...(overrideOptions.textMetrics || {})
    };
  }

  return merged;
}

class FuncDrawExpression {
  constructor(resolver, options = {}) {
    this.resolver = resolver;
    this.options = options;
  }

  evaluate(overrides = {}) {
    const merged = mergeOptions(this.options, overrides);
    return loadGraphics(this.resolver, merged);
  }
}

class FuncDraw {
  constructor(options = {}) {
    this.options = options;
  }

  prepare(resolver, overrides = {}) {
    const merged = mergeOptions(this.options, overrides);
    return new FuncDrawExpression(resolver, merged);
  }

  evaluate(resolver, overrides = {}) {
    return this.prepare(resolver, overrides).evaluate();
  }
}

function createFuncDraw(options = {}) {
  return new FuncDraw(options);
}

function createExpression(resolver, options = {}) {
  return new FuncDrawExpression(resolver, options);
}

module.exports = {
  loadGraphics,
  createFuncDraw,
  FuncDraw,
  FuncDrawExpression,
  createExpression
};

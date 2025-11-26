'use strict';

const { isPlainObject } = require('./utils');

function createFdContext(options = {}) {
  if (typeof options.measureText !== 'function') {
    throw new Error('FuncDraw requires a measureText helper');
  }
  const context = {
    measureText: options.measureText
  };

  if (isPlainObject(options.expose)) {
    Object.assign(context, options.expose);
  }

  return context;
}

module.exports = {
  createFdContext
};

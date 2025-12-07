'use strict';

function createFdContext(options = {}) {
  if (typeof options.measureText !== 'function') {
    throw new Error('FuncDraw requires a measureText helper');
  }
  const context = {
    measureText: options.measureText
  };

  if (options.expose && typeof options.expose === 'object') {
    Object.assign(context, options.expose);
  }

  return context;
}

module.exports = {
  createFdContext
};

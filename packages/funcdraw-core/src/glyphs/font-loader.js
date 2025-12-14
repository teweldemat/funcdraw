'use strict';

const opentype = require('opentype.js');

let cachedDefaultFont = null;

function tryRequire(moduleName) {
  if (typeof require !== 'function') {
    return null;
  }
  try {
    return require(moduleName);
  } catch {
    return null;
  }
}

function isParsedFont(value) {
  return value && typeof value === 'object' && typeof value.getPath === 'function';
}

function tryReadBuffer(source) {
  if (source instanceof ArrayBuffer) {
    return source;
  }
  if (ArrayBuffer.isView(source)) {
    return source.buffer.slice(source.byteOffset, source.byteOffset + source.byteLength);
  }
  if (typeof Buffer !== 'undefined' && Buffer.isBuffer(source)) {
    return source.buffer.slice(source.byteOffset, source.byteOffset + source.byteLength);
  }
  if (typeof source === 'string') {
    const fs = tryRequire('fs');
    const path = tryRequire('path');
    if (!fs || !path) {
      throw new Error('Font path inputs are only supported in Node.js');
    }
    const resolved = path.resolve(source);
    if (!fs.existsSync(resolved)) {
      throw new Error(`Font file '${resolved}' does not exist`);
    }
    const buffer = fs.readFileSync(resolved);
    return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
  }
  if (typeof source === 'object') {
    if (source.buffer && source.buffer instanceof ArrayBuffer) {
      return source.buffer;
    }
    if (source.buffer && ArrayBuffer.isView(source.buffer)) {
      return source.buffer.buffer.slice(source.buffer.byteOffset, source.buffer.byteOffset + source.buffer.byteLength);
    }
    if (source.data) {
      const encoding = typeof source.encoding === 'string' ? source.encoding : 'base64';
      if (typeof Buffer === 'undefined') {
        throw new Error('Font base64 inputs require Buffer support');
      }
      const buffer = Buffer.from(String(source.data), encoding);
      return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
    }
    if (source.path) {
      const fs = tryRequire('fs');
      const path = tryRequire('path');
      if (!fs || !path) {
        throw new Error('Font path inputs are only supported in Node.js');
      }
      const resolved = path.resolve(source.path);
      if (!fs.existsSync(resolved)) {
        throw new Error(`Font file '${resolved}' does not exist`);
      }
      const buffer = fs.readFileSync(resolved);
      return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
    }
  }
  throw new Error('Unsupported font input');
}

function parseFont(buffer) {
  let arrayBuffer;
  if (buffer instanceof ArrayBuffer) {
    arrayBuffer = buffer;
  } else if (ArrayBuffer.isView(buffer)) {
    arrayBuffer = buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
  } else {
    throw new Error('Font buffer must be an ArrayBuffer');
  }
  return opentype.parse(arrayBuffer);
}

function loadDefaultFontBuffer() {
  const fs = tryRequire('fs');
  const path = tryRequire('path');
  if (!fs || !path) {
    throw new Error('Default font loading is only supported in Node.js (provide a font buffer instead).');
  }
  const defaultPath = path.resolve(__dirname, '../../assets/fonts/Inter-Regular.ttf');
  if (!fs.existsSync(defaultPath)) {
    throw new Error(`Default font file '${defaultPath}' does not exist`);
  }
  const buffer = fs.readFileSync(defaultPath);
  return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength);
}

function loadDefaultFont() {
  if (cachedDefaultFont) {
    return cachedDefaultFont;
  }
  const buffer = loadDefaultFontBuffer();
  cachedDefaultFont = parseFont(buffer);
  return cachedDefaultFont;
}

function loadFont(option) {
  if (isParsedFont(option)) {
    return option;
  }
  if (option === null || option === undefined) {
    return loadDefaultFont();
  }
  const buffer = tryReadBuffer(option);
  return parseFont(buffer);
}

module.exports = {
  loadFont
};

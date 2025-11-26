'use strict';

const fs = require('fs');
const path = require('path');
const opentype = require('opentype.js');
const { createDefaultFont } = require('./default-font');

const DEFAULT_FONT_PATH = path.resolve(__dirname, '../../assets/fonts/Inter-Regular.ttf');
let cachedDefaultFont = null;

function tryReadBuffer(source) {
  if (!source) {
    return null;
  }
  if (Buffer.isBuffer(source)) {
    return Buffer.from(source);
  }
  if (typeof source === 'string') {
    const resolved = path.resolve(source);
    if (fs.existsSync(resolved)) {
      return fs.readFileSync(resolved);
    }
    return null;
  }
  if (typeof source === 'object') {
    if (source.buffer && Buffer.isBuffer(source.buffer)) {
      return Buffer.from(source.buffer);
    }
    if (source.buffer && ArrayBuffer.isView(source.buffer)) {
      return Buffer.from(source.buffer.buffer);
    }
    if (source.data) {
      if (Buffer.isBuffer(source.data)) {
        return Buffer.from(source.data);
      }
      const encoding = typeof source.encoding === 'string' ? source.encoding : 'base64';
      return Buffer.from(String(source.data), encoding);
    }
    if (source.path) {
      const resolved = path.resolve(source.path);
      if (fs.existsSync(resolved)) {
        return fs.readFileSync(resolved);
      }
    }
  }
  return null;
}

function parseFont(buffer) {
  if (!buffer) {
    return null;
  }
  try {
    return opentype.parse(buffer);
  } catch {
    return null;
  }
}

function loadDefaultFont() {
  if (cachedDefaultFont) {
    return cachedDefaultFont;
  }
  if (fs.existsSync(DEFAULT_FONT_PATH)) {
    try {
      const buffer = fs.readFileSync(DEFAULT_FONT_PATH);
      const parsed = parseFont(buffer);
      if (parsed) {
        cachedDefaultFont = parsed;
        return cachedDefaultFont;
      }
    } catch {
      // fall back to geometric default
    }
  }
  cachedDefaultFont = createDefaultFont();
  return cachedDefaultFont;
}

function loadFont(option) {
  const buffer = tryReadBuffer(option);
  const parsed = parseFont(buffer);
  if (parsed) {
    return parsed;
  }
  return loadDefaultFont();
}

module.exports = {
  loadFont
};

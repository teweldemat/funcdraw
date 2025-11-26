'use strict';

const { clamp } = require('../utils');

const FONT_SIZE_RANGE = [2, 1024];

function getUnitsPerEm(font) {
  return typeof font.unitsPerEm === 'number' && font.unitsPerEm > 0 ? font.unitsPerEm : 1000;
}

function readAscender(font) {
  if (typeof font.ascender === 'number') {
    return font.ascender;
  }
  return getUnitsPerEm(font) * 0.8;
}

function readDescender(font) {
  if (typeof font.descender === 'number') {
    return font.descender;
  }
  return -getUnitsPerEm(font) * 0.2;
}

function measureLineWidth(font, text, fontSize) {
  if (!text) {
    return 0;
  }
  if (typeof font.getAdvanceWidth === 'function') {
    try {
      return font.getAdvanceWidth(text, fontSize);
    } catch {
      // fall through
    }
  }
  const unitsPerEm = getUnitsPerEm(font);
  const scale = fontSize / unitsPerEm;
  let widthUnits = 0;
  let previousGlyph = null;
  for (const char of text) {
    const glyph = typeof font.charToGlyph === 'function' ? font.charToGlyph(char) : null;
    const advance =
      glyph && typeof glyph.advanceWidth === 'number'
        ? glyph.advanceWidth
        : unitsPerEm * 0.6;
    if (previousGlyph && typeof font.getKerningValue === 'function') {
      const kern = font.getKerningValue(previousGlyph, glyph);
      if (typeof kern === 'number') {
        widthUnits += kern;
      }
    }
    widthUnits += advance;
    previousGlyph = glyph;
  }
  return widthUnits * scale;
}

function createFontMeasure(font, defaults = {}) {
  const baseFontSize = typeof defaults.fontSize === 'number' ? defaults.fontSize : 12;
  const baseLetterSpacing = typeof defaults.letterSpacing === 'number' ? defaults.letterSpacing : 0;
  const baseLineHeightFactor = typeof defaults.lineHeight === 'number' ? defaults.lineHeight : 1.2;

  return (text, fontSizeInput, overrides = {}) => {
    const targetSize = clamp(
      Number(fontSizeInput) || baseFontSize,
      FONT_SIZE_RANGE[0],
      FONT_SIZE_RANGE[1],
      baseFontSize
    );
    const letterSpacing =
      typeof overrides.letterSpacing === 'number' ? overrides.letterSpacing : baseLetterSpacing;
    const lineHeightFactor =
      typeof overrides.lineHeight === 'number' ? overrides.lineHeight : baseLineHeightFactor;

    const content = text == null ? '' : String(text);
    const lines = content.split(/\r?\n/);
    const widths = lines.map((line) => measureLineWidth(font, line, targetSize));
    const spacedWidths = widths.map(
      (width, index) => width + Math.max(0, lines[index].length - 1) * letterSpacing
    );
    const maxWidth = spacedWidths.reduce((max, val) => Math.max(max, val), 0);

    const unitsPerEm = getUnitsPerEm(font);
    const scale = targetSize / unitsPerEm;
    const ascent = readAscender(font) * scale;
    const rawDescender = readDescender(font);
    const descent = Math.abs(rawDescender) * scale;
    const lineHeight = (ascent + descent) * lineHeightFactor;
    const height = lineHeight * (lines.length || 1);

    return {
      width: maxWidth,
      height,
      lineHeight,
      lines: spacedWidths,
      ascent,
      descent,
      baseline: ascent,
      avgCharWidth: unitsPerEm * 0.6 * scale
    };
  };
}

module.exports = {
  createFontMeasure
};

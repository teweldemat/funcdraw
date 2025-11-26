'use strict';

const ASCII_WIDTHS = {
  i: 250,
  l: 280,
  j: 300,
  '.': 240,
  ',': 240,
  ':': 260,
  ';': 260,
  '!': 260,
  '"': 320,
  '\'': 200,
  ' ': 300,
  '1': 320,
  't': 380,
  'f': 360,
  'r': 360,
  'I': 360,
  'J': 420,
  '(': 300,
  ')': 300,
  '[': 300,
  ']': 300,
  '{': 320,
  '}': 320,
  default: 600
};

class DefaultGlyph {
  constructor(char) {
    this.char = char;
    this.advanceWidth = ASCII_WIDTHS[char] ?? ASCII_WIDTHS.default;
  }
}

class SimplePath {
  constructor() {
    this.segments = [];
  }

  rect(x, y, width, height) {
    const d = [
      'M',
      x,
      y,
      'L',
      x + width,
      y,
      'L',
      x + width,
      y - height,
      'L',
      x,
      y - height,
      'Z'
    ].join(' ');
    this.segments.push(d);
  }

  toPathData() {
    return this.segments.join(' ');
  }
}

function createDefaultFont() {
  const unitsPerEm = 1000;
  const ascender = 800;
  const descender = -200;
  const height = ascender - descender;

  return {
    familyName: 'FuncDraw Default',
    styleName: 'Regular',
    unitsPerEm,
    ascender,
    descender,
    charToGlyph(char) {
      const symbol = char && char.length > 0 ? char[0] : '';
      return new DefaultGlyph(symbol);
    },
    getKerningValue() {
      return 0;
    },
    getAdvanceWidth(text = '', fontSize = 12) {
      if (!text) {
        return 0;
      }
      let widthUnits = 0;
      for (const ch of text) {
        widthUnits += ASCII_WIDTHS[ch] ?? ASCII_WIDTHS.default;
      }
      const scale = fontSize / unitsPerEm;
      return widthUnits * scale;
    },
    getPath(text = '', x = 0, y = 0, fontSize = 12) {
      const path = new SimplePath();
      const scale = fontSize / unitsPerEm;
      let cursorX = x;
      const glyphHeight = height * scale;
      for (const ch of text) {
        const advance = (ASCII_WIDTHS[ch] ?? ASCII_WIDTHS.default) * scale;
        path.rect(cursorX, y + descender * scale, advance, glyphHeight);
        cursorX += advance;
      }
      return path;
    }
  };
}

module.exports = {
  createDefaultFont
};

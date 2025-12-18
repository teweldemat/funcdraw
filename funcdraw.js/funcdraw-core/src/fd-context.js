'use strict';

function createFdContext(options = {}) {
  const engine = options.engine;
  if (!engine || !engine.FsError) {
    throw new Error('FuncDraw requires a FuncScript engine instance');
  }
  const { FsError } = engine;
  if (typeof options.measureText !== 'function') {
    throw new Error('FuncDraw requires a measureText helper');
  }

  const normalizeNumber = (value) => {
    const number = Number(value);
    return Number.isFinite(number) ? number : NaN;
  };

  const createColor = (r, g, b, a) => ({
    type: 'color',
    space: 'srgb',
    r,
    g,
    b,
    a
  });

  const isColorObject = (value) => {
    return (
      value &&
      typeof value === 'object' &&
      !Array.isArray(value) &&
      String(value.type || '').toLowerCase() === 'color' &&
      typeof value.space === 'string' &&
      String(value.space).trim().toLowerCase() === 'srgb'
    );
  };

  const parseHexColor = (value, functionName) => {
    const raw = value == null ? '' : String(value).trim();
    if (!raw.startsWith('#')) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, `${functionName}: expected hex color string '#RRGGBB'`);
    }
    const hex = raw.slice(1);
    const isHex = (text) => /^[0-9a-fA-F]+$/.test(text);
    if (!isHex(hex) || ![3, 4, 6, 8].includes(hex.length)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, `${functionName}: expected hex color string '#RGB', '#RGBA', '#RRGGBB', or '#RRGGBBAA'`);
    }

    const expandNibble = (c) => Number.parseInt(c + c, 16);
    const readPair = (start) => Number.parseInt(hex.slice(start, start + 2), 16);

    let r = 0;
    let g = 0;
    let b = 0;
    let a = 1;

    if (hex.length === 3 || hex.length === 4) {
      r = expandNibble(hex[0]);
      g = expandNibble(hex[1]);
      b = expandNibble(hex[2]);
      if (hex.length === 4) {
        a = expandNibble(hex[3]) / 255;
      }
      return createColor(r, g, b, a);
    }

    r = readPair(0);
    g = readPair(2);
    b = readPair(4);
    if (hex.length === 8) {
      a = readPair(6) / 255;
    }
    return createColor(r, g, b, a);
  };

  const parseColor = (value, functionName) => {
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      const type = String(value.type || '').toLowerCase();
      if (type === 'color' && !isColorObject(value)) {
        return new FsError(FsError.ERROR_TYPE_MISMATCH, `${functionName}: expected srgb color`);
      }
      if (isColorObject(value)) {
        const r = normalizeNumber(value.r);
        const g = normalizeNumber(value.g);
        const b = normalizeNumber(value.b);
        const a = normalizeNumber(value.a);
        if (![r, g, b, a].every(Number.isFinite)) {
          return new FsError(FsError.ERROR_TYPE_MISMATCH, `${functionName}: expected fd.color.* value`);
        }
        return createColor(r, g, b, a);
      }
    }
    return parseHexColor(value, functionName);
  };

  const readPoint = (value) => {
    if (!Array.isArray(value) || value.length < 2) {
      return null;
    }
    const x = normalizeNumber(value[0]);
    const y = normalizeNumber(value[1]);
    if (!Number.isFinite(x) || !Number.isFinite(y)) {
      return null;
    }
    return { x, y };
  };

  const createTransform = (graphics, matrix) => ({
    type: 'transform',
    matrix,
    graphics
  });

  const createMatrix = (a, b, c, d, e, f) => ({
    a,
    b,
    c,
    d,
    e,
    f,
    multiply(other) {
      return createMatrix(
        this.a * other.a + this.c * other.b,
        this.b * other.a + this.d * other.b,
        this.a * other.c + this.c * other.d,
        this.b * other.c + this.d * other.d,
        this.a * other.e + this.c * other.f + this.e,
        this.b * other.e + this.d * other.f + this.f
      );
    },
    transformPoint(point) {
      return {
        x: this.a * point.x + this.c * point.y + this.e,
        y: this.b * point.x + this.d * point.y + this.f
      };
    }
  });
  const identityMatrix = createMatrix(1, 0, 0, 1, 0, 0);

  const rotate = (graphics, originValue, angleValue) => {
    const origin = readPoint(originValue);
    if (!origin) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.rotate: expected origin [x, y]');
    }

    const angle = normalizeNumber(angleValue);
    if (!Number.isFinite(angle)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.rotate: expected angle number (radians)');
    }

    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    const e = origin.x * (1 - cos) + origin.y * sin;
    const f = -origin.x * sin + origin.y * (1 - cos);
    return createTransform(graphics, [cos, sin, -sin, cos, e, f]);
  };

  const translate = (graphics, dxValue, dyValue) => {
    const dx = normalizeNumber(dxValue);
    if (!Number.isFinite(dx)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.translate: expected dx number');
    }

    const dy = normalizeNumber(dyValue);
    if (!Number.isFinite(dy)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.translate: expected dy number');
    }

    return createTransform(graphics, [1, 0, 0, 1, dx, dy]);
  };

  const traslate = () => {
    return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.traslate is not supported (did you mean fd.translate?)');
  };

  const scale = (graphics, originValue, scaleXValue, scaleYValue) => {
    const origin = readPoint(originValue);
    if (!origin) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.scale: expected origin [x, y]');
    }

    const sx = normalizeNumber(scaleXValue);
    if (!Number.isFinite(sx)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.scale: expected scaleX number');
    }

    const sy = normalizeNumber(scaleYValue);
    if (!Number.isFinite(sy)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.scale: expected scaleY number');
    }

    const e = origin.x * (1 - sx);
    const f = origin.y * (1 - sy);
    return createTransform(graphics, [sx, 0, 0, sy, e, f]);
  };

  const font = options.font;

  const createAccumulator = () => ({
    hasValue: false,
    minX: 0,
    minY: 0,
    maxX: 0,
    maxY: 0,
    includePoint(point) {
      if (!this.hasValue) {
        this.hasValue = true;
        this.minX = point.x;
        this.maxX = point.x;
        this.minY = point.y;
        this.maxY = point.y;
        return;
      }
      this.minX = Math.min(this.minX, point.x);
      this.minY = Math.min(this.minY, point.y);
      this.maxX = Math.max(this.maxX, point.x);
      this.maxY = Math.max(this.maxY, point.y);
    },
    include(minX, minY, maxX, maxY) {
      this.includePoint({ x: minX, y: minY });
      this.includePoint({ x: maxX, y: maxY });
    },
    expand(dx, dy) {
      this.minX -= dx;
      this.maxX += dx;
      this.minY -= dy;
      this.maxY += dy;
    }
  });

  const isFsError = (value) => {
    return value && typeof value === 'object' && value.__fsKind === 'FsError';
  };

  const readMatrix = (value) => {
    if (!Array.isArray(value) || value.length !== 6) {
      return null;
    }
    const a = normalizeNumber(value[0]);
    const b = normalizeNumber(value[1]);
    const c = normalizeNumber(value[2]);
    const d = normalizeNumber(value[3]);
    const e = normalizeNumber(value[4]);
    const f = normalizeNumber(value[5]);
    if (![a, b, c, d, e, f].every((num) => Number.isFinite(num))) {
      return null;
    }
    return createMatrix(a, b, c, d, e, f);
  };

  const expandStrokeBounds = (collection, transform, accumulator) => {
    const rawWidth = collection.width;
    const width = rawWidth == null ? 0.25 : normalizeNumber(rawWidth);
    if (!Number.isFinite(width) || width === 0) {
      return;
    }
    const radius = Math.abs(width) / 2;
    const dx = radius * Math.sqrt(transform.a * transform.a + transform.c * transform.c);
    const dy = radius * Math.sqrt(transform.b * transform.b + transform.d * transform.d);
    accumulator.expand(dx, dy);
  };

  const appendLineBounds = (collection, transform, accumulator) => {
    const from = readPoint(collection.from);
    if (!from) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: line.from must be [x, y]');
    }
    const to = readPoint(collection.to);
    if (!to) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: line.to must be [x, y]');
    }

    accumulator.includePoint(transform.transformPoint(from));
    accumulator.includePoint(transform.transformPoint(to));
    expandStrokeBounds(collection, transform, accumulator);
    return null;
  };

  const appendRectBounds = (collection, transform, accumulator) => {
    const position = readPoint(collection.position);
    if (!position) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: rect.position must be [x, y]');
    }
    const size = readPoint(collection.size);
    if (!size) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: rect.size must be [width, height]');
    }

    const x = position.x;
    const y = position.y;
    const w = size.x;
    const h = size.y;
    accumulator.includePoint(transform.transformPoint({ x, y }));
    accumulator.includePoint(transform.transformPoint({ x: x + w, y }));
    accumulator.includePoint(transform.transformPoint({ x: x + w, y: y + h }));
    accumulator.includePoint(transform.transformPoint({ x, y: y + h }));
    expandStrokeBounds(collection, transform, accumulator);
    return null;
  };

  const appendCircleBounds = (collection, transform, accumulator) => {
    const center = readPoint(collection.center);
    if (!center) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: circle.center must be [x, y]');
    }
    const radius = normalizeNumber(collection.radius);
    if (!Number.isFinite(radius)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: circle.radius must be a number');
    }

    const worldCenter = transform.transformPoint(center);
    const dx = Math.abs(radius) * Math.sqrt(transform.a * transform.a + transform.c * transform.c);
    const dy = Math.abs(radius) * Math.sqrt(transform.b * transform.b + transform.d * transform.d);
    accumulator.include(worldCenter.x - dx, worldCenter.y - dy, worldCenter.x + dx, worldCenter.y + dy);
    expandStrokeBounds(collection, transform, accumulator);
    return null;
  };

  const appendEllipseBounds = (collection, transform, accumulator) => {
    const center = readPoint(collection.center);
    if (!center) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: ellipse.center must be [x, y]');
    }
    const rxRaw = collection.radiusx ?? collection.rx;
    const ryRaw = collection.radiusy ?? collection.ry;
    const rx = normalizeNumber(rxRaw);
    const ry = normalizeNumber(ryRaw);
    if (!Number.isFinite(rx) || !Number.isFinite(ry)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: ellipse.radiusX/radiusY must be numbers');
    }

    const worldCenter = transform.transformPoint(center);
    const dx = Math.sqrt(Math.pow(transform.a * rx, 2) + Math.pow(transform.c * ry, 2));
    const dy = Math.sqrt(Math.pow(transform.b * rx, 2) + Math.pow(transform.d * ry, 2));
    accumulator.include(worldCenter.x - dx, worldCenter.y - dy, worldCenter.x + dx, worldCenter.y + dy);
    expandStrokeBounds(collection, transform, accumulator);
    return null;
  };

  const appendPointsBounds = (collection, kind, transform, accumulator) => {
    const points = collection.points;
    if (!Array.isArray(points)) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, `fd.boundingbox: ${kind}.points must be a list of [x, y]`);
    }
    let foundAny = false;
    for (const pointValue of points) {
      const point = readPoint(pointValue);
      if (!point) {
        return new FsError(FsError.ERROR_TYPE_MISMATCH, `fd.boundingbox: ${kind}.points must be a list of [x, y]`);
      }
      accumulator.includePoint(transform.transformPoint(point));
      foundAny = true;
    }
    if (!foundAny) {
      return null;
    }
    expandStrokeBounds(collection, transform, accumulator);
    return null;
  };

  const getUnitsPerEm = () => {
    if (font && typeof font.unitsPerEm === 'number' && font.unitsPerEm > 0) {
      return font.unitsPerEm;
    }
    return 1000;
  };

  const getAscender = () => {
    if (font && typeof font.ascender === 'number') {
      return font.ascender;
    }
    return getUnitsPerEm() * 0.8;
  };

  const getDescender = () => {
    if (font && typeof font.descender === 'number') {
      return font.descender;
    }
    return -getUnitsPerEm() * 0.2;
  };

  const appendTextBounds = (collection, transform, accumulator) => {
    const position = readPoint(collection.position);
    if (!position) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: text.position must be [x, y]');
    }
    const text = collection.text == null ? '' : String(collection.text);
    const fontSize = normalizeNumber(collection.fontsize ?? collection.fontSize);
    const normalizedFontSize = Number.isFinite(fontSize) ? fontSize : 12;
    const align = collection.align == null ? 'left' : String(collection.align);

    const unitsPerEm = getUnitsPerEm();
    const scale = normalizedFontSize / unitsPerEm;
    const ascent = getAscender() * scale;
    const descent = Math.abs(getDescender()) * scale;
    const lineHeight = (ascent + descent) * 1.2;
    const normalizedAlign = align.trim().toLowerCase();
    const lines = text.split(/\r?\n/);

    for (let i = 0; i < lines.length; i += 1) {
      const line = lines[i];
      const metrics = options.measureText(line, normalizedFontSize) || {};
      const lineWidth = normalizeNumber(metrics.width);
      const widthValue = Number.isFinite(lineWidth) ? lineWidth : 0;

      let penX = position.x;
      if (normalizedAlign === 'center') {
        penX -= widthValue / 2;
      } else if (normalizedAlign === 'right') {
        penX -= widthValue;
      }
      const baselineY = position.y - i * lineHeight;

      let previousGlyph = null;
      for (const char of line) {
        const glyph = font && typeof font.charToGlyph === 'function' ? font.charToGlyph(char) : null;
        if (previousGlyph && glyph && typeof font.getKerningValue === 'function') {
          const kern = font.getKerningValue(previousGlyph, glyph);
          if (typeof kern === 'number' && Number.isFinite(kern)) {
            penX += kern * scale;
          }
        }

        const advanceWidth = glyph && typeof glyph.advanceWidth === 'number' ? glyph.advanceWidth : unitsPerEm * 0.6;
        const bounds =
          glyph && typeof glyph.getBoundingBox === 'function'
            ? glyph.getBoundingBox()
            : glyph && typeof glyph.xMin === 'number' && typeof glyph.yMin === 'number' && typeof glyph.xMax === 'number' && typeof glyph.yMax === 'number'
              ? { x1: glyph.xMin, y1: glyph.yMin, x2: glyph.xMax, y2: glyph.yMax }
              : { x1: 0, y1: getDescender(), x2: advanceWidth, y2: getAscender() };

        if (bounds.x1 !== bounds.x2 || bounds.y1 !== bounds.y2) {
          const minX = penX + bounds.x1 * scale;
          const maxX = penX + bounds.x2 * scale;
          const minY = baselineY + bounds.y1 * scale;
          const maxY = baselineY + bounds.y2 * scale;
          accumulator.includePoint(transform.transformPoint({ x: minX, y: minY }));
          accumulator.includePoint(transform.transformPoint({ x: maxX, y: minY }));
          accumulator.includePoint(transform.transformPoint({ x: maxX, y: maxY }));
          accumulator.includePoint(transform.transformPoint({ x: minX, y: maxY }));
        }

        penX += advanceWidth * scale;
        previousGlyph = glyph;
      }
    }

    return null;
  };

  const appendTransformBounds = (collection, transform, accumulator) => {
    if (collection.matrix == null) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: transform missing 'matrix'");
    }
    const localMatrix = readMatrix(collection.matrix);
    if (!localMatrix) {
      return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: transform.matrix must be [a, b, c, d, e, f]');
    }
    if (collection.graphics == null) {
      return null;
    }
    return appendBounds(collection.graphics, transform.multiply(localMatrix), accumulator);
  };

  const appendBoundsFromCollection = (collection, transform, accumulator) => {
    if (collection.type == null) {
      if (collection.graphics != null) {
        return appendBounds(collection.graphics, transform, accumulator);
      }
      return new FsError(
        FsError.ERROR_TYPE_MISMATCH,
        "fd.boundingbox: expected graphics object (missing 'type' or 'graphics')"
      );
    }

    const typeText = String(collection.type);
    const type = typeText.trim().toLowerCase();
    if (type === 'transofrm') {
      return new FsError(
        FsError.ERROR_TYPE_MISMATCH,
        "fd.boundingbox: unknown primitive type 'transofrm' (did you mean 'transform'?)"
      );
    }

    switch (type) {
      case 'line':
        return appendLineBounds(collection, transform, accumulator);
      case 'rect':
      case 'rectangle':
        return appendRectBounds(collection, transform, accumulator);
      case 'circle':
        return appendCircleBounds(collection, transform, accumulator);
      case 'ellipse':
        return appendEllipseBounds(collection, transform, accumulator);
      case 'polygon':
        return appendPointsBounds(collection, 'polygon', transform, accumulator);
      case 'polyline':
        return appendPointsBounds(collection, 'polyline', transform, accumulator);
      case 'text':
        return appendTextBounds(collection, transform, accumulator);
      case 'debug':
        return null;
      case 'path':
        return new FsError(FsError.ERROR_TYPE_MISMATCH, "fd.boundingbox: 'path' is not supported yet");
      case 'transform':
        return appendTransformBounds(collection, transform, accumulator);
      default:
        if (collection.graphics != null) {
          return appendBounds(collection.graphics, transform, accumulator);
        }
        return new FsError(
          FsError.ERROR_TYPE_MISMATCH,
          `fd.boundingbox: unsupported primitive type '${typeText.trim()}'`
        );
    }
  };

  const appendBounds = (value, transform, accumulator) => {
    if (value == null) {
      return null;
    }
    if (isFsError(value)) {
      return value;
    }
    if (Array.isArray(value)) {
      for (const item of value) {
        const err = appendBounds(item, transform, accumulator);
        if (err) {
          return err;
        }
      }
      return null;
    }
    if (value && typeof value === 'object') {
      return appendBoundsFromCollection(value, transform, accumulator);
    }
    if (
      typeof value !== 'string' &&
      typeof value?.[Symbol.iterator] === 'function' &&
      !(value instanceof Uint8Array) &&
      !(typeof Buffer !== 'undefined' && Buffer.isBuffer(value))
    ) {
      for (const item of value) {
        const err = appendBounds(item, transform, accumulator);
        if (err) {
          return err;
        }
      }
      return null;
    }
    return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.boundingbox: expected graphics (primitive or list)');
  };

  const boundingBox = (graphics) => {
    const accumulator = createAccumulator();
    const error = appendBounds(graphics, identityMatrix, accumulator);
    if (error) {
      return error;
    }
    if (!accumulator.hasValue) {
      return null;
    }
    const left = accumulator.minX;
    const bottom = accumulator.minY;
    const right = accumulator.maxX;
    const top = accumulator.maxY;
    return {
      left,
      bottom,
      right,
      top,
      width: right - left,
      height: top - bottom
    };
  };

  const context = {
    measureText: options.measureText,
    rotate,
    translate,
    traslate,
    scale,
    boundingBox,
    color: {
      rgb: (r, g, b) => {
        const rr = normalizeNumber(r);
        const gg = normalizeNumber(g);
        const bb = normalizeNumber(b);
        if (![rr, gg, bb].every(Number.isFinite)) {
          return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.color.rgb: expected numbers r, g, b');
        }
        return createColor(rr, gg, bb, 1);
      },
      rgba: (r, g, b, a) => {
        const rr = normalizeNumber(r);
        const gg = normalizeNumber(g);
        const bb = normalizeNumber(b);
        const aa = normalizeNumber(a);
        if (![rr, gg, bb, aa].every(Number.isFinite)) {
          return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.color.rgba: expected numbers r, g, b, a');
        }
        return createColor(rr, gg, bb, aa);
      },
      hex: (hex) => parseHexColor(hex, 'fd.color.hex'),
      parse: (value) => parseColor(value, 'fd.color.parse'),
      alpha: (value, alphaValue) => {
        const base = parseColor(value, 'fd.color.alpha');
        if (isFsError(base)) {
          return base;
        }
        const aa = normalizeNumber(alphaValue);
        if (!Number.isFinite(aa)) {
          return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.color.alpha: expected alpha number');
        }
        return createColor(base.r, base.g, base.b, aa);
      },
      mulAlpha: (value, alphaValue) => {
        const base = parseColor(value, 'fd.color.mulAlpha');
        if (isFsError(base)) {
          return base;
        }
        const factor = normalizeNumber(alphaValue);
        if (!Number.isFinite(factor)) {
          return new FsError(FsError.ERROR_TYPE_MISMATCH, 'fd.color.mulAlpha: expected alpha multiplier number');
        }
        return createColor(base.r, base.g, base.b, base.a * factor);
      }
    }
  };

  if (options.expose && typeof options.expose === 'object') {
    Object.assign(context, options.expose);
  }

  return context;
}

module.exports = {
  createFdContext
};

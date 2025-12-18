'use strict';

const { toArray } = require('../utils');

const DEFAULT_VIEW_SIZE = [1920, 1080];

function paintToCss(value) {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === 'string') {
    return value;
  }
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    const type = typeof value.type === 'string' ? value.type.toLowerCase() : '';
    if (type === 'color') {
      const space = typeof value.space === 'string' ? value.space.toLowerCase() : '';
      if (space !== 'srgb') {
        throw new Error('Unsupported color space (expected srgb)');
      }
      const r = Number(value.r);
      const g = Number(value.g);
      const b = Number(value.b);
      const a = Number(value.a);
      if (![r, g, b, a].every(Number.isFinite)) {
        throw new Error('Invalid srgb color value (expected numbers r,g,b,a)');
      }
      return `rgba(${r}, ${g}, ${b}, ${a})`;
    }
  }
  throw new Error('Unsupported paint value');
}

function formatOpacity(node) {
  if (!node || typeof node !== 'object') {
    return '';
  }
  if (node.opacity === null || node.opacity === undefined) {
    return '';
  }
  const opacity = Number(node.opacity);
  if (!Number.isFinite(opacity)) {
    throw new Error('opacity must be a finite number');
  }
  return ` opacity="${opacity}"`;
}

function formatBlendMode(node) {
  if (!node || typeof node !== 'object') {
    return '';
  }
  const raw = node.blendMode;
  if (raw === null || raw === undefined) {
    return '';
  }
  const blendMode = String(raw).trim();
  if (!blendMode || blendMode === 'source-over' || blendMode === 'normal') {
    return '';
  }
  return ` style="mix-blend-mode: ${blendMode};"`;
}

function renderSvg(scene, options) {
  if (!scene || !Array.isArray(scene.graphics)) {
    return '';
  }
  const viewBox = resolveViewBox(scene.view);
  const canvasSize = resolveCanvasSize(options && options.canvas);
  const outputWidth = canvasSize ? canvasSize.width : viewBox.width;
  const outputHeight = canvasSize ? canvasSize.height : viewBox.height;
  const layers = toArray(scene.graphics);
  const parts = layers
    .map((node, index) => renderNode(node, { ...options, layer: index, depth: 0 }))
    .join('');
  const transform = formatRootTransform(viewBox);
  const body = transform ? `<g transform="${transform}">${parts}</g>` : parts;
  return [
    `<svg xmlns="http://www.w3.org/2000/svg" width="${outputWidth}" height="${outputHeight}" viewBox="0 0 ${viewBox.width} ${viewBox.height}" fill="none">`,
    body,
    '</svg>'
  ].join('');
}

function resolveCanvasSize(canvas) {
  if (canvas === null || canvas === undefined) {
    return null;
  }
  if (Array.isArray(canvas) && canvas.length >= 2) {
    const width = Number(canvas[0]);
    const height = Number(canvas[1]);
    if (!Number.isFinite(width) || !Number.isFinite(height)) {
      throw new Error('FuncDraw svg renderer expects canvas width/height to be finite numbers');
    }
    return { width, height };
  }
  if (canvas && typeof canvas === 'object') {
    const width = Number(canvas.width);
    const height = Number(canvas.height);
    if (!Number.isFinite(width) || !Number.isFinite(height)) {
      throw new Error('FuncDraw svg renderer expects canvas width/height to be finite numbers');
    }
    return { width, height };
  }
  throw new Error('FuncDraw svg renderer expects canvas to be {width,height} or [width,height]');
}

function resolveViewBox(view) {
  if (Array.isArray(view) && view.length >= 2) {
    const width = Number(view[0]) || DEFAULT_VIEW_SIZE[0];
    const height = Number(view[1]) || DEFAULT_VIEW_SIZE[1];
    return {
      left: 0,
      bottom: 0,
      right: width,
      top: height,
      width,
      height
    };
  }
  if (view && typeof view === 'object') {
    const left = Number(view.left);
    const bottom = Number(view.bottom);
    const right = Number(view.right);
    const top = Number(view.top);
    if ([left, bottom, right, top].every(Number.isFinite)) {
      const width = right - left;
      const height = top - bottom;
      if (width > 0 && height > 0) {
        return { left, bottom, right, top, width, height };
      }
    }
  }
  const width = DEFAULT_VIEW_SIZE[0];
  const height = DEFAULT_VIEW_SIZE[1];
  return {
    left: 0,
    bottom: 0,
    right: width,
    top: height,
    width,
    height
  };
}

function formatRootTransform(viewBox) {
  return `translate(0 ${viewBox.height}) scale(1 -1) translate(${-viewBox.left} ${-viewBox.bottom})`;
}

function renderNode(node, context) {
  if (node == null) {
    return '';
  }
  if (Array.isArray(node)) {
    const inner = node.map((child) => renderNode(child, context)).join('');
    return `<g data-layer="${context.layer || 0}">${inner}</g>`;
  }
  if (!node || typeof node !== 'object') {
    return '';
  }
  const type = typeof node.type === 'string' ? node.type.toLowerCase() : '';
  switch (type) {
    case 'group':
      return renderGroup(node, context);
    case 'line':
      return renderLine(node);
    case 'rect':
    case 'rectangle':
      return renderRect(node);
    case 'circle':
      return renderCircle(node);
    case 'ellipse':
      return renderEllipse(node);
    case 'polygon':
      return renderPolygon(node);
    case 'polyline':
      return renderPolyline(node);
    case 'path':
      return renderPath(node);
    case 'text':
      return renderText(node, context);
    case 'transform':
      return renderTransform(node, context);
    case 'custom':
      return renderCustom(node, context);
    case 'debug':
      return '';
    default:
      if (node.graphics) {
        return renderCustom(
          {
            name: node.type || 'custom',
            graphics: node.graphics,
            props: node.props || {},
            opacity: node.opacity,
            blendMode: node.blendMode
          },
          context
        );
      }
      return '';
  }
}

function renderLine(node) {
  const from = toPoint(node.from);
  const to = toPoint(node.to);
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  const dash =
    Array.isArray(node.dash) && node.dash.length > 0 ? ` stroke-dasharray="${node.dash.join(' ')}"` : '';
  return `<line x1="${from[0]}" y1="${from[1]}" x2="${to[0]}" y2="${to[1]}" stroke="${stroke}" stroke-width="${width}"${dash}${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderRect(node) {
  const position = toPoint(node.position);
  const size = toPoint(node.size || [1, 1]);
  const fill = paintToCss(node.fill) || 'none';
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  return `<rect x="${position[0]}" y="${position[1]}" width="${size[0]}" height="${size[1]}" fill="${fill}" stroke="${stroke}" stroke-width="${width}"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderCircle(node) {
  const center = toPoint(node.center);
  const radius = Number(node.radius) || 1;
  const fill = paintToCss(node.fill) || 'none';
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  return `<circle cx="${center[0]}" cy="${center[1]}" r="${radius}" fill="${fill}" stroke="${stroke}" stroke-width="${width}"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderEllipse(node) {
  const center = toPoint(node.center);
  const rx = Number(node.radiusX) || Number(node.rx) || 1;
  const ry = Number(node.radiusY) || Number(node.ry) || 1;
  const fill = paintToCss(node.fill) || 'none';
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  return `<ellipse cx="${center[0]}" cy="${center[1]}" rx="${rx}" ry="${ry}" fill="${fill}" stroke="${stroke}" stroke-width="${width}"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderPolygon(node) {
  const points = formatPoints(node.points);
  if (!points) {
    return '';
  }
  const fill = paintToCss(node.fill) || 'none';
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  return `<polygon points="${points}" fill="${fill}" stroke="${stroke}" stroke-width="${width}"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderPolyline(node) {
  const points = formatPoints(node.points);
  if (!points) {
    return '';
  }
  const fill = paintToCss(node.fill) || 'none';
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  return `<polyline points="${points}" fill="${fill}" stroke="${stroke}" stroke-width="${width}"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderPath(node) {
  if (!node.d) {
    return '';
  }
  const fill = paintToCss(node.fill) || 'none';
  const stroke = paintToCss(node.stroke) || '#38bdf8';
  const width = node.width || 0.25;
  return `<path d="${node.d}" fill="${fill}" stroke="${stroke}" stroke-width="${width}"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function renderCustom(node, context) {
  const props = node.props && typeof node.props === 'object' ? node.props : {};
  const attributes = Object.entries(props)
    .map(([key, value]) => ` data-${encodeAttribute(key)}="${encodeAttribute(value)}"`)
    .join('');
  const inner = renderNode(node.graphics, context);
  return `<g data-custom="${encodeAttribute(node.name || 'custom')}"${attributes}${formatOpacity(node)}${formatBlendMode(node)}>${inner}</g>`;
}

function renderGroup(node, context) {
  const inner = renderNode(node.graphics, context);
  return `<g${formatOpacity(node)}${formatBlendMode(node)}>${inner}</g>`;
}

function renderTransform(node, context) {
  const matrix = node.matrix;
  if (!Array.isArray(matrix) || matrix.length !== 6) {
    throw new Error('transform.matrix must be [a, b, c, d, e, f]');
  }
  const numbers = matrix.map((entry) => Number(entry));
  if (numbers.some((value) => !Number.isFinite(value))) {
    throw new Error('transform.matrix must be [a, b, c, d, e, f]');
  }
  const inner = renderNode(node.graphics, context);
  return `<g transform="matrix(${numbers.join(' ')})"${formatOpacity(node)}${formatBlendMode(node)}>${inner}</g>`;
}

function renderText(node, context) {
  const text = node.text == null ? '' : String(node.text);
  if (!text) {
    return '';
  }
  const position = toPoint(node.position);
  const align = typeof node.align === 'string' ? node.align.toLowerCase() : 'left';
  const fontSize = Number(node.fontSize) || 12;
  const fill = paintToCss(node.color) || paintToCss(node.fill) || '#e2e8f0';
  const measureText = context.measureText;
  if (typeof measureText !== 'function') {
    throw new Error('FuncDraw svg renderer requires a measureText helper');
  }
  const font = context.font;
  if (!font || typeof font.getPath !== 'function') {
    throw new Error('FuncDraw svg renderer requires a glyph font');
  }
  const lines = text.split(/\r?\n/);
  const pathSegments = [];
  for (let lineIndex = 0; lineIndex < lines.length; lineIndex += 1) {
    const line = lines[lineIndex];
    const metrics = measureText(line, fontSize);
    const width = metrics.width;
    const lineHeight = metrics.lineHeight;
    let x = position[0];
    if (align === 'center') {
      x -= width / 2;
    } else if (align === 'right') {
      x -= width;
    }
    const baseline = position[1] - lineIndex * lineHeight;
    const linePath = font.getPath(line, x, baseline, fontSize);
    const d = pathToData(linePath);
    if (d) {
      pathSegments.push(d);
    }
  }
  if (pathSegments.length === 0) {
    return '';
  }
  return `<path d="${pathSegments.join(' ')}" fill="${fill}" stroke="none"${formatOpacity(node)}${formatBlendMode(node)} />`;
}

function pathToData(path) {
  if (!path) {
    return '';
  }
  if (typeof path.toPathData === 'function') {
    return path.toPathData();
  }
  if (Array.isArray(path.commands)) {
    return path.commands
      .map((cmd) => {
        if (!cmd) {
          return '';
        }
        switch (cmd.type) {
          case 'M':
            return `M${cmd.x} ${cmd.y}`;
          case 'L':
            return `L${cmd.x} ${cmd.y}`;
          case 'C':
            return `C${cmd.x1} ${cmd.y1} ${cmd.x2} ${cmd.y2} ${cmd.x} ${cmd.y}`;
          case 'Q':
            return `Q${cmd.x1} ${cmd.y1} ${cmd.x} ${cmd.y}`;
          case 'Z':
            return 'Z';
          default:
            return '';
        }
      })
      .join(' ');
  }
  if (typeof path.toSVG === 'function') {
    const svg = path.toSVG();
    const match = /d="([^"]+)"/.exec(svg);
    return match ? match[1] : '';
  }
  return '';
}

function encodeAttribute(value) {
  if (value == null) {
    return '';
  }
  return String(value).replace(/"/g, '&quot;');
}

function toPoint(value) {
  if (Array.isArray(value) && value.length >= 2) {
    return [Number(value[0]) || 0, Number(value[1]) || 0];
  }
  return [0, 0];
}

function formatPoints(points) {
  if (!Array.isArray(points) || points.length === 0) {
    return '';
  }
  return points
    .map((point) => {
      const [x, y] = toPoint(point);
      return `${x},${y}`;
    })
    .join(' ');
}

module.exports = {
  renderSvg
};

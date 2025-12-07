'use strict';

const { toArray } = require('../utils');

const DEFAULT_VIEW_SIZE = [1920, 1080];

function renderSvg(scene, options) {
  if (!scene || !Array.isArray(scene.graphics)) {
    return '';
  }
  const [width, height] = getViewSize(scene.view);
  const layers = toArray(scene.graphics);
  const parts = [];
  layers.forEach((node, index) => {
    parts.push(renderNode(node, { ...options, layer: index, depth: 0 }));
  });
  return [
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" fill="none">`,
    parts.join(''),
    '</svg>'
  ].join('');
}

function getViewSize(view) {
  if (Array.isArray(view) && view.length >= 2) {
    return [Number(view[0]) || DEFAULT_VIEW_SIZE[0], Number(view[1]) || DEFAULT_VIEW_SIZE[1]];
  }
  if (view && typeof view === 'object' && Array.isArray(view.size)) {
    const size = view.size;
    return [Number(size[0]) || DEFAULT_VIEW_SIZE[0], Number(size[1]) || DEFAULT_VIEW_SIZE[1]];
  }
  return DEFAULT_VIEW_SIZE.slice();
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
            props: node.props || {}
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
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  const dash =
    Array.isArray(node.dash) && node.dash.length > 0 ? ` stroke-dasharray="${node.dash.join(' ')}"` : '';
  return `<line x1="${from[0]}" y1="${from[1]}" x2="${to[0]}" y2="${to[1]}" stroke="${stroke}" stroke-width="${width}"${dash} />`;
}

function renderRect(node) {
  const position = toPoint(node.position);
  const size = toPoint(node.size || [1, 1]);
  const fill = node.fill || 'none';
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  return `<rect x="${position[0]}" y="${position[1]}" width="${size[0]}" height="${size[1]}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" />`;
}

function renderCircle(node) {
  const center = toPoint(node.center);
  const radius = Number(node.radius) || 1;
  const fill = node.fill || 'none';
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  return `<circle cx="${center[0]}" cy="${center[1]}" r="${radius}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" />`;
}

function renderEllipse(node) {
  const center = toPoint(node.center);
  const rx = Number(node.radiusX) || Number(node.rx) || 1;
  const ry = Number(node.radiusY) || Number(node.ry) || 1;
  const fill = node.fill || 'none';
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  return `<ellipse cx="${center[0]}" cy="${center[1]}" rx="${rx}" ry="${ry}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" />`;
}

function renderPolygon(node) {
  const points = formatPoints(node.points);
  if (!points) {
    return '';
  }
  const fill = node.fill || 'none';
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  return `<polygon points="${points}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" />`;
}

function renderPolyline(node) {
  const points = formatPoints(node.points);
  if (!points) {
    return '';
  }
  const fill = node.fill || 'none';
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  return `<polyline points="${points}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" />`;
}

function renderPath(node) {
  if (!node.d) {
    return '';
  }
  const fill = node.fill || 'none';
  const stroke = node.stroke || '#38bdf8';
  const width = node.width || 0.25;
  return `<path d="${node.d}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" />`;
}

function renderCustom(node, context) {
  const props = node.props && typeof node.props === 'object' ? node.props : {};
  const attributes = Object.entries(props)
    .map(([key, value]) => ` data-${encodeAttribute(key)}="${encodeAttribute(value)}"`)
    .join('');
  const inner = renderNode(node.graphics, context);
  return `<g data-custom="${encodeAttribute(node.name || 'custom')}"${attributes}>${inner}</g>`;
}

function renderText(node, context) {
  const text = node.text == null ? '' : String(node.text);
  if (!text) {
    return '';
  }
  const position = toPoint(node.position);
  const align = typeof node.align === 'string' ? node.align.toLowerCase() : 'left';
  const fontSize = Number(node.fontSize) || 12;
  const fill = node.color || node.fill || '#e2e8f0';
  const measure =
    typeof context.measureText === 'function'
      ? context.measureText
      : (value, size) => ({
          width: (value ? value.length : 0) * size * 0.6,
          lineHeight: size * 1.2,
          baseline: size
        });
  const font = context.font;
  const lines = text.split(/\r?\n/);
  const pathSegments = [];
  let offsetY = 0;
  for (const line of lines) {
    const metrics = measure(line, fontSize) || {};
    const width = metrics.width || 0;
    const lineHeight = metrics.lineHeight || fontSize * 1.2;
    let x = position[0];
    if (align === 'center') {
      x -= width / 2;
    } else if (align === 'right') {
      x -= width;
    }
    const baseline = position[1] + offsetY + (metrics.baseline || fontSize);
    const linePath = safeGetPath(font, line, x, baseline, fontSize);
    const d = pathToData(linePath);
    if (d) {
      pathSegments.push(d);
    }
    offsetY += lineHeight;
  }
  if (pathSegments.length === 0) {
    return '';
  }
  return `<path d="${pathSegments.join(' ')}" fill="${fill}" stroke="none" />`;
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
function safeGetPath(font, text, x, y, fontSize) {
  if (!font || typeof font.getPath !== 'function') {
    return null;
  }
  try {
    return font.getPath(text, x, y, fontSize);
  } catch {
    return null;
  }
}

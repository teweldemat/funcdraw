import { Engine, FSDataType, FsList, KeyValueCollection, valueOf, typeOf, ensureTyped } from '@tewelde/funcscript';
import type { PreparedGraphics, PrimitiveLine, PrimitiveRect, PrimitiveCircle, PrimitiveEllipse, PrimitivePolygon, PrimitiveText, ViewExtent } from './types.js';

const DEFAULT_BACKGROUND = '#0f172a';
const DEFAULT_GRID_COLOR = 'rgba(148, 163, 184, 0.2)';
const DEFAULT_STROKE = '#38bdf8';
const DEFAULT_STROKE_WIDTH = 0.25;

const ensureNumber = (value: unknown): number | null =>
  typeof value === 'number' && Number.isFinite(value) ? value : null;

const ensurePoint = (value: unknown): [number, number] | null => {
  if (!Array.isArray(value) || value.length !== 2) {
    return null;
  }
  const [x, y] = value;
  if (typeof x === 'number' && Number.isFinite(x) && typeof y === 'number' && Number.isFinite(y)) {
    return [x, y];
  }
  return null;
};

const ensurePoints = (value: unknown): [number, number][] | null => {
  if (!Array.isArray(value) || value.length < 3) {
    return null;
  }
  const points: [number, number][] = [];
  for (const entry of value) {
    const point = ensurePoint(entry);
    if (!point) {
      return null;
    }
    points.push(point);
  }
  return points;
};

const toPlainValue = (value: unknown): any => {
  if (
    value === null ||
    value === undefined ||
    typeof value === 'boolean' ||
    typeof value === 'number' ||
    typeof value === 'string'
  ) {
    return value;
  }
  if (Array.isArray(value)) {
    return value.map((entry) => toPlainValue(entry));
  }
  if (value && typeof value === 'object') {
    if (value instanceof Date) {
      return value;
    }
    try {
      const typed = ensureTyped(value as any);
      const dataType = typeOf(typed);
      const raw: any = valueOf(typed);
      switch (dataType) {
        case FSDataType.Null:
        case FSDataType.Boolean:
        case FSDataType.Integer:
        case FSDataType.Float:
        case FSDataType.String:
        case FSDataType.BigInteger:
        case FSDataType.Guid:
        case FSDataType.DateTime:
          return raw;
        case FSDataType.ByteArray:
          return raw;
        case FSDataType.List: {
          if (raw instanceof FsList && typeof raw.toArray === 'function') {
            return raw.toArray().map((entry) => toPlainValue(entry));
          }
          if (!raw || typeof raw[Symbol.iterator] !== 'function') {
            return [];
          }
          return Array.from(raw as Iterable<unknown>, (entry) => toPlainValue(entry));
        }
        case FSDataType.KeyValueCollection: {
          if (raw instanceof KeyValueCollection && typeof raw.getAll === 'function') {
            const result: Record<string, unknown> = {};
            for (const [key, entry] of raw.getAll()) {
              result[key] = toPlainValue(entry);
            }
            return result;
          }
          return raw;
        }
        case FSDataType.Error: {
          const err = (raw as { errorType?: string; errorMessage?: string; errorData?: unknown }) ?? {};
          return {
            errorType: err.errorType || 'Error',
            errorMessage: err.errorMessage || '',
            errorData: err.errorData ?? null
          };
        }
        default:
          return raw;
      }
    } catch (err) {
      const entries = Object.entries(value as Record<string, unknown>);
      if (entries.length === 0) {
        return {};
      }
      const result: Record<string, unknown> = {};
      for (const [key, entry] of entries) {
        result[key] = toPlainValue(entry);
      }
      return result;
    }
  }
  return value;
};

export const interpretGraphics = (value: unknown) => {
  const warnings: string[] = [];
  if (value === null || value === undefined) {
    return { layers: null, warnings: ['Graphics expression returned null.'] };
  }

  const toPlain = toPlainValue(value);

  const collect = (node: unknown, label: string): any[] => {
    if (Array.isArray(node)) {
      return node.flatMap((child, index) => collect(child, `${label}[${index}]`));
    }
    if (node && typeof node === 'object') {
      const record = node as Record<string, unknown>;
      const { type, data, transform, ...rest } = record;
      if (data !== undefined) {
        warnings.push(`Primitive at ${label} should not use the 'data' wrapper.`);
        return [];
      }
      if (typeof type === 'string') {
        if (transform !== undefined && transform !== null) {
          warnings.push(`Primitive at ${label} includes a transform which will be ignored.`);
        }
        return [{ type, data: rest }];
      }
    }
    warnings.push(`Skipping graphics entry at ${label} because it is not a primitive.`);
    return [];
  };

  const layers: any[][] = [];
  if (Array.isArray(toPlain)) {
    const primitives = collect(toPlain, 'root');
    if (primitives.length > 0) {
      layers.push(primitives);
    }
  } else if (toPlain && typeof toPlain === 'object') {
    const primitives = collect(toPlain, 'root');
    if (primitives.length > 0) {
      layers.push(primitives);
    }
  } else {
    warnings.push('Graphics expression must return a primitive collection.');
  }

  return { layers: layers.length > 0 ? layers : null, warnings };
};

export const interpretView = (value: unknown) => {
  const plain = toPlainValue(value);
  if (!plain || typeof plain !== 'object' || Array.isArray(plain)) {
    return { extent: null as ViewExtent | null, warning: 'View expression must return { minX, minY, maxX, maxY }.' };
  }
  const record = plain as Record<string, unknown>;
  const minX = ensureNumber(record.minX);
  const maxX = ensureNumber(record.maxX);
  const minY = ensureNumber(record.minY);
  const maxY = ensureNumber(record.maxY);
  if (minX === null || maxX === null || minY === null || maxY === null) {
    return { extent: null as ViewExtent | null, warning: 'All extent fields must be finite numbers.' };
  }
  if (maxX <= minX || maxY <= minY) {
    return { extent: null as ViewExtent | null, warning: 'Extent must define a positive width and height.' };
  }
  return { extent: { minX, maxX, minY, maxY }, warning: null };
};

export const prepareGraphics = (extent: ViewExtent, layers: any[][] | null): PreparedGraphics => {
  if (!extent || !layers) {
    return { layers: [], warnings: ['Missing view extent or graphics layers.'] };
  }
  const preparedLayers: PreparedGraphics['layers'] = [];
  const warnings: string[] = [];

  layers.forEach((layer, layerIndex) => {
    const prepared: PreparedGraphics['layers'][number] = [];
    layer.forEach((primitive, primitiveIndex) => {
      const ctx = `layer ${layerIndex + 1}, primitive ${primitiveIndex + 1}`;
      const data = primitive?.data ?? primitive;
      switch (primitive.type) {
        case 'line': {
          const from = ensurePoint(data.from);
          const to = ensurePoint(data.to);
          if (!from || !to) {
            warnings.push(`Line in ${ctx} requires numeric from/to points.`);
            return;
          }
          const dash = Array.isArray(data.dash) ? data.dash.filter((segment: unknown) => typeof segment === 'number') : null;
          const item: PrimitiveLine = {
            type: 'line',
            from,
            to,
            stroke: typeof data.stroke === 'string' ? data.stroke : DEFAULT_STROKE,
            width: ensureNumber(data.width) ?? DEFAULT_STROKE_WIDTH,
            dash: dash && dash.length > 0 ? dash : null
          };
          prepared.push(item);
          return;
        }
        case 'rect': {
          const position = ensurePoint(data.position);
          const size = ensurePoint(data.size);
          if (!position || !size) {
            warnings.push(`Rectangle in ${ctx} requires position and size.`);
            return;
          }
          const item: PrimitiveRect = {
            type: 'rect',
            position,
            size,
            stroke: typeof data.stroke === 'string' ? data.stroke : DEFAULT_STROKE,
            fill: typeof data.fill === 'string' ? data.fill : null,
            width: ensureNumber(data.width) ?? DEFAULT_STROKE_WIDTH
          };
          prepared.push(item);
          return;
        }
        case 'circle': {
          const center = ensurePoint(data.center);
          const radius = ensureNumber(data.radius);
          if (!center || radius === null || radius <= 0) {
            warnings.push(`Circle in ${ctx} requires center and positive radius.`);
            return;
          }
          const item: PrimitiveCircle = {
            type: 'circle',
            center,
            radius,
            stroke: typeof data.stroke === 'string' ? data.stroke : DEFAULT_STROKE,
            fill: typeof data.fill === 'string' ? data.fill : null,
            width: ensureNumber(data.width) ?? DEFAULT_STROKE_WIDTH
          };
          prepared.push(item);
          return;
        }
        case 'ellipse': {
          const center = ensurePoint(data.center);
          const radiusX = ensureNumber(data.radiusX);
          const radiusY = ensureNumber(data.radiusY);
          if (!center || radiusX === null || radiusY === null || radiusX <= 0 || radiusY <= 0) {
            warnings.push(`Ellipse in ${ctx} requires center plus positive radiusX and radiusY.`);
            return;
          }
          const item: PrimitiveEllipse = {
            type: 'ellipse',
            center,
            radiusX,
            radiusY,
            stroke: typeof data.stroke === 'string' ? data.stroke : DEFAULT_STROKE,
            fill: typeof data.fill === 'string' ? data.fill : null,
            width: ensureNumber(data.width) ?? DEFAULT_STROKE_WIDTH
          };
          prepared.push(item);
          return;
        }
        case 'polygon': {
          const points = ensurePoints(data.points);
          if (!points) {
            warnings.push(`Polygon in ${ctx} requires at least 3 numeric points.`);
            return;
          }
          const item: PrimitivePolygon = {
            type: 'polygon',
            points,
            stroke: typeof data.stroke === 'string' ? data.stroke : DEFAULT_STROKE,
            fill: typeof data.fill === 'string' ? data.fill : null,
            width: ensureNumber(data.width) ?? DEFAULT_STROKE_WIDTH
          };
          prepared.push(item);
          return;
        }
        case 'text': {
          const position = ensurePoint(data.position);
          const textValue = typeof data.text === 'string' ? data.text : null;
          if (!position || textValue === null) {
            warnings.push(`Text in ${ctx} requires position and text.`);
            return;
          }
          const align = data.align === 'right' || data.align === 'center' ? data.align : 'left';
          const item: PrimitiveText = {
            type: 'text',
            position,
            text: textValue,
            color: typeof data.color === 'string' ? data.color : '#e2e8f0',
            fontSize: ensureNumber(data.fontSize) ?? 1,
            align
          };
          prepared.push(item);
          return;
        }
        default:
          warnings.push(`No renderer for primitive type "${primitive.type}" (${ctx}).`);
      }
    });
    preparedLayers.push(prepared);
  });

  return { layers: preparedLayers, warnings };
};

const projectPointBuilder = (extent: ViewExtent, canvasWidth: number, canvasHeight: number, padding: number) => {
  const viewWidth = extent.maxX - extent.minX;
  const viewHeight = extent.maxY - extent.minY;
  const scaleX = (canvasWidth - padding * 2) / viewWidth;
  const scaleY = (canvasHeight - padding * 2) / viewHeight;
  const scale = Math.max(0.0001, Math.min(scaleX, scaleY));
  const drawWidth = viewWidth * scale;
  const drawHeight = viewHeight * scale;
  const originX = (canvasWidth - drawWidth) / 2 - extent.minX * scale;
  const originY = (canvasHeight - drawHeight) / 2 - extent.minY * scale;
  return {
    scale,
    project(point: [number, number]) {
      const [tx, ty] = point;
      const x = originX + tx * scale;
      const y = originY + ty * scale;
      return { x, y };
    }
  };
};

export const renderGraphicsToCanvas = (
  canvas: HTMLCanvasElement,
  extent: ViewExtent,
  graphics: PreparedGraphics,
  options?: {
    background?: string;
    gridColor?: string;
    padding?: number;
  }
) => {
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Canvas 2D context is not available.');
  }
  const width = canvas.width;
  const height = canvas.height;
  const padding = options?.padding ?? 48;
  const appearanceBackground = options?.background ?? DEFAULT_BACKGROUND;
  const appearanceGrid = options?.gridColor ?? DEFAULT_GRID_COLOR;
  ctx.save();
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = appearanceBackground;
  ctx.fillRect(0, 0, width, height);

  const projection = projectPointBuilder(extent, width, height, padding);
  const { project, scale } = projection;

  ctx.strokeStyle = appearanceGrid;
  ctx.lineWidth = 1;
  ctx.setLineDash([4, 6]);

  if (extent.minY <= 0 && extent.maxY >= 0) {
    const left = project([extent.minX, 0]);
    const right = project([extent.maxX, 0]);
    ctx.beginPath();
    ctx.moveTo(left.x, left.y);
    ctx.lineTo(right.x, right.y);
    ctx.stroke();
  }
  if (extent.minX <= 0 && extent.maxX >= 0) {
    const bottom = project([0, extent.minY]);
    const top = project([0, extent.maxY]);
    ctx.beginPath();
    ctx.moveTo(bottom.x, bottom.y);
    ctx.lineTo(top.x, top.y);
    ctx.stroke();
  }

  ctx.setLineDash([]);

  const applyStroke = (primitive: { stroke?: string | null; width?: number }) => {
    if (primitive.stroke && primitive.width && primitive.width > 0) {
      ctx.strokeStyle = primitive.stroke;
      ctx.lineWidth = Math.max(1, primitive.width * scale);
    } else {
      ctx.strokeStyle = 'transparent';
      ctx.lineWidth = 0;
    }
  };

  graphics.layers.forEach((layer) => {
    layer.forEach((primitive) => {
      switch (primitive.type) {
        case 'line': {
          const start = project(primitive.from);
          const end = project(primitive.to);
          applyStroke(primitive);
          if (primitive.dash && primitive.dash.length > 0) {
            ctx.setLineDash(primitive.dash.map((segment) => Math.max(0, segment) * scale));
          } else {
            ctx.setLineDash([]);
          }
          ctx.lineCap = 'round';
          ctx.lineJoin = 'round';
          ctx.beginPath();
          ctx.moveTo(start.x, start.y);
          ctx.lineTo(end.x, end.y);
          ctx.stroke();
          ctx.setLineDash([]);
          break;
        }
        case 'rect': {
          const [x, y] = primitive.position;
          const [w, h] = primitive.size;
          const p1 = project([x, y]);
          const p2 = project([x + w, y]);
          const p3 = project([x + w, y + h]);
          const p4 = project([x, y + h]);
          if (primitive.fill) {
            ctx.fillStyle = primitive.fill;
            ctx.beginPath();
            ctx.moveTo(p1.x, p1.y);
            ctx.lineTo(p2.x, p2.y);
            ctx.lineTo(p3.x, p3.y);
            ctx.lineTo(p4.x, p4.y);
            ctx.closePath();
            ctx.fill();
          }
          if (primitive.stroke && primitive.width && primitive.width > 0) {
            ctx.strokeStyle = primitive.stroke;
            ctx.lineWidth = Math.max(1, primitive.width * scale);
            ctx.beginPath();
            ctx.moveTo(p1.x, p1.y);
            ctx.lineTo(p2.x, p2.y);
            ctx.lineTo(p3.x, p3.y);
            ctx.lineTo(p4.x, p4.y);
            ctx.closePath();
            ctx.stroke();
          }
          break;
        }
        case 'circle': {
          const center = project(primitive.center);
          const radius = Math.max(0, primitive.radius * scale);
          ctx.beginPath();
          ctx.arc(center.x, center.y, radius, 0, Math.PI * 2);
          if (primitive.fill) {
            ctx.fillStyle = primitive.fill;
            ctx.fill();
          }
          if (primitive.stroke && primitive.width && primitive.width > 0) {
            ctx.strokeStyle = primitive.stroke;
            ctx.lineWidth = Math.max(1, primitive.width * scale);
            ctx.stroke();
          }
          break;
        }
        case 'ellipse': {
          const center = project(primitive.center);
          const radiusX = Math.max(0, primitive.radiusX * scale);
          const radiusY = Math.max(0, primitive.radiusY * scale);
          ctx.beginPath();
          ctx.ellipse(center.x, center.y, radiusX, radiusY, 0, 0, Math.PI * 2);
          if (primitive.fill) {
            ctx.fillStyle = primitive.fill;
            ctx.fill();
          }
          if (primitive.stroke && primitive.width && primitive.width > 0) {
            ctx.strokeStyle = primitive.stroke;
            ctx.lineWidth = Math.max(1, primitive.width * scale);
            ctx.stroke();
          }
          break;
        }
        case 'polygon': {
          if (primitive.points.length < 3) {
            break;
          }
          ctx.beginPath();
          primitive.points.forEach((point, index) => {
            const projected = project(point);
            if (index === 0) {
              ctx.moveTo(projected.x, projected.y);
            } else {
              ctx.lineTo(projected.x, projected.y);
            }
          });
          ctx.closePath();
          if (primitive.fill) {
            ctx.fillStyle = primitive.fill;
            ctx.fill();
          }
          if (primitive.stroke && primitive.width && primitive.width > 0) {
            ctx.strokeStyle = primitive.stroke;
            ctx.lineWidth = Math.max(1, primitive.width * scale);
            ctx.stroke();
          }
          break;
        }
        case 'text': {
          const position = project(primitive.position);
          const fontSize = Math.max(12, primitive.fontSize * scale);
          ctx.fillStyle = primitive.color || '#e2e8f0';
          ctx.font = `${fontSize}px Inter, system-ui, sans-serif`;
          ctx.textAlign = primitive.align === 'center' ? 'center' : primitive.align === 'right' ? 'right' : 'left';
          ctx.textBaseline = 'middle';
          ctx.fillText(primitive.text, position.x, position.y);
          break;
        }
        default:
          break;
      }
    });
  });

  ctx.restore();
};

export { toPlainValue };

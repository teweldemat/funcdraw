import {
  DefaultFsDataProvider,
  Engine,
  FSDataType,
  FsDataProvider,
  FsError,
  FsList,
  FuncScriptParser,
  KeyValueCollection,
  type TypedValue
} from '@tewelde/funcscript/browser';
import type { SyntaxErrorData } from '@tewelde/funcscript/parser';
import type { ExpressionLanguage, FuncDrawErrorDetail } from '@funcdraw/core';

export type PrimitiveType = 'line' | 'rect' | 'circle' | 'polygon' | 'text';

const DEFAULT_STROKE = '#38bdf8';
const DEFAULT_STROKE_WIDTH = 0.25;

export type Primitive = {
  type: PrimitiveType | string;
  data: Record<string, unknown>;
};

export type PrimitiveLayer = Primitive[];

export type ViewExtent = {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
};

export type PreparedLine = {
  type: 'line';
  from: [number, number];
  to: [number, number];
  stroke: string;
  width: number;
  dash: number[] | null;
};

export type PreparedRect = {
  type: 'rect';
  position: [number, number];
  size: [number, number];
  stroke: string | null;
  fill: string | null;
  width: number;
};

export type PreparedCircle = {
  type: 'circle';
  center: [number, number];
  radius: number;
  stroke: string | null;
  fill: string | null;
  width: number;
};

export type PreparedPolygon = {
  type: 'polygon';
  points: Array<[number, number]>;
  stroke: string | null;
  fill: string | null;
  width: number;
};

export type PreparedText = {
  type: 'text';
  position: [number, number];
  text: string;
  color: string;
  fontSize: number;
  align: CanvasTextAlign;
};

export type PreparedPrimitive =
  | PreparedLine
  | PreparedRect
  | PreparedCircle
  | PreparedPolygon
  | PreparedText;

export type PreparedGraphics = {
  layers: PreparedPrimitive[][];
  warnings: string[];
};

export type EvaluationResult = {
  value: unknown;
  typed: TypedValue | null;
  error: string | null;
  errorDetails: FuncDrawErrorDetail[] | null;
};

export type ViewInterpretation = {
  extent: ViewExtent | null;
  warning: string | null;
};

export type GraphicsInterpretation = {
  layers: PrimitiveLayer[] | null;
  warning: string | null;
  unknownTypes: string[];
};

export const defaultViewExpression = `{
  return { minX:-10, minY:-10, maxX:10, maxY:10 };
}`;

export const defaultGraphicsExpression = `{
  baseColor:'#38bdf8';
  accent:'#f97316';
  background:'#0f172a';
  return [
    {
      type:'rect',
      position:[-12,-8],
      size:[24,16],
      fill:'rgba(15, 23, 42, 0.55)',
      stroke:'rgba(148, 163, 184, 0.6)',
      width:0.3
    },
    {
      type:'polygon',
      points:[[-6,-4],[0,9],[6,-4]],
      fill:'rgba(56, 189, 248, 0.45)',
      stroke:baseColor,
      width:0.4
    },
    {
      type:'circle',
      center:[4,-1],
      radius:5,
      stroke:accent,
      width:0.35
    },
    {
      type:'text',
      position:[0,-6.5],
      text:'FuncScript',
      color:'#e2e8f0',
      fontSize:1.4,
      align:'center'
    }
  ];
}`;

const toPlainValue = (value: TypedValue | null): unknown => {
  if (!value) {
    return null;
  }

  const type = Engine.typeOf(value);
  const raw = Engine.valueOf(value);

  switch (type) {
    case FSDataType.Null:
    case FSDataType.Boolean:
    case FSDataType.Integer:
    case FSDataType.Float:
    case FSDataType.String:
    case FSDataType.BigInteger:
    case FSDataType.Guid:
    case FSDataType.DateTime:
      return raw;
    case FSDataType.List: {
      if (raw && typeof (raw as { __jsValue?: unknown }).__jsValue !== 'undefined') {
        return (raw as { __jsValue?: unknown }).__jsValue;
      }
      const list = raw as FsList;
      const entries: unknown[] = [];
      const toArray = (list as unknown as { toArray?: () => TypedValue[] }).toArray;
      if (list && typeof toArray === 'function') {
        for (const entry of toArray.call(list)) {
          entries.push(toPlainValue(entry));
        }
        return entries;
      }
      if (typeof Symbol !== 'undefined' && Symbol.iterator in (list as object)) {
        for (const entry of list as unknown as Iterable<TypedValue>) {
          entries.push(toPlainValue(entry));
        }
        return entries;
      }
      return null;
    }
    case FSDataType.KeyValueCollection: {
      if (raw && typeof (raw as { __jsValue?: unknown }).__jsValue !== 'undefined') {
        return (raw as { __jsValue?: unknown }).__jsValue;
      }
      const collection = raw as KeyValueCollection;
      if (collection && typeof collection.getAll === 'function') {
        const result: Record<string, unknown> = {};
        for (const [key, typed] of collection.getAll()) {
          result[key] = toPlainValue(typed);
        }
        return result;
      }
      return raw;
    }
    case FSDataType.Error: {
      const error = raw as FsError;
      return {
        type: error?.errorType ?? 'Error',
        message: error?.errorMessage ?? 'Unknown error',
        data: error?.errorData ?? null
      };
    }
    case FSDataType.Function:
      return '[Function]';
    case FSDataType.ValRef:
      return '[ValRef]';
    case FSDataType.ValSink:
      return '[ValSink]';
    case FSDataType.SigSource:
      return '[SigSource]';
    case FSDataType.SigSink:
      return '[SigSink]';
    default:
      return raw;
  }
};

const DEFAULT_LANGUAGE: ExpressionLanguage = 'funcscript';

const MAX_CONTEXT_WIDTH = 96;

type SourceLineEntry = {
  line: number;
  start: number;
  end: number;
  text: string;
};

const buildLineIndex = (source: string): SourceLineEntry[] => {
  const entries: SourceLineEntry[] = [];
  let start = 0;
  let line = 1;
  for (let i = 0; i < source.length; i += 1) {
    const ch = source[i];
    if (ch === '\n' || ch === '\r') {
      entries.push({ line, start, end: i, text: source.slice(start, i) });
      if (ch === '\r' && source[i + 1] === '\n') {
        i += 1;
      }
      start = i + 1;
      line += 1;
    }
  }
  entries.push({ line, start, end: source.length, text: source.slice(start) });
  return entries;
};

const clampIndex = (entries: SourceLineEntry[], target: number): number => {
  if (!Number.isFinite(target)) {
    return 0;
  }
  if (entries.length === 0) {
    return Math.max(0, Math.trunc(target));
  }
  const last = entries[entries.length - 1];
  const maxIndex = Math.max(last.end - 1, last.start);
  if (maxIndex < 0) {
    return 0;
  }
  const clamped = Math.trunc(target);
  if (clamped < 0) {
    return 0;
  }
  if (clamped > maxIndex) {
    return maxIndex;
  }
  return clamped;
};

const findLineEntry = (entries: SourceLineEntry[], target: number): SourceLineEntry => {
  if (entries.length === 0) {
    return { line: 1, start: 0, end: 0, text: '' };
  }
  const index = clampIndex(entries, target);
  for (const entry of entries) {
    if (index >= entry.start && index < entry.end) {
      return entry;
    }
  }
  return entries[entries.length - 1];
};

const buildContextSegment = (
  lineText: string,
  column: number,
  span: number
): FuncDrawErrorDetail['context'] => {
  if (!lineText) {
    return null;
  }
  const safeColumn = column > 0 ? column : 1;
  const safeSpan = span > 0 ? span : 1;
  const maxStart = Math.max(0, lineText.length - MAX_CONTEXT_WIDTH);
  const idealStart = Math.max(0, safeColumn - 1 - Math.floor(MAX_CONTEXT_WIDTH / 2));
  const start = Math.min(idealStart, maxStart);
  const end = Math.min(lineText.length, start + MAX_CONTEXT_WIDTH);
  const prefix = start > 0 ? '...' : '';
  const suffix = end < lineText.length ? '...' : '';
  const visible = `${prefix}${lineText.slice(start, end)}${suffix}`;
  const caretColumn = safeColumn - start + prefix.length;
  const availableSpan = Math.max(1, visible.length - Math.max(0, caretColumn - 1));
  const pointerSpan = Math.max(1, Math.min(safeSpan, availableSpan));
  const pointer = `${' '.repeat(Math.max(0, caretColumn - 1))}${'^'}${pointerSpan > 1 ? '~'.repeat(pointerSpan - 1) : ''}`;
  return {
    lineText: visible,
    pointerText: pointer
  };
};

const describeSourceLocation = (
  source: string,
  index: number,
  length: number,
  entries: SourceLineEntry[]
): { location: FuncDrawErrorDetail['location']; context: FuncDrawErrorDetail['context'] } => {
  if (!source) {
    return { location: null, context: null };
  }
  const entry = findLineEntry(entries, index);
  const normalizedIndex = clampIndex(entries, index);
  const column = Math.max(1, normalizedIndex - entry.start + 1);
  const location = {
    index: normalizedIndex,
    line: entry.line,
    column,
    length: length > 0 ? length : 1
  };
  const context = buildContextSegment(entry.text, column, location.length);
  return { location, context };
};

const createParserErrorDetail = (
  source: string,
  syntaxError: SyntaxErrorData,
  entries: SourceLineEntry[]
): FuncDrawErrorDetail | null => {
  if (!syntaxError || !syntaxError.Message) {
    return null;
  }
  const loc = typeof syntaxError.Loc === 'number' ? syntaxError.Loc : 0;
  const len = typeof syntaxError.Length === 'number' ? syntaxError.Length : 1;
  const { location, context } = describeSourceLocation(source, loc, len, entries);
  return {
    kind: 'funcscript-parser',
    message: syntaxError.Message,
    location,
    context,
    stack: null
  };
};

const collectParserErrorDetails = (
  provider: FsDataProvider,
  source: string
): FuncDrawErrorDetail[] | null => {
  if (!source) {
    return null;
  }
  const errors: SyntaxErrorData[] = [];
  try {
    FuncScriptParser.parse(provider, source, errors);
  } catch (err) {
    return [createGenericErrorDetail(err, 'funcscript-parser')];
  }
  if (errors.length === 0) {
    return null;
  }
  const entries = buildLineIndex(source);
  const details = errors
    .map((syntaxError) => createParserErrorDetail(source, syntaxError, entries))
    .filter((detail): detail is FuncDrawErrorDetail => Boolean(detail));
  return details.length > 0 ? details : null;
};

const createGenericErrorDetail = (
  err: unknown,
  kind: FuncDrawErrorDetail['kind']
): FuncDrawErrorDetail => ({
  kind,
  message: err instanceof Error ? err.message : String(err),
  location: null,
  context: null,
  stack: err instanceof Error && typeof err.stack === 'string' ? err.stack : null
});

const isDefinedInProvider = (provider: FsDataProvider, name: string): boolean => {
  if (typeof provider.isDefined === 'function') {
    try {
      return provider.isDefined(name);
    } catch {
      return false;
    }
  }
  try {
    return provider.get(name) !== null;
  } catch {
    return false;
  }
};

const getTypedValue = (provider: FsDataProvider, name: string): TypedValue | null => {
  try {
    return provider.get(name) ?? null;
  } catch {
    return null;
  }
};

const JS_VALUE_SENTINEL = Symbol.for('funcdraw.js.value');
type JsValueCapableProvider = FsDataProvider & {
  getJsValue: (identifier: string) => unknown;
};

const hasJsValueGetter = (provider: FsDataProvider | null | undefined): provider is JsValueCapableProvider => {
  return !!provider && typeof (provider as { getJsValue?: unknown }).getJsValue === 'function';
};

export class PlayerFsDataProvider extends DefaultFsDataProvider {
  private jsValues = new Map<string, { marker: symbol; value: unknown }>();

  setJsValue(name: string, value: unknown) {
    if (typeof name !== 'string') {
      return;
    }
    const trimmed = name.trim();
    if (!trimmed) {
      return;
    }
    const key = trimmed.toLowerCase();
    if (value === undefined) {
      this.jsValues.delete(key);
      return;
    }
    this.jsValues.set(key, { marker: JS_VALUE_SENTINEL, value });
  }

  clearJsValue(name: string) {
    if (typeof name !== 'string') {
      return;
    }
    const key = name.trim().toLowerCase();
    if (!key) {
      return;
    }
    this.jsValues.delete(key);
  }

  getJsValue(name: string) {
    if (typeof name !== 'string') {
      return undefined;
    }
    const key = name.trim().toLowerCase();
    if (!key) {
      return undefined;
    }
    if (this.jsValues.has(key)) {
      return this.jsValues.get(key);
    }
    const parent = this.parent;
    if (hasJsValueGetter(parent)) {
      return parent.getJsValue(name);
    }
    return undefined;
  }
}

const getBuiltinGlobal = (prop: PropertyKey): unknown => {
  if (typeof prop !== 'string') {
    return undefined;
  }
  switch (prop) {
    case 'Math':
      return globalThis.Math;
    default:
      return undefined;
  }
};

const tryGetJsBinding = (
  provider: FsDataProvider,
  name: string
): { hasValue: boolean; value: unknown } => {
  const candidate = provider as FsDataProvider & { getJsValue?: (identifier: string) => unknown };
  if (!candidate || typeof candidate.getJsValue !== 'function') {
    return { hasValue: false, value: undefined };
  }
  try {
    const result = candidate.getJsValue(name);
    if (result && typeof result === 'object' && (result as { marker?: unknown }).marker === JS_VALUE_SENTINEL) {
      return { hasValue: true, value: (result as { value?: unknown }).value };
    }
  } catch {
    return { hasValue: false, value: undefined };
  }
  return { hasValue: false, value: undefined };
};

const createJavaScriptScope = (provider: FsDataProvider) => {
  const cache = new Map<string, unknown>();
  return new Proxy(Object.create(null), {
    has: (_target, prop) => {
      if (prop === Symbol.unscopables) {
        return false;
      }
      if (getBuiltinGlobal(prop) !== undefined) {
        return true;
      }
      if (typeof prop === 'string') {
        const jsBinding = tryGetJsBinding(provider, prop);
        if (jsBinding.hasValue) {
          return true;
        }
        return isDefinedInProvider(provider, prop);
      }
      return false;
    },
    get: (_target, prop) => {
      if (prop === Symbol.unscopables || typeof prop !== 'string') {
        return undefined;
      }
      const builtin = getBuiltinGlobal(prop);
      if (builtin !== undefined) {
        return builtin;
      }
      const jsBinding = tryGetJsBinding(provider, prop);
      if (jsBinding.hasValue) {
        cache.set(prop, jsBinding.value);
        return jsBinding.value;
      }
      if (!isDefinedInProvider(provider, prop)) {
        return undefined;
      }
      if (cache.has(prop)) {
        return cache.get(prop);
      }
      const value = toPlainValue(getTypedValue(provider, prop));
      cache.set(prop, value);
      return value;
    },
    set() {
      throw new Error('Cannot assign to FuncDraw bindings inside a JavaScript expression.');
    },
    deleteProperty() {
      throw new Error('Cannot delete FuncDraw bindings inside a JavaScript expression.');
    }
  });
};

const JS_EXECUTOR_CACHE_LIMIT = 64;
type JsExecutorEntry = {
  source: string;
  executor: (scope: unknown) => unknown;
};

const jsExecutorCache = new Map<string, JsExecutorEntry>();

const buildJavaScriptExecutor = (source: string): ((scope: unknown) => unknown) => {
  const body = [
    'return (function() {',
    '  with (scope) {',
    '    return (function() {',
    source,
    '    }).call(scope);',
    '  }',
    '}).call(scope);'
  ].join('\n');
  return new Function('scope', body) as (scope: unknown) => unknown;
};

const getJavaScriptExecutor = (source: string): ((scope: unknown) => unknown) => {
  const cached = jsExecutorCache.get(source);
  if (cached && cached.source === source) {
    return cached.executor;
  }
  const executor = buildJavaScriptExecutor(source);
  jsExecutorCache.set(source, { source, executor });
  if (jsExecutorCache.size > JS_EXECUTOR_CACHE_LIMIT) {
    const firstKey = jsExecutorCache.keys().next().value;
    if (firstKey !== undefined) {
      jsExecutorCache.delete(firstKey);
    }
  }
  return executor;
};

const executeJavaScriptExpression = (source: string, provider: FsDataProvider): unknown => {
  const executor = getJavaScriptExecutor(source);
  const scope = createJavaScriptScope(provider);
  return executor(scope);
};

const ensureNumber = (value: unknown): number | null =>
  typeof value === 'number' && Number.isFinite(value) ? value : null;

const mapIterableToPlainArray = (iterable: Iterable<unknown>): unknown[] | null => {
  const result: unknown[] = [];
  try {
    for (const entry of iterable) {
      result.push(toPlainValue(entry as TypedValue));
    }
    return result;
  } catch {
    return null;
  }
};

const toArrayLike = (value: unknown): unknown[] | null => {
  if (Array.isArray(value)) {
    return value;
  }
  if (value && typeof value === 'object') {
    const candidate = value as { toArray?: () => unknown[] } & Iterable<unknown>;
    if (typeof candidate.toArray === 'function') {
      try {
        const result = candidate.toArray();
        if (Array.isArray(result)) {
          return result.map((entry) => toPlainValue(entry as TypedValue));
        }
      } catch {
        return null;
      }
    }
    if (Symbol.iterator in candidate) {
      return mapIterableToPlainArray(candidate as Iterable<unknown>);
    }
  }
  return null;
};

const ensurePoint = (value: unknown): [number, number] | null => {
  const tuple = toArrayLike(value);
  if (!tuple || tuple.length !== 2) {
    console.warn('[ensurePoint] invalid point tuple', value);
    return null;
  }
  const [x, y] = tuple;
  if (typeof x === 'number' && Number.isFinite(x) && typeof y === 'number' && Number.isFinite(y)) {
    return [x, y];
  }
  console.warn('[ensurePoint] non-numeric entries', value);
  return null;
};

const ensurePoints = (value: unknown): Array<[number, number]> | null => {
  const entries = toArrayLike(value);
  if (!entries || entries.length < 3) {
    return null;
  }
  const points: Array<[number, number]> = [];
  for (const entry of entries) {
    const point = ensurePoint(entry);
    if (!point) {
      return null;
    }
    points.push(point);
  }
  return points;
};

const toPlainRecord = (value: unknown): Record<string, unknown> | null => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return null;
  }
  const candidate = value as Record<string, unknown> & { getAll?: () => Iterable<[string, unknown]> };
  if (typeof candidate.getAll === 'function') {
    const result: Record<string, unknown> = {};
    try {
      for (const [key, entry] of candidate.getAll()) {
        result[key] = toPlainValue(entry as TypedValue);
      }
      return result;
    } catch {
      return null;
    }
  }
  return candidate;
};

export const evaluateExpression = (
  provider: FsDataProvider,
  expression: string,
  language: ExpressionLanguage = DEFAULT_LANGUAGE
): EvaluationResult => {
  const trimmed = expression.trim();
  if (!trimmed) {
    return {
      value: null,
      typed: null,
      error: null,
      errorDetails: null
    };
  }

  try {
    let typed: TypedValue | null = null;
    let value: unknown = null;
    if (language === 'javascript') {
      const jsValue = executeJavaScriptExpression(trimmed, provider);
      value = jsValue === undefined ? null : jsValue;
    } else {
      typed = Engine.evaluate(trimmed, provider);
      value = toPlainValue(typed);
    }
    return { value, typed, error: null, errorDetails: null };
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    let errorDetails: FuncDrawErrorDetail[] | null = null;
    if (language === 'funcscript') {
      errorDetails = collectParserErrorDetails(provider, trimmed);
    }
    if (!errorDetails || errorDetails.length === 0) {
      const kind: FuncDrawErrorDetail['kind'] = language === 'javascript' ? 'javascript' : 'funcscript-runtime';
      errorDetails = [createGenericErrorDetail(err, kind)];
    }
    return {
      value: null,
      typed: null,
      error: message,
      errorDetails
    };
  }
};

export const interpretView = (value: unknown): ViewInterpretation => {
  if (value === null || value === undefined) {
    return { extent: null, warning: 'View expression returned null. Provide numeric bounds.' };
  }
  if (Array.isArray(value) || typeof value !== 'object') {
    return { extent: null, warning: 'View expression must return { minX, minY, maxX, maxY }.' };
  }
  const record = value as Record<string, unknown>;
  const minX = ensureNumber(record.minX);
  const maxX = ensureNumber(record.maxX);
  const minY = ensureNumber(record.minY);
  const maxY = ensureNumber(record.maxY);
  if (minX === null || maxX === null || minY === null || maxY === null) {
    return {
      extent: null,
      warning: 'All extent fields must be finite numbers.'
    };
  }
  if (maxX <= minX || maxY <= minY) {
    return {
      extent: null,
      warning: 'Extent must define a positive width and height (max > min).'
    };
  }
  return {
    extent: { minX, maxX, minY, maxY },
    warning: null
  };
};

const describeError = (err: unknown): string => (err instanceof Error ? err.message : String(err));

const collectPrimitives = (
  node: unknown,
  path: string,
  warnings: string[],
  unknownTypes: Set<string>
): Primitive[] => {
  const location = path || 'root';

  if (Array.isArray(node)) {
    const primitives: Primitive[] = [];
    node.forEach((child, index) => {
      primitives.push(...collectPrimitives(child, `${path}[${index}]`, warnings, unknownTypes));
    });
    return primitives;
  }

  if (node && typeof node === 'object') {
    const normalizedNode = toPlainRecord(node);
    if (!normalizedNode) {
      warnings.push(`Primitive at ${location} must be an object or key/value collection.`);
      return [];
    }

    const { type, data, transform, ...rest } = normalizedNode as {
      type?: unknown;
      data?: unknown;
      transform?: unknown;
      [key: string]: unknown;
    };

    if (data !== undefined) {
      warnings.push(
        `Primitive at ${location} should place drawing fields directly on the object. The 'data' wrapper is no longer supported.`
      );
      return [];
    }

    if (typeof type === 'string') {
      if (!['line', 'rect', 'circle', 'polygon', 'text'].includes(type)) {
        unknownTypes.add(type);
      }
      if (transform !== undefined && transform !== null) {
        warnings.push(
          `Primitive at ${location} includes a transform, but transforms are no longer supported and will be ignored.`
        );
      }

      return [
        {
          type,
          data: rest
        }
      ];
    }

    warnings.push(`Primitive at ${location} must include string 'type'.`);
    return [];
  }

  warnings.push(`Skipping graphics entry at ${location} because it is not a list or object.`);
  return [];
};

export const interpretGraphics = (value: unknown): GraphicsInterpretation => {
  if (value === null || value === undefined) {
    return {
      layers: null,
      warning: 'Graphics expression returned null. Provide a primitive or a list of primitives.',
      unknownTypes: []
    };
  }

  const warnings: string[] = [];
  const unknown = new Set<string>();
  const layers: PrimitiveLayer[] = [];

  if (Array.isArray(value)) {
    const primitives = collectPrimitives(value, 'root', warnings, unknown);
    if (primitives.length > 0) {
      layers.push(primitives);
    }
  } else if (value && typeof value === 'object') {
    const primitives = collectPrimitives(value, 'root', warnings, unknown);
    if (primitives.length > 0) {
      layers.push(primitives);
    }
  } else {
    warnings.push('Graphics expression must evaluate to an object or list of primitives.');
  }

  return {
    layers: layers.length > 0 ? layers : null,
    warning: warnings.length > 0 ? warnings.join(' ') : null,
    unknownTypes: Array.from(unknown)
  };
};

export const prepareGraphics = (
  extent: ViewExtent | null,
  layers: PrimitiveLayer[] | null
): PreparedGraphics => {
  if (!extent || !layers) {
    return {
      layers: [],
      warnings: extent ? [] : ['Cannot render without a valid view extent.']
    };
  }

  const warnings: string[] = [];
  const preparedLayers: PreparedPrimitive[][] = [];

  for (let layerIndex = 0; layerIndex < layers.length; layerIndex += 1) {
    const layer = layers[layerIndex];
    const prepared: PreparedPrimitive[] = [];

    for (let primitiveIndex = 0; primitiveIndex < layer.length; primitiveIndex += 1) {
      const primitive = layer[primitiveIndex];
      const ctx = `layer ${layerIndex + 1}, primitive ${primitiveIndex + 1}`;

      try {
        switch (primitive.type) {
          case 'line': {
            const from = ensurePoint(primitive.data.from);
            const to = ensurePoint(primitive.data.to);
            if (!from || !to) {
              warnings.push(`Line in ${ctx} requires numeric from/to points.`);
              break;
            }
            const stroke = typeof primitive.data.stroke === 'string' ? primitive.data.stroke : DEFAULT_STROKE;
            const width = ensureNumber(primitive.data.width) ?? DEFAULT_STROKE_WIDTH;
            const dash = Array.isArray(primitive.data.dash)
              ? primitive.data.dash.every((segment) => typeof segment === 'number' && segment >= 0)
                ? (primitive.data.dash as number[])
                : null
              : null;
            prepared.push({
              type: 'line',
              from,
              to,
              stroke,
              width,
              dash
            });
            break;
          }
          case 'rect': {
            const position = ensurePoint(primitive.data.position);
            const size = ensurePoint(primitive.data.size);
            if (!position || !size) {
              console.warn('[collectPrimitives] invalid rect data', ctx, primitive.data);
              warnings.push(`Rectangle in ${ctx} requires position and size points.`);
              break;
            }
            const stroke = typeof primitive.data.stroke === 'string' ? primitive.data.stroke : DEFAULT_STROKE;
            const fill = typeof primitive.data.fill === 'string' ? primitive.data.fill : null;
            const width = ensureNumber(primitive.data.width) ?? DEFAULT_STROKE_WIDTH;
            prepared.push({
              type: 'rect',
              position,
              size,
              stroke,
              fill,
              width
            });
            break;
          }
          case 'circle': {
            const center = ensurePoint(primitive.data.center);
            const radius = ensureNumber(primitive.data.radius);
            if (!center || radius === null || radius <= 0) {
              warnings.push(`Circle in ${ctx} requires center and positive radius.`);
              break;
            }
            const stroke = typeof primitive.data.stroke === 'string' ? primitive.data.stroke : DEFAULT_STROKE;
            const fill = typeof primitive.data.fill === 'string' ? primitive.data.fill : null;
            const width = ensureNumber(primitive.data.width) ?? DEFAULT_STROKE_WIDTH;
            prepared.push({
              type: 'circle',
              center,
              radius,
              stroke,
              fill,
              width
            });
            break;
          }
          case 'polygon': {
            const points = ensurePoints(primitive.data.points);
            if (!points) {
              warnings.push(`Polygon in ${ctx} requires an array of at least 3 numeric points.`);
              break;
            }
            const stroke = typeof primitive.data.stroke === 'string' ? primitive.data.stroke : DEFAULT_STROKE;
            const fill = typeof primitive.data.fill === 'string' ? primitive.data.fill : null;
            const width = ensureNumber(primitive.data.width) ?? DEFAULT_STROKE_WIDTH;
            prepared.push({
              type: 'polygon',
              points,
              stroke,
              fill,
              width
            });
            break;
          }
          case 'text': {
            const position = ensurePoint(primitive.data.position);
            const text = typeof primitive.data.text === 'string' ? primitive.data.text : null;
            if (!position || text === null) {
              warnings.push(`Text in ${ctx} requires position and text.`);
              break;
            }
            const color = typeof primitive.data.color === 'string' ? primitive.data.color : '#e2e8f0';
            const fontSize = ensureNumber(primitive.data.fontSize) ?? 1;
            const alignValue = primitive.data.align;
            const align: CanvasTextAlign = alignValue === 'right' || alignValue === 'center' ? alignValue : 'left';
            prepared.push({
              type: 'text',
              position,
              text,
              color,
              fontSize,
              align
            });
            break;
          }
          default:
            warnings.push(`No renderer for primitive type "${primitive.type}" (${ctx}).`);
            break;
        }
      } catch (err) {
        warnings.push(`Skipping ${ctx} because it threw during preparation: ${describeError(err)}.`);
      }
    }

    preparedLayers.push(prepared);
  }

  return {
    layers: preparedLayers,
    warnings
  };
};

export const projectPointBuilder = (
  extent: ViewExtent,
  canvasWidth: number,
  canvasHeight: number,
  padding: number
) => {
  const viewWidth = extent.maxX - extent.minX;
  const viewHeight = extent.maxY - extent.minY;
  const scaleX = (canvasWidth - padding * 2) / viewWidth;
  const scaleY = (canvasHeight - padding * 2) / viewHeight;
  const scale = Math.max(0.0001, Math.min(scaleX, scaleY));

  const drawWidth = viewWidth * scale;
  const drawHeight = viewHeight * scale;
  const originX = (canvasWidth - drawWidth) / 2 - extent.minX * scale;
  const originY = (canvasHeight - drawHeight) / 2 + extent.maxY * scale;

  return {
    scale,
    project(point: [number, number]) {
      const [tx, ty] = point;
      const x = originX + tx * scale;
      const y = originY - ty * scale;
      return { x, y };
    }
  };
};

export const prepareProvider = (): PlayerFsDataProvider => new PlayerFsDataProvider();

export type SvgRenderOptions = {
  width: number;
  height: number;
  padding: number;
  background?: string;
  gridColor?: string;
};

const DEFAULT_SVG_BACKGROUND = '#0f172a';
const DEFAULT_SVG_GRID = 'rgba(148, 163, 184, 0.2)';

const escapeXmlAttr = (value: string) =>
  value
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

const escapeXmlText = (value: string) =>
  value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

const formatSvgNumber = (value: number) => {
  if (!Number.isFinite(value)) {
    return '0';
  }
  const rounded = Math.round(value * 1000) / 1000;
  const text = rounded.toFixed(3);
  return text.replace(/\.?(?:0{1,3})$/, '');
};

export const renderSvgDocument = (
  extent: ViewExtent | null,
  graphics: PreparedGraphics,
  options: SvgRenderOptions
): string => {
  if (!extent) {
    throw new Error('Cannot export SVG without a valid view extent.');
  }
  if (!graphics.layers || graphics.layers.length === 0) {
    throw new Error('Graphics expression did not return any primitives.');
  }

  const width = Math.max(1, Math.round(options.width));
  const height = Math.max(1, Math.round(options.height));
  const padding = Math.max(0, options.padding);
  const background = options.background ?? DEFAULT_SVG_BACKGROUND;
  const gridColor = options.gridColor ?? DEFAULT_SVG_GRID;

  const projector = projectPointBuilder(extent, width, height, padding);
  const { project, scale } = projector;

  const drawAxis = (): string[] => {
    const axisLines: string[] = [];
    if (extent.minY <= 0 && extent.maxY >= 0) {
      const left = project([extent.minX, 0]);
      const right = project([extent.maxX, 0]);
      axisLines.push(
        `<line x1="${formatSvgNumber(left.x)}" y1="${formatSvgNumber(left.y)}" x2="${formatSvgNumber(
          right.x
        )}" y2="${formatSvgNumber(right.y)}" stroke="${escapeXmlAttr(
          gridColor
        )}" stroke-width="1" stroke-dasharray="4 6" />`
      );
    }
    if (extent.minX <= 0 && extent.maxX >= 0) {
      const bottom = project([0, extent.minY]);
      const top = project([0, extent.maxY]);
      axisLines.push(
        `<line x1="${formatSvgNumber(bottom.x)}" y1="${formatSvgNumber(bottom.y)}" x2="${formatSvgNumber(
          top.x
        )}" y2="${formatSvgNumber(top.y)}" stroke="${escapeXmlAttr(
          gridColor
        )}" stroke-width="1" stroke-dasharray="4 6" />`
      );
    }
    return axisLines;
  };

  const lineElement = (primitive: PreparedLine) => {
    const start = project(primitive.from);
    const end = project(primitive.to);
    const dash = Array.isArray(primitive.dash) && primitive.dash.length > 0
      ? ` stroke-dasharray="${primitive.dash
          .map((segment) => Math.max(0, segment) * scale)
          .map(formatSvgNumber)
          .join(' ')}"`
      : '';
    return `<line x1="${formatSvgNumber(start.x)}" y1="${formatSvgNumber(start.y)}" x2="${formatSvgNumber(
      end.x
    )}" y2="${formatSvgNumber(end.y)}" stroke="${escapeXmlAttr(
      primitive.stroke
    )}" stroke-width="${formatSvgNumber(Math.max(1, primitive.width * scale))}" stroke-linecap="round" stroke-linejoin="round"${dash} />`;
  };

  const rectElement = (primitive: PreparedRect) => {
    const [x, y] = primitive.position;
    const [w, h] = primitive.size;
    const corners = [
      project([x, y]),
      project([x + w, y]),
      project([x + w, y + h]),
      project([x, y + h])
    ];
    const points = corners.map((point) => `${formatSvgNumber(point.x)},${formatSvgNumber(point.y)}`).join(' ');
    const fill = primitive.fill ? ` fill="${escapeXmlAttr(primitive.fill)}"` : '';
    const stroke =
      primitive.stroke && primitive.width > 0
        ? ` stroke="${escapeXmlAttr(primitive.stroke)}" stroke-width="${formatSvgNumber(Math.max(1, primitive.width * scale))}"`
        : '';
    return `<polygon points="${points}"${fill}${stroke} />`;
  };

  const circleElement = (primitive: PreparedCircle) => {
    const center = project(primitive.center);
    const radius = Math.max(0, primitive.radius * scale);
    const fill = primitive.fill ? ` fill="${escapeXmlAttr(primitive.fill)}"` : ' fill="none"';
    const stroke =
      primitive.stroke && primitive.width > 0
        ? ` stroke="${escapeXmlAttr(primitive.stroke)}" stroke-width="${formatSvgNumber(Math.max(1, primitive.width * scale))}"`
        : '';
    return `<circle cx="${formatSvgNumber(center.x)}" cy="${formatSvgNumber(center.y)}" r="${formatSvgNumber(radius)}"${fill}${stroke} />`;
  };

  const polygonElement = (primitive: PreparedPolygon) => {
    if (primitive.points.length < 3) {
      return '';
    }
    const points = primitive.points
      .map((point) => {
        const projected = project(point);
        return `${formatSvgNumber(projected.x)},${formatSvgNumber(projected.y)}`;
      })
      .join(' ');
    const fill = primitive.fill ? ` fill="${escapeXmlAttr(primitive.fill)}"` : ' fill="none"';
    const stroke =
      primitive.stroke && primitive.width > 0
        ? ` stroke="${escapeXmlAttr(primitive.stroke)}" stroke-width="${formatSvgNumber(Math.max(1, primitive.width * scale))}"`
        : '';
    return `<polygon points="${points}"${fill}${stroke} />`;
  };

  const textElement = (primitive: PreparedText) => {
    const position = project(primitive.position);
    const fontSize = Math.max(12, primitive.fontSize * scale);
    const anchor = primitive.align === 'center' ? 'middle' : primitive.align === 'right' ? 'end' : 'start';
    return `<text x="${formatSvgNumber(position.x)}" y="${formatSvgNumber(position.y)}" fill="${escapeXmlAttr(
      primitive.color
    )}" font-size="${formatSvgNumber(fontSize)}" text-anchor="${anchor}" dominant-baseline="middle">${escapeXmlText(
      primitive.text
    )}</text>`;
  };

  const lines: string[] = [];
  lines.push(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" fill="none">`
  );
  lines.push(`<rect x="0" y="0" width="${width}" height="${height}" fill="${escapeXmlAttr(background)}" />`);
  lines.push(...drawAxis());

  for (const layer of graphics.layers) {
    if (!layer || layer.length === 0) {
      continue;
    }
    lines.push('<g>');
    for (const primitive of layer) {
      switch (primitive.type) {
        case 'line':
          lines.push(lineElement(primitive));
          break;
        case 'rect':
          lines.push(rectElement(primitive));
          break;
        case 'circle':
          lines.push(circleElement(primitive));
          break;
        case 'polygon':
          lines.push(polygonElement(primitive));
          break;
        case 'text':
          lines.push(textElement(primitive));
          break;
        default:
          break;
      }
    }
    lines.push('</g>');
  }

  lines.push('</svg>');
  return lines.join('');
};

const FuncScript = require('@tewelde/funcscript/browser');

const {
  Engine,
  DefaultFsDataProvider,
  KeyValueCollection,
  SimpleKeyValueCollection,
  ArrayFsList,
  FSDataType,
  FsError,
  ensureTyped,
  makeValue,
  typedNull,
  typeOf,
  valueOf,
  BaseFunction,
  CallType,
  FuncScriptParser
} = FuncScript;

const VALID_TYPED_TYPES = new Set(Object.values(FSDataType));

const JS_VALUE_SENTINEL = Symbol.for('funcdraw.js.value');

function normalizeName(name) {
  return String(name ?? '').trim().toLowerCase();
}

const DEFAULT_LANGUAGE = 'funcscript';

function normalizeLanguage(language) {
  if (typeof language !== 'string') {
    return DEFAULT_LANGUAGE;
  }
  const trimmed = language.trim().toLowerCase();
  if (!trimmed) {
    return DEFAULT_LANGUAGE;
  }
  if (trimmed === 'js') {
    return 'javascript';
  }
  if (trimmed === 'funcscript' || trimmed === 'fx') {
    return DEFAULT_LANGUAGE;
  }
  return trimmed;
}

function pathKey(segments) {
  if (!Array.isArray(segments) || segments.length === 0) {
    return '';
  }
  return segments.map(normalizeName).join('/');
}

function safeListItems(resolver, path) {
  if (!resolver || typeof resolver.listItems !== 'function') {
    return [];
  }
  try {
    const result = resolver.listItems(Array.isArray(path) ? [...path] : []);
    return Array.isArray(result) ? result : [];
  } catch {
    return [];
  }
}

function safeGetExpression(resolver, path) {
  if (!resolver || typeof resolver.getExpression !== 'function') {
    return null;
  }
  try {
    const result = resolver.getExpression(Array.isArray(path) ? [...path] : []);
    if (typeof result === 'string') {
      return result;
    }
    return result && typeof result.toString === 'function' ? result.toString() : null;
  } catch {
    return null;
  }
}

function previewValue(value) {
  if (value === null || value === undefined) {
    return value;
  }
  if (typeof value === 'string') {
    return value.length > 80 ? `${value.slice(0, 77)}…` : value;
  }
  if (typeof value === 'number' || typeof value === 'boolean') {
    return value;
  }
  if (Array.isArray(value)) {
    return `[array length=${value.length}]`;
  }
  if (typeof value === 'object') {
    const keys = Object.keys(value);
    return `{object keys=${keys.slice(0, 5).join(',')}${keys.length > 5 ? ',…' : ''}}`;
  }
  try {
    return JSON.stringify(value);
  } catch {
    return '[unserializable value]';
  }
}

const MAX_CONTEXT_WIDTH = 96;

function buildLineIndex(source) {
  const entries = [];
  let start = 0;
  let line = 1;
  const length = typeof source === 'string' ? source.length : 0;
  for (let i = 0; i < length; i += 1) {
    const ch = source[i];
    if (ch === '\n' || ch === '\r') {
      const end = i;
      entries.push({ line, start, end, text: source.slice(start, end) });
      if (ch === '\r' && source[i + 1] === '\n') {
        i += 1;
      }
      start = i + 1;
      line += 1;
    }
  }
  entries.push({ line, start, end: length, text: source.slice(start, length) });
  return entries;
}

function clampIndex(entries, targetIndex) {
  if (!Number.isFinite(targetIndex)) {
    return 0;
  }
  if (!Array.isArray(entries) || entries.length === 0) {
    return Math.max(0, targetIndex);
  }
  const lastEntry = entries[entries.length - 1];
  const maxIndex = Math.max(lastEntry.end - 1, lastEntry.start);
  if (maxIndex < 0) {
    return 0;
  }
  if (targetIndex < 0) {
    return 0;
  }
  if (targetIndex > maxIndex) {
    return maxIndex;
  }
  return targetIndex;
}

function findLineEntry(entries, targetIndex) {
  if (!Array.isArray(entries) || entries.length === 0) {
    return { line: 1, start: 0, text: '' };
  }
  const index = clampIndex(entries, targetIndex);
  for (const entry of entries) {
    if (index >= entry.start && index < entry.end) {
      return entry;
    }
  }
  return entries[entries.length - 1];
}

function buildContextLine(lineText, column, length) {
  if (typeof lineText !== 'string') {
    return null;
  }
  const safeColumn = Number.isFinite(column) && column > 0 ? column : 1;
  const safeLength = Number.isFinite(length) && length > 0 ? length : 1;
  const maxStart = Math.max(0, lineText.length - MAX_CONTEXT_WIDTH);
  const idealStart = Math.max(0, safeColumn - 1 - Math.floor(MAX_CONTEXT_WIDTH / 2));
  const start = Math.min(idealStart, maxStart);
  const end = Math.min(lineText.length, start + MAX_CONTEXT_WIDTH);
  const prefix = start > 0 ? '...' : '';
  const suffix = end < lineText.length ? '...' : '';
  const visible = prefix + lineText.slice(start, end) + suffix;
  const caretColumn = safeColumn - start + prefix.length;
  const availableSpan = Math.max(1, visible.length - Math.max(0, caretColumn - 1));
  const pointerSpan = Math.max(1, Math.min(safeLength, availableSpan));
  const pointer = `${' '.repeat(Math.max(0, caretColumn - 1))}${'^'}${pointerSpan > 1 ? '~'.repeat(pointerSpan - 1) : ''}`;
  return {
    lineText: visible,
    pointerText: pointer
  };
}

function describeSourceLocation(source, index, length, entries) {
  if (typeof source !== 'string' || !source) {
    return { location: null, context: null };
  }
  const lineEntries = Array.isArray(entries) && entries.length > 0 ? entries : buildLineIndex(source);
  const entry = findLineEntry(lineEntries, index);
  const normalizedIndex = clampIndex(lineEntries, index);
  const column = Math.max(1, normalizedIndex - entry.start + 1);
  const location = {
    index: normalizedIndex,
    line: entry.line,
    column,
    length: Number.isFinite(length) && length > 0 ? length : 1
  };
  const context = buildContextLine(entry.text, column, location.length);
  return { location, context };
}

function createParserErrorDetail(source, syntaxError, lineEntries) {
  if (!syntaxError || typeof syntaxError.Message !== 'string') {
    return null;
  }
  const loc = typeof syntaxError.Loc === 'number' ? syntaxError.Loc : 0;
  const len = typeof syntaxError.Length === 'number' ? syntaxError.Length : 1;
  const { location, context } = describeSourceLocation(source, loc, len, lineEntries);
  return {
    kind: 'funcscript-parser',
    message: syntaxError.Message || 'Syntax error',
    location,
    context,
    stack: null
  };
}

function collectParserErrorDetails(provider, source) {
  if (!source || !FuncScriptParser || typeof FuncScriptParser.parse !== 'function') {
    return null;
  }
  const errors = [];
  try {
    FuncScriptParser.parse(provider, source, errors);
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    return [
      {
        kind: 'funcscript-parser',
        message,
        location: null,
        context: null,
        stack: err instanceof Error && typeof err.stack === 'string' ? err.stack : null
      }
    ];
  }
  if (errors.length === 0) {
    return null;
  }
  const lineEntries = buildLineIndex(source);
  const details = [];
  for (const syntaxError of errors) {
    const detail = createParserErrorDetail(source, syntaxError, lineEntries);
    if (detail) {
      details.push(detail);
    }
  }
  return details.length > 0 ? details : null;
}

function createGenericErrorDetail(err, kind) {
  const message = err instanceof Error ? err.message : String(err);
  return {
    kind,
    message,
    location: null,
    context: null,
    stack: err instanceof Error && typeof err.stack === 'string' ? err.stack : null
  };
}

function toPlainValue(value) {
  if (value === null || value === undefined) {
    return null;
  }
  let typed;
  try {
    typed = ensureTyped(value);
  } catch (err) {
    return err instanceof Error ? err.message : String(err);
  }
  const dataType = typeOf(typed);
  const raw = valueOf(typed);
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
    case FSDataType.ByteArray: {
      if (raw instanceof Uint8Array) {
        if (typeof Buffer !== 'undefined' && typeof Buffer.from === 'function') {
          return Buffer.from(raw).toString('base64');
        }
        return Array.from(raw);
      }
      if (typeof Buffer !== 'undefined' && Buffer.isBuffer && Buffer.isBuffer(raw)) {
        return raw.toString('base64');
      }
      return raw;
    }
    case FSDataType.List: {
      if (raw && typeof raw.__jsValue !== 'undefined') {
        return raw.__jsValue;
      }
      if (!raw || typeof raw[Symbol.iterator] !== 'function') {
        return [];
      }
      const entries = [];
      for (const entry of raw) {
        entries.push(toPlainValue(entry));
      }
      return entries;
    }
    case FSDataType.KeyValueCollection: {
      if (raw && typeof raw.__jsValue !== 'undefined') {
        return raw.__jsValue;
      }
      if (!raw || typeof raw.getAll !== 'function') {
        return raw;
      }
      const result = {};
      for (const [key, entry] of raw.getAll()) {
        result[key] = toPlainValue(entry);
      }
      return result;
    }
    case FSDataType.Error: {
      const err = raw || {};
      const data = err.errorData;
      let converted = data;
      if (Array.isArray(data) && data.length === 2 && typeof data[0] === 'number') {
        try {
          converted = toPlainValue(data);
        } catch {
          converted = null;
        }
      }
      return {
        errorType: err.errorType || 'Error',
        errorMessage: err.errorMessage || '',
        errorData: converted ?? null
      };
    }
    default:
      return raw;
  }
}

function isFuncScriptTypedValue(value) {
  return Array.isArray(value) && value.length === 2 && typeof value[0] === 'number' && VALID_TYPED_TYPES.has(value[0]);
}

function convertJsValueToTyped(value, seen = new WeakSet()) {
  if (isFuncScriptTypedValue(value)) {
    return ensureTyped(value);
  }
  if (value === null || value === undefined) {
    return typedNull();
  }
  const valueType = typeof value;
  if (valueType === 'boolean') {
    return makeValue(FSDataType.Boolean, value);
  }
  if (valueType === 'number') {
    if (!Number.isFinite(value)) {
      return makeValue(FSDataType.Float, value);
    }
    if (Number.isInteger(value)) {
      return makeValue(FSDataType.Integer, value);
    }
    return makeValue(FSDataType.Float, value);
  }
  if (valueType === 'bigint') {
    return makeValue(FSDataType.BigInteger, value);
  }
  if (value instanceof Date) {
    return makeValue(FSDataType.DateTime, value);
  }
  if (valueType === 'string') {
    return makeValue(FSDataType.String, value);
  }
  if (valueType === 'function') {
    const collection = new SimpleKeyValueCollection(null, []);
    Object.defineProperty(collection, '__jsValue', {
      value,
      enumerable: false,
      configurable: false,
      writable: false
    });
    return makeValue(FSDataType.KeyValueCollection, collection);
  }
  if (value instanceof Uint8Array) {
    return makeValue(FSDataType.ByteArray, value);
  }
  if (typeof Buffer !== 'undefined' && Buffer.isBuffer && Buffer.isBuffer(value)) {
    return makeValue(FSDataType.ByteArray, Uint8Array.from(value));
  }
  if (Array.isArray(value)) {
    if (seen.has(value)) {
      throw new Error('Cannot convert cyclic array to FuncScript value.');
    }
    seen.add(value);
    const entries = value.map((entry) => convertJsValueToTyped(entry, seen));
    seen.delete(value);
    const fsList = new ArrayFsList(entries);
    Object.defineProperty(fsList, '__jsValue', {
      value,
      enumerable: false,
      configurable: false,
      writable: false
    });
    return makeValue(FSDataType.List, fsList);
  }
  if (value instanceof Set) {
    if (seen.has(value)) {
      throw new Error('Cannot convert cyclic set to FuncScript value.');
    }
    seen.add(value);
    const entries = [];
    for (const entry of value) {
      entries.push(convertJsValueToTyped(entry, seen));
    }
    seen.delete(value);
    const fsList = new ArrayFsList(entries);
    Object.defineProperty(fsList, '__jsValue', {
      value,
      enumerable: false,
      configurable: false,
      writable: false
    });
    return makeValue(FSDataType.List, fsList);
  }
  if (value instanceof Map) {
    if (seen.has(value)) {
      throw new Error('Cannot convert cyclic map to FuncScript value.');
    }
    seen.add(value);
    const kvEntries = [];
    for (const [key, entry] of value.entries()) {
      kvEntries.push([String(key), convertJsValueToTyped(entry, seen)]);
    }
    seen.delete(value);
    const collection = new SimpleKeyValueCollection(null, kvEntries);
    Object.defineProperty(collection, '__jsValue', {
      value,
      enumerable: false,
      configurable: false,
      writable: false
    });
    return makeValue(FSDataType.KeyValueCollection, collection);
  }
  if (valueType === 'object') {
    if (seen.has(value)) {
      throw new Error('Cannot convert cyclic object to FuncScript value.');
    }
    seen.add(value);
    const entries = [];
    for (const [key, entry] of Object.entries(value)) {
      entries.push([key, convertJsValueToTyped(entry, seen)]);
    }
    seen.delete(value);
    const collection = new SimpleKeyValueCollection(null, entries);
    Object.defineProperty(collection, '__jsValue', {
      value,
      enumerable: false,
      configurable: false,
      writable: false
    });
    return makeValue(FSDataType.KeyValueCollection, collection);
  }
  return ensureTyped(value);
}

const clampNumber = (value, min, max) => {
  if (!Number.isFinite(value)) {
    return min;
  }
  if (value < min) {
    return min;
  }
  if (value > max) {
    return max;
  }
  return value;
};

const makeFuncDrawError = (symbol, type, message) =>
  makeValue(FSDataType.Error, new FsError(type ?? FsError.ERROR_DEFAULT, `${symbol}: ${message}`));

const requireNumericParameter = (symbol, parameter, name) => {
  const typed = ensureTyped(parameter);
  const t = typeOf(typed);
  if (t === FSDataType.Integer || t === FSDataType.Float) {
    return { ok: true, value: Number(valueOf(typed)) };
  }
  if (t === FSDataType.BigInteger) {
    const raw = valueOf(typed);
    return { ok: true, value: Number(raw) };
  }
  return {
    ok: false,
    error: makeFuncDrawError(symbol, FsError.ERROR_TYPE_MISMATCH, `${name} must be a number.`)
  };
};

const requireStringParameter = (symbol, parameter, name) => {
  const typed = ensureTyped(parameter);
  const t = typeOf(typed);
  if (t === FSDataType.String) {
    return { ok: true, value: valueOf(typed) };
  }
  if (t === FSDataType.Null) {
    return { ok: true, value: '' };
  }
  return {
    ok: false,
    error: makeFuncDrawError(symbol, FsError.ERROR_TYPE_MISMATCH, `${name} must be a string.`)
  };
};

const normalizePointValue = (value) => {
  if (Array.isArray(value) && value.length >= 2) {
    const x = Number(value[0]);
    const y = Number(value[1]);
    if (Number.isFinite(x) && Number.isFinite(y)) {
      return [x, y];
    }
    return null;
  }
  if (value && typeof value === 'object') {
    const xCandidate = value.x ?? value.X;
    const yCandidate = value.y ?? value.Y;
    const x = Number(xCandidate);
    const y = Number(yCandidate);
    if (Number.isFinite(x) && Number.isFinite(y)) {
      return [x, y];
    }
  }
  return null;
};

const requirePointParameter = (symbol, parameter, name) => {
  const plain = toPlainValue(parameter);
  const point = normalizePointValue(plain);
  if (!point) {
    return {
      ok: false,
      error: makeFuncDrawError(symbol, FsError.ERROR_TYPE_MISMATCH, `${name} must be a point [x, y].`)
    };
  }
  return { ok: true, value: point };
};

const transformPointIfValid = (value, transformPoint) => {
  const point = normalizePointValue(value);
  if (!point) {
    return null;
  }
  const next = transformPoint(point);
  if (!Array.isArray(next) || next.length < 2) {
    return point;
  }
  const x = Number(next[0]);
  const y = Number(next[1]);
  if (!Number.isFinite(x) || !Number.isFinite(y)) {
    return point;
  }
  return [x, y];
};

const transformPrimitiveData = (type, data, transformPoint) => {
  if (!data || typeof data !== 'object') {
    return data;
  }
  const clone = { ...data };
  const lower = String(type || '').toLowerCase();
  const applyPoint = (key) => {
    if (!Object.prototype.hasOwnProperty.call(data, key)) {
      return;
    }
    const transformed = transformPointIfValid(data[key], transformPoint);
    if (transformed) {
      clone[key] = transformed;
    }
  };

  switch (lower) {
    case 'line':
      applyPoint('from');
      applyPoint('to');
      break;
    case 'rect':
    case 'text':
      applyPoint('position');
      break;
    case 'circle':
      applyPoint('center');
      break;
    case 'polygon':
      if (Array.isArray(data.points)) {
        clone.points = data.points.map((point) => transformPointIfValid(point, transformPoint) ?? point);
      }
      break;
    default:
      applyPoint('position');
      applyPoint('center');
      break;
  }

  return clone;
};

function transformPrimitiveNode(node, transformPoint) {
  const result = { ...node };
  if (node.data && typeof node.data === 'object') {
    result.data = transformPrimitiveData(node.type, node.data, transformPoint);
  }
  if (Object.prototype.hasOwnProperty.call(node, 'children')) {
    result.children = transformGraphicsCollection(node.children, transformPoint);
  }
  if (Object.prototype.hasOwnProperty.call(node, 'layers')) {
    result.layers = transformGraphicsCollection(node.layers, transformPoint);
  }
  return result;
}

function transformGraphicsCollection(node, transformPoint) {
  if (Array.isArray(node)) {
    return node.map((entry) => transformGraphicsCollection(entry, transformPoint));
  }
  if (node && typeof node === 'object') {
    if (typeof node.type === 'string') {
      return transformPrimitiveNode(node, transformPoint);
    }
    const clone = {};
    for (const [key, value] of Object.entries(node)) {
      clone[key] = transformGraphicsCollection(value, transformPoint);
    }
    return clone;
  }
  return node;
}

class MeasureTextFunction extends BaseFunction {
  constructor() {
    super();
    this.symbol = 'fd.measuretext';
    this.callType = CallType.Prefix;
  }

  get maxParameters() {
    return 3;
  }

  evaluate(provider, parameters) {
    if (parameters.count < 1 || parameters.count > 3) {
      return makeFuncDrawError(
        this.symbol,
        FsError.ERROR_PARAMETER_COUNT_MISMATCH,
        `expected between 1 and 3 parameters, received ${parameters.count}`
      );
    }

    const textResult = requireStringParameter(this.symbol, parameters.getParameter(provider, 0), 'text');
    if (!textResult.ok) {
      return textResult.error;
    }

    let fontSize = 12;
    if (parameters.count >= 2) {
      const sizeResult = requireNumericParameter(this.symbol, parameters.getParameter(provider, 1), 'fontSize');
      if (!sizeResult.ok) {
        return sizeResult.error;
      }
      fontSize = sizeResult.value;
    }
    if (!Number.isFinite(fontSize) || fontSize <= 0) {
      fontSize = 12;
    }

    let optionsRecord = {};
    if (parameters.count === 3) {
      const rawOptions = toPlainValue(parameters.getParameter(provider, 2));
      if (rawOptions === null || rawOptions === undefined) {
        optionsRecord = {};
      } else if (rawOptions && typeof rawOptions === 'object' && !Array.isArray(rawOptions)) {
        optionsRecord = rawOptions;
      } else {
        return makeFuncDrawError(this.symbol, FsError.ERROR_TYPE_MISMATCH, 'options must be a record.');
      }
    }

    const widthFactor = clampNumber(
      typeof optionsRecord.widthFactor === 'number' ? optionsRecord.widthFactor : 0.58,
      0.2,
      5
    );
    const letterSpacing = Number.isFinite(optionsRecord.letterSpacing) ? optionsRecord.letterSpacing : 0;
    const lineHeightFactor = clampNumber(
      typeof optionsRecord.lineHeight === 'number' && optionsRecord.lineHeight > 0
        ? optionsRecord.lineHeight
        : 1.2,
      0.5,
      4
    );
    const ascentRatio = clampNumber(
      typeof optionsRecord.ascentRatio === 'number' ? optionsRecord.ascentRatio : 0.78,
      0.1,
      0.95
    );

    const rawText = textResult.value;
    const lines = rawText.length > 0 ? rawText.split(/\r?\n/) : [''];
    const baseCharWidth = fontSize * widthFactor;
    const classifyChar = (char) => {
      if (!char) {
        return 1;
      }
      if (char === ' ') {
        return 0.55;
      }
      if ('.,:;!`\"\'|'.indexOf(char) >= 0) {
        return 0.45;
      }
      if ('-_'.indexOf(char) >= 0) {
        return 0.65;
      }
      if (char === '/' || char === '\\') {
        return 0.7;
      }
      if ('{}[]()<>'.indexOf(char) >= 0) {
        return 0.7;
      }
      const code = char.charCodeAt(0);
      if (code >= 48 && code <= 57) {
        return 0.75;
      }
      if (code >= 65 && code <= 90) {
        if (char === 'M' || char === 'W') {
          return 1.05;
        }
        return 0.95;
      }
      if (code >= 97 && code <= 122) {
        if ('ilftjr'.indexOf(char) >= 0) {
          return 0.7;
        }
        return 0.85;
      }
      return 0.85;
    };

    let measuredWidth = 0;
    let totalWidthSum = 0;
    let totalChars = 0;
    for (const line of lines) {
      let lineWidth = 0;
      for (let i = 0; i < line.length; i += 1) {
        const char = line[i];
        const weight = classifyChar(char);
        lineWidth += baseCharWidth * weight;
      }
      if (line.length > 1 && letterSpacing !== 0) {
        lineWidth += (line.length - 1) * letterSpacing;
      }
      if (lineWidth > measuredWidth) {
        measuredWidth = lineWidth;
      }
      totalWidthSum += lineWidth;
      totalChars += line.length;
    }

    const avgCharWidthResult = totalChars > 0 ? totalWidthSum / totalChars : baseCharWidth;
    const perLineHeight = fontSize * lineHeightFactor;
    const totalHeight = perLineHeight * lines.length;
    const ascent = fontSize * ascentRatio;
    const descent = perLineHeight - ascent;

    const metrics = {
      text: rawText,
      fontSize,
      width: measuredWidth,
      lineHeight: perLineHeight,
      height: totalHeight,
      lines: lines.length,
      ascent,
      descent,
      baseline: ascent,
      avgCharWidth: avgCharWidthResult,
      letterSpacing,
      widthFactor,
      lineHeightFactor,
      ascentRatio
    };

    return convertJsValueToTyped(metrics);
  }
}

class TranslateFunction extends BaseFunction {
  constructor() {
    super();
    this.symbol = 'fd.translate';
    this.callType = CallType.Prefix;
  }

  get maxParameters() {
    return 3;
  }

  evaluate(provider, parameters) {
    if (parameters.count !== 3) {
      return makeFuncDrawError(
        this.symbol,
        FsError.ERROR_PARAMETER_COUNT_MISMATCH,
        `expected 3 parameters (deltaX, deltaY, graphics) but received ${parameters.count}`
      );
    }

    const deltaXResult = requireNumericParameter(this.symbol, parameters.getParameter(provider, 0), 'deltaX');
    if (!deltaXResult.ok) {
      return deltaXResult.error;
    }
    const deltaYResult = requireNumericParameter(this.symbol, parameters.getParameter(provider, 1), 'deltaY');
    if (!deltaYResult.ok) {
      return deltaYResult.error;
    }

    const target = toPlainValue(parameters.getParameter(provider, 2));
    if (target === null || target === undefined) {
      return typedNull();
    }
    const dx = deltaXResult.value;
    const dy = deltaYResult.value;
    const transformed = transformGraphicsCollection(target, (point) => [point[0] + dx, point[1] + dy]);
    return convertJsValueToTyped(transformed);
  }
}

class RotateFunction extends BaseFunction {
  constructor() {
    super();
    this.symbol = 'fd.rotate';
    this.callType = CallType.Prefix;
  }

  get maxParameters() {
    return 3;
  }

  evaluate(provider, parameters) {
    if (parameters.count !== 3) {
      return makeFuncDrawError(
        this.symbol,
        FsError.ERROR_PARAMETER_COUNT_MISMATCH,
        `expected 3 parameters (origin, radians, graphics) but received ${parameters.count}`
      );
    }

    const originResult = requirePointParameter(this.symbol, parameters.getParameter(provider, 0), 'origin');
    if (!originResult.ok) {
      return originResult.error;
    }
    const radiansResult = requireNumericParameter(this.symbol, parameters.getParameter(provider, 1), 'radians');
    if (!radiansResult.ok) {
      return radiansResult.error;
    }

    const target = toPlainValue(parameters.getParameter(provider, 2));
    if (target === null || target === undefined) {
      return typedNull();
    }

    const [ox, oy] = originResult.value;
    const angle = radiansResult.value;
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    const transformed = transformGraphicsCollection(target, (point) => {
      const translatedX = point[0] - ox;
      const translatedY = point[1] - oy;
      return [ox + translatedX * cos - translatedY * sin, oy + translatedX * sin + translatedY * cos];
    });
    return convertJsValueToTyped(transformed);
  }
}

const createFdCollectionValue = () => {
  const measureText = ensureTyped(new MeasureTextFunction());
  const translate = ensureTyped(new TranslateFunction());
  const rotate = ensureTyped(new RotateFunction());
  const entries = [
    ['measuretext', measureText],
    ['translate', translate],
    ['rotate', rotate]
  ];
  const collection = new SimpleKeyValueCollection(null, entries);
  return ensureTyped(collection);
};

const FD_COLLECTION_VALUE = createFdCollectionValue();

class FolderNode {
  constructor(name, path, createdAt, parentKey) {
    this.name = name;
    this.path = path;
    this.createdAt = createdAt;
    this.parentKey = parentKey;
    this.key = pathKey(path);
  }
}

class ExpressionNode {
  constructor(name, path, createdAt, parentKey, language) {
    this.name = name;
    this.path = path;
    this.createdAt = createdAt;
    this.parentKey = parentKey;
    this.key = pathKey(path);
    this.language = normalizeLanguage(language);
  }
}

class CollectionGraph {
  constructor(resolver) {
    this.resolver = resolver;
    this.root = new FolderNode(null, [], 0, null);
    this.folderNodes = new Map([[this.root.key, this.root]]);
    this.childFolderMaps = new Map();
    this.expressionMaps = new Map();
    this.expressionNodes = new Map();
    this.walk();
  }

  ensureFolderMap(key) {
    if (!this.childFolderMaps.has(key)) {
      this.childFolderMaps.set(key, new Map());
    }
    return this.childFolderMaps.get(key);
  }

  ensureExpressionMap(key) {
    if (!this.expressionMaps.has(key)) {
      this.expressionMaps.set(key, new Map());
    }
    return this.expressionMaps.get(key);
  }

  walk() {
    const queue = [this.root];
    const visited = new Set();
    while (queue.length > 0) {
      const folder = queue.shift();
      if (!folder || visited.has(folder.key)) {
        continue;
      }
      visited.add(folder.key);
      const items = safeListItems(this.resolver, folder.path);
      const childMap = this.ensureFolderMap(folder.key);
      const exprMap = this.ensureExpressionMap(folder.key);
      let index = 0;
      for (const item of items) {
        if (!item || typeof item.name !== 'string') {
          index += 1;
          continue;
        }
        const createdAt = typeof item.createdAt === 'number' ? item.createdAt : index;
        const lower = normalizeName(item.name);
        if (item.kind === 'folder') {
          if (childMap.has(lower)) {
            index += 1;
            continue;
          }
          const path = folder.path.concat([item.name]);
          const child = new FolderNode(item.name, path, createdAt, folder.key);
          childMap.set(lower, child);
          if (!this.folderNodes.has(child.key)) {
            this.folderNodes.set(child.key, child);
            queue.push(child);
          }
        } else if (item.kind === 'expression') {
          if (exprMap.has(lower)) {
            index += 1;
            continue;
          }
          const path = folder.path.concat([item.name]);
          const node = new ExpressionNode(item.name, path, createdAt, folder.key, item.language);
          exprMap.set(lower, node);
          if (!this.expressionNodes.has(node.key)) {
            this.expressionNodes.set(node.key, node);
          }
        }
        index += 1;
      }
    }
  }

  getFolderNodeByPath(path) {
    const key = pathKey(path);
    return this.folderNodes.get(key) ?? null;
  }

  getFolderNodeByKey(key) {
    return this.folderNodes.get(key) ?? null;
  }

  getExpressionNodeByPath(path) {
    const key = pathKey(path);
    return this.expressionNodes.get(key) ?? null;
  }

  getChildFolder(folderKey, lowerName) {
    const map = this.childFolderMaps.get(folderKey);
    if (!map) {
      return null;
    }
    return map.get(lowerName) ?? null;
  }

  getChildFolders(folderKey) {
    const map = this.childFolderMaps.get(folderKey);
    if (!map) {
      return [];
    }
    return Array.from(map.values());
  }

  getExpressionInFolder(folderKey, lowerName) {
    const map = this.expressionMaps.get(folderKey);
    if (!map) {
      return null;
    }
    return map.get(lowerName) ?? null;
  }

  getExpressions(folderKey) {
    const map = this.expressionMaps.get(folderKey);
    if (!map) {
      return [];
    }
    return Array.from(map.values());
  }
}

function getBuiltinGlobal(prop) {
  if (typeof prop !== 'string') {
    return undefined;
  }
  switch (prop) {
    case 'Math':
      return globalThis.Math;
    default:
      return undefined;
  }
}

function tryGetJsBinding(provider, name) {
  if (!provider || typeof provider.getJsValue !== 'function' || typeof name !== 'string') {
    return { hasValue: false, value: undefined };
  }
  try {
    const result = provider.getJsValue(name);
    if (result && result.marker === JS_VALUE_SENTINEL) {
      return { hasValue: true, value: result.value };
    }
  } catch {
    return { hasValue: false, value: undefined };
  }
  return { hasValue: false, value: undefined };
}

function createJavaScriptScope(provider) {
  const cache = new Map();
  return new Proxy(Object.create(null), {
    has(_target, property) {
      if (property === Symbol.unscopables) {
        return false;
      }
      if (getBuiltinGlobal(property) !== undefined) {
        return true;
      }
      if (typeof property !== 'string') {
        return false;
      }
      if (typeof provider.isDefined === 'function') {
        try {
          return provider.isDefined(property);
        } catch {
          return false;
        }
      }
      return false;
    },
    get(_target, property) {
      if (property === Symbol.unscopables) {
        return undefined;
      }
      if (typeof property !== 'string') {
        return undefined;
      }
      const builtin = getBuiltinGlobal(property);
      if (builtin !== undefined) {
        return builtin;
      }
      const jsBinding = tryGetJsBinding(provider, property);
      if (jsBinding.hasValue) {
        cache.set(property, jsBinding.value);
        return jsBinding.value;
      }
      if (cache.has(property)) {
        return cache.get(property);
      }
      if (typeof provider.isDefined === 'function') {
        let defined = false;
        try {
          defined = provider.isDefined(property);
        } catch {
          defined = false;
        }
        if (!defined) {
          return undefined;
        }
      }
      let typedValue = null;
      if (typeof provider.get === 'function') {
        typedValue = provider.get(property);
      }
      const plainValue = toPlainValue(typedValue);
      cache.set(property, plainValue);
      return plainValue;
    },
    set() {
      throw new Error('Cannot assign to FuncDraw bindings inside a JavaScript expression.');
    },
    getOwnPropertyDescriptor() {
      return undefined;
    },
    ownKeys() {
      return [];
    }
  });
}

function createJavaScriptExecutor(source) {
  const body = [
    'return (function() {',
    '  with (scope) {',
    '    return (function() {',
    source,
    '    }).call(scope);',
    '  }',
    '}).call(scope);'
  ].join('\n');
  return new Function('scope', body);
}

function runJavaScriptExecutor(executor, provider) {
  const scope = createJavaScriptScope(provider);
  return executor(scope);
}

class FolderProvider extends KeyValueCollection {
  constructor(manager, folderNode, parentProvider) {
    super(parentProvider ?? null);
    this.manager = manager;
    this.folderNode = folderNode;
  }

  findExpression(name) {
    return this.manager.graph.getExpressionInFolder(this.folderNode.key, normalizeName(name));
  }

  getJsValue(name) {
    const expression = this.findExpression(name);
    if (expression) {
      this.manager.evaluateNode(expression, this);
      if ((expression.language || '').toLowerCase() === 'javascript') {
        return { marker: JS_VALUE_SENTINEL, value: this.manager.getJavaScriptValue(expression.key) };
      }
      return undefined;
    }
    const childFolder = this.findChildFolder(name);
    if (childFolder) {
      const folderProvider = this.manager.getFolderProvider(childFolder.key);
      if (folderProvider && typeof folderProvider.getJsValue === 'function') {
        return folderProvider.getJsValue('return');
      }
    }
    const parent = this.getParentProvider();
    if (parent && typeof parent.getJsValue === 'function') {
      return parent.getJsValue(name);
    }
    return undefined;
  }

  findChildFolder(name) {
    return this.manager.graph.getChildFolder(this.folderNode.key, normalizeName(name));
  }

  getParentProvider() {
    return (this.parent && typeof this.parent === 'object') ? this.parent : null;
  }

  get(name) {
    const expression = this.findExpression(name);
    if (expression) {
      const evaluation = this.manager.evaluateNode(expression, this);
      return evaluation.typed ?? typedNull();
    }
    const childFolder = this.findChildFolder(name);
    if (childFolder) {
      return this.manager.getFolderValue(childFolder.path);
    }
    const parent = this.getParentProvider();
    return parent ? parent.get(name) : null;
  }

  isDefined(name) {
    if (this.findExpression(name)) {
      return true;
    }
    if (this.findChildFolder(name)) {
      return true;
    }
    const parent = this.getParentProvider();
    return parent ? parent.isDefined(name) : false;
  }

  getAll() {
    const entries = [];
    const expressions = this.manager.graph.getExpressions(this.folderNode.key);
    for (const expression of expressions) {
      if (normalizeName(expression.name) === 'return') {
        continue;
      }
      const evaluation = this.manager.evaluateNode(expression, this);
      entries.push([expression.name, evaluation.typed ?? typedNull()]);
    }
    const children = this.manager.graph.getChildFolders(this.folderNode.key);
    for (const child of children) {
      const typedValue = this.manager.getFolderValue(child.path);
      entries.push([child.name, typedValue]);
    }
    return entries;
  }
}

class FuncDrawEnvironmentProvider extends Engine.FsDataProvider {
  constructor(manager) {
    super(manager.baseProvider);
    this.manager = manager;
    this.namedValues = new Map();
    this.setNamedValue('fd', FD_COLLECTION_VALUE);
  }

  getParentProvider() {
    return this.parent && typeof this.parent === 'object' ? this.parent : null;
  }

  setNamedValue(name, value) {
    const lower = normalizeName(name);
    if (value) {
      this.namedValues.set(lower, ensureTyped(value));
    } else {
      this.namedValues.delete(lower);
    }
  }

  get(name) {
    const lower = normalizeName(name);
    if (lower === this.manager.timeVariableName) {
      return this.manager.timeValue;
    }
    if (this.namedValues.has(lower)) {
      return this.namedValues.get(lower) ?? null;
    }
    const expression = this.manager.graph.getExpressionInFolder('', lower);
    if (expression) {
      const evaluation = this.manager.evaluateNode(expression, this);
      return evaluation.typed ?? typedNull();
    }
    const folder = this.manager.graph.getChildFolder('', lower);
    if (folder) {
      return this.manager.getFolderValue(folder.path);
    }
    return super.get(name);
  }

  getJsValue(name) {
    const lower = normalizeName(name);
    if (lower === this.manager.timeVariableName) {
      return undefined;
    }
    const expression = this.manager.graph.getExpressionInFolder('', lower);
    if (expression) {
      this.manager.evaluateNode(expression, this);
      if ((expression.language || '').toLowerCase() === 'javascript') {
        return { marker: JS_VALUE_SENTINEL, value: this.manager.getJavaScriptValue(expression.key) };
      }
      return undefined;
    }
    const folder = this.manager.graph.getChildFolder('', lower);
    if (folder) {
      const provider = this.manager.getFolderProvider(folder.key);
      if (provider && typeof provider.getJsValue === 'function') {
        return provider.getJsValue('return');
      }
    }
    const parent = this.getParentProvider();
    if (parent && typeof parent.getJsValue === 'function') {
      return parent.getJsValue(name);
    }
    return undefined;
  }

  isDefined(name) {
    const lower = normalizeName(name);
    if (lower === this.manager.timeVariableName) {
      return true;
    }
    if (this.namedValues.has(lower)) {
      return true;
    }
    if (this.manager.graph.getExpressionInFolder('', lower)) {
      return true;
    }
    if (this.manager.graph.getChildFolder('', lower)) {
      return true;
    }
    const parent = this.getParentProvider();
    return parent ? parent.isDefined(name) : super.isDefined(name);
  }
}

class FuncDrawEvaluationManager {
  constructor(resolver, options = {}, explicitTime) {
    this.resolver = resolver;
    this.graph = new CollectionGraph(resolver);
    this.baseProvider = options.baseProvider || new DefaultFsDataProvider();
    const configuredTimeName = typeof options.timeName === 'string' ? options.timeName : '';
    const normalizedTimeName = normalizeName(configuredTimeName) || 't';
    this.timeVariableName = normalizedTimeName;
    const hasExplicitTime = typeof explicitTime === 'number' && Number.isFinite(explicitTime);
    const resolvedTimeSeconds = hasExplicitTime ? explicitTime : Date.now() / 1000;
    this.timeValue = ensureTyped(resolvedTimeSeconds);
    this.evaluations = new Map();
    this.evaluating = new Set();
    this.folderProviders = new Map();
    this.environmentProvider = null;
    this.javascriptValues = new Map();
    this.javascriptExecutors = new Map();
  }

  getEnvironmentProvider() {
    if (!this.environmentProvider) {
      this.environmentProvider = new FuncDrawEnvironmentProvider(this);
    }
    return this.environmentProvider;
  }

  getJavaScriptValue(nodeKey) {
    return this.javascriptValues.get(nodeKey);
  }

  getFolderProvider(folderKey) {
    if (this.folderProviders.has(folderKey)) {
      return this.folderProviders.get(folderKey);
    }
    const folderNode = this.graph.getFolderNodeByKey(folderKey);
    if (!folderNode) {
      throw new Error(`Unknown folder key: ${folderKey}`);
    }
    const parentProvider = folderNode.parentKey
      ? this.getFolderProvider(folderNode.parentKey)
      : this.getEnvironmentProvider();
    const provider = new FolderProvider(this, folderNode, parentProvider);
    this.folderProviders.set(folderKey, provider);
    return provider;
  }

  getFolderValue(path) {
    const folderNode = this.graph.getFolderNodeByPath(path);
    if (!folderNode) {
      return typedNull();
    }
    if (!folderNode.name) {
      return ensureTyped(this.getEnvironmentProvider());
    }
    const provider = this.getFolderProvider(folderNode.key);
    const returnExpression = this.graph.getExpressionInFolder(folderNode.key, 'return');
    if (returnExpression) {
      const evaluation = this.evaluateNode(returnExpression, provider);
      return evaluation.typed ?? typedNull();
    }
    const evalExpression = this.graph.getExpressionInFolder(folderNode.key, 'eval');
    if (evalExpression) {
      const evaluation = this.evaluateNode(evalExpression, provider);
      return evaluation.typed ?? typedNull();
    }
    return ensureTyped(provider);
  }

  getJavaScriptExecutor(key, source) {
    const cached = this.javascriptExecutors.get(key);
    if (cached && cached.source === source) {
      return cached.executor;
    }
    const executor = createJavaScriptExecutor(source);
    this.javascriptExecutors.set(key, { source, executor });
    return executor;
  }

  setTime(seconds) {
    const hasExplicitTime = typeof seconds === 'number' && Number.isFinite(seconds);
    const resolvedTime = hasExplicitTime ? seconds : Date.now() / 1000;
    this.timeValue = ensureTyped(resolvedTime);
    this.evaluations.clear();
    this.javascriptValues.clear();
  }

  evaluateNode(expressionNode, provider) {
    const key = expressionNode.key;
    if (this.evaluations.has(key)) {
      return this.evaluations.get(key);
    }
    if (this.evaluating.has(key)) {
      const message = 'Circular reference detected while evaluating expression.';
      const typedError = makeValue(FSDataType.Error, new FsError(FsError.ERROR_DEFAULT, message));
      const fallback = {
        value: null,
        typed: typedError,
        error: message,
        errorDetails: [
          {
            kind: 'funcdraw',
            message,
            location: null,
            context: null,
            stack: null
          }
        ]
      };
      this.evaluations.set(key, fallback);
      return fallback;
    }
    this.evaluating.add(key);
    const source = safeGetExpression(this.resolver, expressionNode.path) || '';
    const trimmed = source.trim();
    const language = expressionNode.language || DEFAULT_LANGUAGE;
    let evaluation;
    console.log('[FuncDraw] evaluateNode:start', {
      path: expressionNode.path,
      language,
      hasSource: Boolean(trimmed.length),
      sourcePreview: trimmed.slice(0, 80)
    });
    if (!trimmed) {
      this.javascriptValues.delete(key);
      evaluation = { value: null, typed: typedNull(), error: null, errorDetails: null };
    } else {
      try {
        let typed;
        let jsValue;
        if (language === 'javascript') {
          const normalizedJs = normalizeJavaScriptSource(trimmed);
          const executor = this.getJavaScriptExecutor(key, normalizedJs);
          jsValue = runJavaScriptExecutor(executor, provider);
          if (jsValue === undefined) {
            typed = typedNull();
          } else {
            typed = convertJsValueToTyped(jsValue);
          }
          this.javascriptValues.set(key, jsValue);
        } else {
          this.javascriptValues.delete(key);
          typed = ensureTyped(Engine.evaluate(trimmed, provider));
        }
        if (language === 'javascript') {
          evaluation = {
            value: jsValue === undefined ? null : jsValue,
            typed,
            error: null,
            errorDetails: null
          };
        } else {
          evaluation = {
            value: toPlainValue(typed),
            typed,
            error: null,
            errorDetails: null
          };
        }
        console.log('[FuncDraw] evaluateNode:success', {
          path: expressionNode.path,
          language,
          hasError: false,
          resultType: typeof evaluation.value,
          typedKind: typed?.type,
          plainPreview: previewValue(evaluation.value)
        });
      } catch (err) {
        this.javascriptValues.delete(key);
        const message = err instanceof Error ? err.message : String(err);
        let errorDetails = null;
        if (language === 'funcscript') {
          errorDetails = collectParserErrorDetails(provider, trimmed);
        }
        if (!errorDetails || errorDetails.length === 0) {
          const kind = language === 'javascript' ? 'javascript' : 'funcscript-runtime';
          errorDetails = [createGenericErrorDetail(err, kind)];
        }
        const typed = makeValue(FSDataType.Error, new FsError(FsError.ERROR_DEFAULT, message));
        evaluation = {
          value: null,
          typed,
          error: message,
          errorDetails
        };
        console.error('[FuncDraw] evaluateNode:error', {
          path: expressionNode.path,
          language,
          message,
          errorDetails
        });
      }
    }
    this.evaluating.delete(key);
    this.evaluations.set(key, evaluation);
    return evaluation;
  }

  evaluatePath(path) {
    console.log('[FuncDraw] evaluatePath:request', { path });
    const expression = this.graph.getExpressionNodeByPath(path);
    if (!expression) {
      console.warn('[FuncDraw] evaluatePath:missing-expression', { path });
      return null;
    }
    const provider = expression.parentKey
      ? this.getFolderProvider(expression.parentKey)
      : this.getEnvironmentProvider();
    const result = this.evaluateNode(expression, provider);
    console.log('[FuncDraw] evaluatePath:response', {
      path,
      hasResult: Boolean(result),
      hasError: Boolean(result?.error),
      typedKind: result?.typed?.type
    });
    return result;
  }

  listExpressions() {
    return Array.from(this.graph.expressionNodes.values()).map((node) => ({
      path: [...node.path],
      name: node.name
    }));
  }

  listFolders(path) {
    const folderNode = this.graph.getFolderNodeByPath(path);
    if (!folderNode) {
      return [];
    }
    return this.graph.getChildFolders(folderNode.key).map((child) => ({
      path: [...child.path],
      name: child.name
    }));
  }
}

function evaluate(resolver, timeOrOptions, maybeOptions) {
  if (!resolver || typeof resolver.listItems !== 'function' || typeof resolver.getExpression !== 'function') {
    throw new TypeError('ExpressionCollectionResolver must implement listItems and getExpression.');
  }
  let explicitTime;
  let options;
  if (typeof timeOrOptions === 'number' || typeof timeOrOptions === 'undefined') {
    explicitTime = timeOrOptions;
    options = maybeOptions;
  } else {
    options = timeOrOptions;
    if (options && typeof options.time === 'number') {
      explicitTime = options.time;
    }
  }

  const normalizedOptions = options && typeof options === 'object' ? { ...options } : {};
  if (Object.prototype.hasOwnProperty.call(normalizedOptions, 'time')) {
    delete normalizedOptions.time;
  }

  const manager = new FuncDrawEvaluationManager(resolver, normalizedOptions, explicitTime);
  return {
    environmentProvider: manager.getEnvironmentProvider(),
    evaluateExpression: (path) => manager.evaluatePath(path),
    getFolderValue: (path) => manager.getFolderValue(path),
    listExpressions: () => manager.listExpressions(),
    listFolders: (path) => manager.listFolders(path),
    setTime: (seconds) => manager.setTime(seconds)
  };
}

const FuncDraw = {
  evaluate
};

module.exports = {
  FuncDraw,
  evaluate,
  default: FuncDraw
};
function normalizeJavaScriptSource(source) {
  if (typeof source !== 'string') {
    return source;
  }
  const trimmed = source.trimStart();
  if (!/^export\s+default\b/.test(trimmed)) {
    return source;
  }
  const body = trimmed.replace(/^export\s+default\s*/, '').replace(/;\s*$/, '');
  return `return (${body});`;
}

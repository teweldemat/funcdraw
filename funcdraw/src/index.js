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
  valueOf
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
    return super.isDefined(name);
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
      const fallback = { value: null, typed: typedError, error: message };
      this.evaluations.set(key, fallback);
      return fallback;
    }
    this.evaluating.add(key);
    const source = safeGetExpression(this.resolver, expressionNode.path) || '';
    const trimmed = source.trim();
    const language = expressionNode.language || DEFAULT_LANGUAGE;
    let evaluation;
    if (!trimmed) {
      this.javascriptValues.delete(key);
      evaluation = { value: null, typed: typedNull(), error: null };
    } else {
      try {
        let typed;
        let jsValue;
        if (language === 'javascript') {
          const executor = this.getJavaScriptExecutor(key, trimmed);
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
            error: null
          };
        } else {
          evaluation = {
            value: toPlainValue(typed),
            typed,
            error: null
          };
        }
      } catch (err) {
        this.javascriptValues.delete(key);
        const message = err instanceof Error ? err.message : String(err);
        const typed = makeValue(FSDataType.Error, new FsError(FsError.ERROR_DEFAULT, message));
        evaluation = {
          value: null,
          typed,
          error: message
        };
      }
    }
    this.evaluating.delete(key);
    this.evaluations.set(key, evaluation);
    return evaluation;
  }

  evaluatePath(path) {
    const expression = this.graph.getExpressionNodeByPath(path);
    if (!expression) {
      return null;
    }
    const provider = expression.parentKey
      ? this.getFolderProvider(expression.parentKey)
      : this.getEnvironmentProvider();
    return this.evaluateNode(expression, provider);
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

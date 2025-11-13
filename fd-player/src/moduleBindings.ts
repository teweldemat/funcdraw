import {
  ArrayFsList,
  BaseFunction,
  CallType,
  DefaultFsDataProvider,
  Engine,
  FSDataType,
  SimpleKeyValueCollection,
  type FsDataProvider,
  type ParameterList,
  type TypedValue
} from '@tewelde/funcscript';

const JS_VALUE_SENTINEL = Symbol.for('funcdraw.js.value');
export const FUNC_DRAW_MODULE_SENTINEL = Symbol('fd-player.funcdraw.module');

export interface FuncdrawModuleValue {
  [FUNC_DRAW_MODULE_SENTINEL]: true;
  getTypedValue: () => TypedValue;
}

const isFuncdrawModuleValue = (value: unknown): value is FuncdrawModuleValue =>
  Boolean(value && typeof value === 'object' && FUNC_DRAW_MODULE_SENTINEL in (value as object));

export type ModuleImportContext = {
  folderPath?: string[];
};

export type ModuleImportHandler =
  | ((specifier: unknown, context?: ModuleImportContext | null) => unknown)
  | null
  | undefined;

const convertNumber = (value: number): TypedValue => {
  if (Number.isInteger(value)) {
    return Engine.makeValue(FSDataType.Integer, value);
  }
  return Engine.makeValue(FSDataType.Float, value);
};

const convertModuleValueToTyped = (value: unknown, seen = new WeakSet<object>()): TypedValue => {
  if (isFuncdrawModuleValue(value)) {
    return Engine.ensureTyped(value.getTypedValue());
  }
  if (value === null || value === undefined) {
    return Engine.makeValue(FSDataType.Null, null);
  }
  if (value && typeof value === 'object') {
    try {
      return Engine.ensureTyped(value as TypedValue);
    } catch {
      // fall through to conversion
    }
  }
  if (Array.isArray(value)) {
    if (seen.has(value)) {
      throw new Error('Cannot convert cyclic module array value.');
    }
    const ref = value as object;
    seen.add(ref);
    const entries = value.map((entry) => convertModuleValueToTyped(entry, seen));
    seen.delete(ref);
    return Engine.makeValue(FSDataType.List, new ArrayFsList(entries));
  }
  const valueType = typeof value;
  if (valueType === 'string') {
    return Engine.makeValue(FSDataType.String, value);
  }
  if (valueType === 'number') {
    const numeric = value as number;
    if (!Number.isFinite(numeric)) {
      return Engine.makeValue(FSDataType.Null, null);
    }
    return convertNumber(numeric);
  }
  if (valueType === 'boolean') {
    return Engine.makeValue(FSDataType.Boolean, value);
  }
  if (valueType === 'bigint') {
    return Engine.makeValue(FSDataType.BigInteger, value);
  }
  if (value instanceof Date) {
    return Engine.makeValue(FSDataType.DateTime, value);
  }
  if (value instanceof BaseFunction) {
    return Engine.makeValue(FSDataType.Function, value);
  }
  if (valueType === 'function') {
    return Engine.makeValue(FSDataType.Function, value as unknown as BaseFunction);
  }
  if (valueType === 'object') {
    const record = value as Record<string, unknown>;
    if (seen.has(record as object)) {
      throw new Error('Cannot convert cyclic module object value.');
    }
    seen.add(record as object);
    const seenKeys = new Set<string>();
    const entries: Array<readonly [string, TypedValue]> = [];
    for (const [key, entry] of Object.entries(record)) {
      const normalized = key.trim().toLowerCase();
      if (!normalized || seenKeys.has(normalized)) {
        continue;
      }
      seenKeys.add(normalized);
      entries.push([key, convertModuleValueToTyped(entry, seen)] as const);
    }
    seen.delete(record as object);
    return Engine.makeValue(
      FSDataType.KeyValueCollection,
      new SimpleKeyValueCollection(null, entries)
    );
  }
  throw new Error(`Unsupported module value type: ${String(valueType)}`);
};

class FuncscriptImportFunction extends BaseFunction {
  private readonly importFn: NonNullable<ModuleImportHandler>;

  constructor(importFn: NonNullable<ModuleImportHandler>) {
    super();
    this.symbol = 'import';
    this.callType = CallType.Prefix;
    this.importFn = importFn;
  }

  get maxParameters() {
    return 1;
  }

  evaluate(provider: FsDataProvider, parameters: ParameterList) {
    if (parameters.count !== 1) {
      throw new Error('import expects exactly one string argument.');
    }
    const typedSpecifier = Engine.ensureTyped(parameters.getParameter(provider, 0));
    const type = Engine.typeOf(typedSpecifier);
    if (type !== FSDataType.String) {
      throw new Error('import argument must be a string.');
    }
    const specifier = String(Engine.valueOf(typedSpecifier));
    const context: ModuleImportContext = { folderPath: getProviderFolderPath(provider) };
    const moduleValue = this.importFn(specifier, context);
    // eslint-disable-next-line no-console
    console.log('[fd-player import]', specifier, typeof moduleValue, moduleValue && typeof moduleValue === 'object' && FUNC_DRAW_MODULE_SENTINEL in (moduleValue as object));
    return convertModuleValueToTyped(moduleValue);
  }
}

class PlayerFsDataProvider extends DefaultFsDataProvider {
  private jsValues = new Map<string, { marker: symbol; value: unknown }>();

  setJsValue(name: string, value: unknown) {
    if (typeof name !== 'string') {
      return;
    }
    const key = name.trim().toLowerCase();
    if (!key) {
      return;
    }
    if (value === undefined) {
      this.jsValues.delete(key);
      return;
    }
    this.jsValues.set(key, { marker: JS_VALUE_SENTINEL, value });
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
    const parent = this.parent as { getJsValue?: (identifier: string) => unknown } | undefined;
    if (parent && typeof parent.getJsValue === 'function') {
      return parent.getJsValue(name);
    }
    return undefined;
  }
}

const applyModuleBindings = (
  provider: PlayerFsDataProvider,
  importFn: ModuleImportHandler
) => {
  if (!importFn) {
    return;
  }
  const funcscriptImport = new FuncscriptImportFunction(importFn);
  provider.set('import', funcscriptImport);
  provider.set('require', funcscriptImport);
  provider.setJsValue('fdimport', importFn);
  provider.setJsValue('require', importFn);
  if (typeof globalThis !== 'undefined') {
    const globalObj = globalThis as Record<string, unknown>;
    if (globalObj['fdimport'] === undefined) {
      try {
        globalObj['fdimport'] = importFn;
      } catch {
        // ignore assignment failures
      }
    }
    if (globalObj['require'] === undefined) {
      try {
        globalObj['require'] = importFn;
      } catch {
        // ignore assignment failures
      }
    }
  }
};

export const createBaseProvider = (importFn: ModuleImportHandler): PlayerFsDataProvider => {
  const provider = new PlayerFsDataProvider();
  applyModuleBindings(provider, importFn);
  return provider;
};

const getProviderFolderPath = (provider: FsDataProvider | null): string[] => {
  let current: unknown = provider;
  const visited = new Set<unknown>();
  while (current && typeof current === 'object' && !visited.has(current)) {
    visited.add(current);
    const folderNode = (current as { folderNode?: { path?: string[] } }).folderNode;
    if (folderNode && Array.isArray(folderNode.path)) {
      return [...folderNode.path];
    }
    const kvcFolder = (current as { kvc?: { folderNode?: { path?: string[] } } }).kvc?.folderNode;
    if (kvcFolder && Array.isArray(kvcFolder.path)) {
      return [...kvcFolder.path];
    }
    const getParentProvider = (current as { getParentProvider?: () => FsDataProvider | null }).getParentProvider;
    if (typeof getParentProvider === 'function') {
      try {
        const parent = getParentProvider.call(current);
        if (parent) {
          current = parent;
          continue;
        }
      } catch {
        // ignore
      }
    }
    current = (current as { parent?: FsDataProvider | null }).parent ?? null;
  }
  return [];
};

import {
  ArrayFsList,
  BaseFunction,
  CallType,
  Engine,
  FSDataType,
  SimpleKeyValueCollection,
  type FsDataProvider,
  type ParameterList,
  type TypedValue
} from '@tewelde/funcscript/browser';
import type { PlayerFsDataProvider } from './graphics';

export type ProjectModuleImport = ((specifier: unknown) => unknown) | null | undefined;

const makeTypedNull = () => Engine.makeValue(FSDataType.Null, null);

const convertNumber = (value: number): TypedValue => {
  if (Number.isInteger(value)) {
    return Engine.makeValue(FSDataType.Integer, value);
  }
  return Engine.makeValue(FSDataType.Float, value);
};

const convertModuleValueToTyped = (value: unknown, seen = new WeakSet<object>()): TypedValue => {
  if (value === null || value === undefined) {
    return makeTypedNull();
  }
  if (Array.isArray(value)) {
    if (seen.has(value)) {
      throw new Error('Cannot convert cyclic module value.');
    }
    const asObject = value as object;
    seen.add(asObject);
    const entries = value.map((entry) => convertModuleValueToTyped(entry, seen));
    seen.delete(asObject);
    return Engine.makeValue(FSDataType.List, new ArrayFsList(entries));
  }
  const valueType = typeof value;
  if (valueType === 'string') {
    return Engine.makeValue(FSDataType.String, value);
  }
  if (valueType === 'number') {
    const numericValue = value as number;
    if (!Number.isFinite(numericValue)) {
      return makeTypedNull();
    }
    return convertNumber(numericValue);
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
  if (valueType === 'object') {
    const objectValue = value as Record<string, unknown>;
    if (seen.has(objectValue)) {
      throw new Error('Cannot convert cyclic module value.');
    }
    seen.add(objectValue);
    const entries: Array<readonly [string, TypedValue]> = Object.entries(objectValue).map(([key, entry]) => [
      key,
      convertModuleValueToTyped(entry, seen)
    ] as const);
    seen.delete(objectValue);
    return Engine.makeValue(
      FSDataType.KeyValueCollection,
      new SimpleKeyValueCollection(null, entries)
    );
  }
  throw new Error(`Unsupported module value type: ${String(valueType)}`);
};

class FuncscriptImportModuleFunction extends BaseFunction {
  private readonly importFn: NonNullable<ProjectModuleImport>;

  constructor(importFn: NonNullable<ProjectModuleImport>) {
    super();
    this.symbol = 'importmodule';
    this.callType = CallType.Prefix;
    this.importFn = importFn;
  }

  get maxParameters() {
    return 1;
  }

  evaluate(provider: FsDataProvider, parameters: ParameterList) {
    if (parameters.count !== 1) {
      throw new Error('importModule expects exactly one string argument.');
    }
    const typedSpecifier = Engine.ensureTyped(parameters.getParameter(provider, 0));
    const specifierType = Engine.typeOf(typedSpecifier);
    if (specifierType !== FSDataType.String) {
      throw new Error('importModule argument must be a string.');
    }
    const specifier = Engine.valueOf(typedSpecifier) as string;
    const moduleValue = this.importFn(specifier);
    return convertModuleValueToTyped(moduleValue);
  }
}

export const createFuncscriptImportFunction = (importFn: ProjectModuleImport): BaseFunction | null => {
  if (!importFn) {
    return null;
  }
  return new FuncscriptImportModuleFunction(importFn);
};

export const applyProjectImportBindings = (
  provider: PlayerFsDataProvider,
  importFn: ProjectModuleImport
) => {
  if (!importFn) {
    return;
  }
  const funcscriptImport = createFuncscriptImportFunction(importFn);
  if (funcscriptImport) {
    provider.set('importModule', funcscriptImport);
    provider.set('require', funcscriptImport);
  }
  const assignBinding = (name: string, options?: { setGlobal?: boolean }) => {
    if (!name) {
      return;
    }
    provider.setJsValue(name, importFn);
    if (options?.setGlobal !== false && typeof globalThis !== 'undefined') {
      const current = (globalThis as Record<string, unknown>)[name];
      if (current === undefined) {
        try {
          (globalThis as Record<string, unknown>)[name] = importFn;
        } catch {
          // Ignore attempts to assign to read-only globals.
        }
      }
    }
  };
  assignBinding('import', { setGlobal: false });
  assignBinding('importModule');
  const shouldSetGlobalRequire =
    typeof globalThis === 'undefined' || (globalThis as Record<string, unknown>).require === undefined;
  assignBinding('require', { setGlobal: shouldSetGlobalRequire });
};

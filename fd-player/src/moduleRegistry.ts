import { FuncDraw } from '@funcdraw/core';
import {
  Engine,
  FSDataType,
  FsList,
  KeyValueCollection,
  type TypedValue
} from '@tewelde/funcscript';
import { MemoryExpressionResolver } from './resolver';
import type { ExpressionEntry } from './types';
import { createBaseProvider, type ModuleImportHandler } from './moduleBindings';

export type ModuleEntryMap = Map<string, ExpressionEntry[]>;

const convertTypedToPlain = (typed: TypedValue | null | undefined): unknown => {
  if (!typed) {
    return null;
  }
  const type = Engine.typeOf(typed);
  const raw = Engine.valueOf(typed);
  switch (type) {
    case FSDataType.Null:
    case FSDataType.Boolean:
    case FSDataType.Integer:
    case FSDataType.Float:
    case FSDataType.String:
    case FSDataType.Guid:
    case FSDataType.DateTime:
    case FSDataType.BigInteger:
      return raw;
    case FSDataType.List: {
      if (raw instanceof FsList && typeof raw.toArray === 'function') {
        return raw.toArray().map((entry: TypedValue) => convertTypedToPlain(entry));
      }
      if (Array.isArray(raw)) {
        return raw.map((entry) => convertTypedToPlain(entry));
      }
      return [];
    }
    case FSDataType.KeyValueCollection: {
      if (raw instanceof KeyValueCollection && typeof raw.getAll === 'function') {
        const result: Record<string, unknown> = {};
        for (const [key, entry] of raw.getAll()) {
          result[key] = convertTypedToPlain(entry);
        }
        return result;
      }
      return raw;
    }
    case FSDataType.Error: {
      const err = raw as { errorType?: string; errorMessage?: string; errorData?: unknown };
      return {
        errorType: err?.errorType ?? 'Error',
        errorMessage: err?.errorMessage ?? 'Unknown error',
        errorData: err?.errorData ?? null
      };
    }
    default:
      return raw;
  }
};

const hasEvalExpression = (handle: ReturnType<typeof FuncDraw.evaluate>, folderPath: string[]) => {
  return handle
    .listExpressions()
    .some((entry) => {
      if (!Array.isArray(entry.path) || entry.path.length !== folderPath.length + 1) {
        return false;
      }
      const matchesParent = entry.path.slice(0, -1).every((segment, index) => segment === folderPath[index]);
      return matchesParent && entry.path.at(-1) === 'eval';
    });
};

const collectFolderValue = (
  handle: ReturnType<typeof FuncDraw.evaluate>,
  folderPath: string[]
): unknown => {
  const typed = handle.getFolderValue(folderPath) as TypedValue | null;
  const plain = convertTypedToPlain(typed);
  if (plain && typeof plain === 'object' && !Array.isArray(plain) && !hasEvalExpression(handle, folderPath)) {
    handle.listFolders(folderPath).forEach((child) => {
      (plain as Record<string, unknown>)[child.name] = collectFolderValue(handle, child.path);
    });
  }
  return plain;
};

const exportModuleSnapshot = (
  entries: ExpressionEntry[],
  importFn: ModuleImportHandler
): Record<string, unknown> => {
  const resolver = new MemoryExpressionResolver(entries);
  const provider = createBaseProvider(importFn);
  const handle = FuncDraw.evaluate(resolver, undefined, { baseProvider: provider });
  const exports: Record<string, unknown> = {};
  const expressions = handle
    .listExpressions()
    .filter((entry) => Array.isArray(entry.path) && entry.path.length === 1);
  expressions.forEach((entry) => {
    const result = handle.evaluateExpression(entry.path);
    if (result && entry.path.length === 1) {
      exports[entry.path[0]] = result.value ?? null;
    }
  });
  handle.listFolders([]).forEach((folder) => {
    if (!folder || !Array.isArray(folder.path)) {
      return;
    }
    try {
      exports[folder.name] = collectFolderValue(handle, folder.path);
    } catch {
      // Ignore folders that fail to evaluate.
    }
  });
  return exports;
};

const cloneModuleValue = (value: unknown, seen = new WeakMap<object, unknown>()): unknown => {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === 'function') {
    return value;
  }
  if (Array.isArray(value)) {
    if (seen.has(value)) {
      return seen.get(value);
    }
    const clone: unknown[] = [];
    seen.set(value, clone);
    value.forEach((entry) => {
      clone.push(cloneModuleValue(entry, seen));
    });
    return clone;
  }
  if (value instanceof Date) {
    return new Date(value.getTime());
  }
  if (typeof value === 'object') {
    if (seen.has(value as object)) {
      return seen.get(value as object);
    }
    const result: Record<string, unknown> = {};
    seen.set(value as object, result);
    for (const [key, entry] of Object.entries(value as Record<string, unknown>)) {
      result[key] = cloneModuleValue(entry, seen);
    }
    return result;
  }
  return value;
};

export class ModuleRegistry {
  private readonly modules: ModuleEntryMap;
  private readonly snapshots = new Map<string, unknown>();
  private readonly resolving = new Set<string>();

  constructor(modules: ModuleEntryMap) {
    this.modules = modules;
  }

  hasModules(): boolean {
    return this.modules.size > 0;
  }

  getImportFunction(): ModuleImportHandler | null {
    if (!this.hasModules()) {
      return null;
    }
    return (specifier: unknown) => this.resolveModule(specifier);
  }

  private resolveModule(specifier: unknown): unknown {
    const normalized = typeof specifier === 'string' ? specifier.trim() : '';
    if (!normalized) {
      throw new Error('Import specifier must be a non-empty string.');
    }
    const entries = this.modules.get(normalized);
    if (!entries) {
      throw new Error(`Module "${normalized}" is not available in this model.`);
    }
    if (this.snapshots.has(normalized)) {
      return cloneModuleValue(this.snapshots.get(normalized));
    }
    if (this.resolving.has(normalized)) {
      throw new Error(`Circular dependency detected while importing "${normalized}".`);
    }
    this.resolving.add(normalized);
    try {
      const snapshot = exportModuleSnapshot(entries, (inner) => this.resolveModule(inner));
      this.snapshots.set(normalized, snapshot);
      return cloneModuleValue(snapshot);
    } finally {
      this.resolving.delete(normalized);
    }
  }
}

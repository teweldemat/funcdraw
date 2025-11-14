import { FuncDraw } from '@funcdraw/core';
import {
  Engine,
  SimpleKeyValueCollection,
  type TypedValue,
  type FuncScriptInput
} from '@tewelde/funcscript';
import { MemoryExpressionResolver } from './resolver.js';
import type { ExpressionEntry } from './types.js';
import {
  createBaseProvider,
  FUNC_DRAW_MODULE_SENTINEL,
  type ModuleImportContext,
  type ModuleImportHandler
} from './moduleBindings.js';

export type ModuleEntryMap = Map<string, ExpressionEntry[]>;

const isRelativeSpecifier = (specifier: string) => specifier.startsWith('./') || specifier.startsWith('../');

const normalizeSegments = (segments: string[]) => segments.map((segment) => segment.trim()).filter((segment) => segment.length > 0);

const resolveRelativePath = (folderPath: string[], specifier: string): string[] => {
  const base = [...folderPath];
  const rawSegments = specifier.split('/');
  rawSegments.forEach((segment) => {
    if (!segment || segment === '.') {
      return;
    }
    if (segment === '..') {
      if (base.length === 0) {
        throw new Error(`Cannot resolve relative import '${specifier}' beyond module root.`);
      }
      base.pop();
      return;
    }
    base.push(segment);
  });
  return normalizeSegments(base);
};

const sliceEntriesByPrefix = (entries: ExpressionEntry[], prefix: string[]): ExpressionEntry[] => {
  if (prefix.length === 0) {
    return entries.slice();
  }
  const trimmed: ExpressionEntry[] = [];
  entries.forEach((entry) => {
    if (entry.path.length < prefix.length) {
      return;
    }
    for (let i = 0; i < prefix.length; i += 1) {
      if (entry.path[i].toLowerCase() !== prefix[i].toLowerCase()) {
        return;
      }
    }
    const nextPath = entry.path.slice(prefix.length);
    if (nextPath.length === 0) {
      return;
    }
    trimmed.push({ ...entry, path: nextPath });
  });
  return trimmed;
};

export class ModuleRegistry {
  private readonly modules: ModuleEntryMap;
  private readonly cache = new Map<string, unknown>();
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
    return (specifier, context) => this.resolveModule(specifier, context);
  }

  private resolveModule(
    specifier: unknown,
    context?: ModuleImportContext | null,
    currentModule?: string
  ): unknown {
    const normalized = typeof specifier === 'string' ? specifier.trim() : '';
    if (!normalized) {
      throw new Error('Import specifier must be a non-empty string.');
    }
    if (isRelativeSpecifier(normalized)) {
      if (!currentModule) {
        throw new Error(`Relative import '${normalized}' is not allowed outside of a module.`);
      }
      const folderPath = context?.folderPath ?? [];
      const targetPath = resolveRelativePath(folderPath, normalized);
      const cacheKey = `${currentModule}::${targetPath.join('/') || '.'}`;
      if (this.cache.has(cacheKey)) {
        return this.cache.get(cacheKey);
      }
      const moduleEntries = this.modules.get(currentModule);
      if (!moduleEntries) {
        throw new Error(`Module "${currentModule}" is not available in this model.`);
      }
      const subset = sliceEntriesByPrefix(moduleEntries, targetPath);
      if (subset.length === 0) {
        throw new Error(`Relative import '${normalized}' did not match any expressions.`);
      }
      const snapshot = this.evaluateEntries(subset, currentModule, targetPath);
      this.cache.set(cacheKey, snapshot);
      return snapshot;
    }

    const entries = this.modules.get(normalized);
    if (!entries) {
      throw new Error(`Module "${normalized}" is not available in this model.`);
    }
    if (this.cache.has(normalized)) {
      return this.cache.get(normalized);
    }
    if (this.resolving.has(normalized)) {
      throw new Error(`Circular dependency detected while importing "${normalized}".`);
    }
    this.resolving.add(normalized);
    try {
      const snapshot = this.evaluateEntries(entries, normalized, []);
      this.cache.set(normalized, snapshot);
      return snapshot;
    } finally {
      this.resolving.delete(normalized);
    }
  }

  private evaluateEntries(entries: ExpressionEntry[], moduleName: string, basePath: string[]) {
    const resolver = new MemoryExpressionResolver(entries);
    const provider = createBaseProvider((specifier, context) =>
      this.resolveModule(specifier, this.mergeContext(basePath, context), moduleName)
    );
    const handle = FuncDraw.evaluate(resolver, undefined, { baseProvider: provider });
    const createCollection = (segments: string[]): SimpleKeyValueCollection => {
      const items = resolver.listItems(segments);
      const entries: Array<readonly [string, TypedValue]> = [];
      items.forEach((item) => {
        const itemPath = [...segments, item.name];
        if (item.kind === 'folder') {
          const typedFolder = handle.getFolderValue(itemPath);
          entries.push([item.name, Engine.ensureTyped(typedFolder)]);
          return;
        }
        if (item.kind === 'expression') {
          const evaluation = handle.evaluateExpression(itemPath);
          if (!evaluation) {
            return;
          }
          if (evaluation.error) {
            throw new Error(
              `Failed to evaluate module "${moduleName}" expression ${itemPath.join('/')}: ${evaluation.error}`
            );
          }
          const fallback = (evaluation.value ?? null) as FuncScriptInput;
          const typed = evaluation.typed ?? Engine.ensureTyped(fallback);
          entries.push([item.name, typed]);
        }
      });
      return new SimpleKeyValueCollection(null, entries);
    };
    const collection = createCollection([]);
    const typedRoot = Engine.ensureTyped(collection);
    return {
      [FUNC_DRAW_MODULE_SENTINEL]: true,
      getTypedValue() {
        return typedRoot;
      }
    };
  }

  private mergeContext(basePath: string[], context?: ModuleImportContext | null): ModuleImportContext {
    const folderPath = context?.folderPath ?? [];
    return { folderPath: normalizeSegments([...basePath, ...folderPath]) };
  }

}

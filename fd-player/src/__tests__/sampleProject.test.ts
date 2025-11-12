import { describe, expect, it, vi } from 'vitest';
vi.mock('../App.css', () => ({}));
import path from 'path';
import fs from 'fs';
import { FuncDraw } from '@tewelde/funcdraw';
import {
  Engine,
  FSDataType,
  FsList,
  KeyValueCollection
} from '@tewelde/funcscript/browser';
import { prepareProvider } from '../graphics';
import { applyProjectImportBindings } from '../importModuleFunction';

const SUPPORTED = [
  { extension: '.fs', language: 'funcscript' as const },
  { extension: '.js', language: 'javascript' as const }
];

type ResolverItem =
  | { kind: 'folder'; name: string; createdAt: number }
  | { kind: 'expression'; name: string; createdAt: number; language: 'funcscript' | 'javascript' };

const createProjectResolver = (projectRoot: string) => {
  const resolveFolder = (segments: string[]) => path.resolve(projectRoot, ...segments);

  const listItems = (segments: string[]): ResolverItem[] => {
    const target = resolveFolder(segments);
    if (!fs.existsSync(target) || !fs.statSync(target).isDirectory()) {
      return [];
    }
    const entries = fs.readdirSync(target, { withFileTypes: true });
    const items: ResolverItem[] = [];
    let index = 0;
    for (const entry of entries) {
      if (entry.name.startsWith('.')) {
        index += 1;
        continue;
      }
      if (entry.isDirectory()) {
        items.push({ kind: 'folder', name: entry.name, createdAt: index });
      } else if (entry.isFile()) {
        const lower = entry.name.toLowerCase();
        const match = SUPPORTED.find((candidate) => lower.endsWith(candidate.extension));
        if (match) {
          items.push({
            kind: 'expression',
            name: entry.name.slice(0, -match.extension.length),
            createdAt: index,
            language: match.language
          });
        }
      }
      index += 1;
    }
    return items;
  };

  const readExpression = (segments: string[]) => {
    if (segments.length === 0) {
      return null;
    }
    const folderSegments = segments.slice(0, -1);
    const name = segments[segments.length - 1];
    const folderPath = resolveFolder(folderSegments);
    for (const candidate of SUPPORTED) {
      const filePath = path.resolve(folderPath, `${name}${candidate.extension}`);
      if (fs.existsSync(filePath) && fs.statSync(filePath).isFile()) {
        return fs.readFileSync(filePath, 'utf8');
      }
    }
    return null;
  };

  return {
    listItems,
    getExpression: readExpression
  };
};

const evaluateProject = (
  projectRoot: string,
  moduleSpecifier: string,
  moduleFactory: () => unknown
) => {
  const resolver = createProjectResolver(projectRoot);
  const provider = prepareProvider();
  const snapshot = JSON.parse(JSON.stringify(moduleFactory()));
  applyProjectImportBindings(provider, (specifier) => {
    const normalized = typeof specifier === 'string' ? specifier.trim() : '';
    if (normalized === moduleSpecifier || normalized.startsWith(`${moduleSpecifier}/`)) {
      return snapshot;
    }
    throw new Error(`Unknown module ${String(specifier)} (normalized: ${normalized})`);
  });
  const funcDraw = FuncDraw.evaluate(resolver, undefined, { baseProvider: provider });
  return funcDraw.evaluateExpression(['main']);
};

const convertTypedToPlain = (typed: unknown): unknown => {
  if (!typed) {
    return null;
  }
  const type = Engine.typeOf(typed as never);
  const raw = Engine.valueOf(typed as never);
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
        return raw.toArray().map((entry) => convertTypedToPlain(entry));
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
    default:
      return raw;
  }
};

const hasEvalExpression = (handle: ReturnType<typeof FuncDraw.evaluate>, folderPath: string[]) => {
  return handle
    .listExpressions()
    .some(
      (entry) =>
        entry.path.length === folderPath.length + 1 &&
        entry.path.slice(0, -1).every((segment, index) => segment === folderPath[index]) &&
        entry.path.at(-1) === 'eval'
    );
};

const collectFolderValue = (
  handle: ReturnType<typeof FuncDraw.evaluate>,
  folderPath: string[]
): unknown => {
  const typed = handle.getFolderValue(folderPath);
  const plain = convertTypedToPlain(typed);
  if (plain && typeof plain === 'object' && !Array.isArray(plain) && !hasEvalExpression(handle, folderPath)) {
    handle.listFolders(folderPath).forEach((child) => {
      (plain as Record<string, unknown>)[child.name] = collectFolderValue(handle, child.path);
    });
  }
  return plain;
};

const loadFuncdrawPackageSnapshot = (packageRoot: string) => {
  const workspaceRoot = fs.existsSync(path.join(packageRoot, 'workspace'))
    ? path.join(packageRoot, 'workspace')
    : packageRoot;
  const resolver = createProjectResolver(workspaceRoot);
  const handle = FuncDraw.evaluate(resolver);
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
      // ignore folders that fail to evaluate
    }
  });

  return exports;
};

describe('aurora demo project', () => {
  it('imports the aurora-library package defined in FuncScript', () => {
    const result = evaluateProject(
      path.resolve(__dirname, '../../../projects/aurora-demo'),
      'aurora-library',
      () => loadFuncdrawPackageSnapshot(path.resolve(__dirname, '../../../packages/aurora-library'))
    );
    expect(result).not.toBeNull();
    expect(result?.error).toBeNull();
  });
});

describe('cartoon library package', () => {
  it('exposes the tree module with optional type selection', () => {
    const snapshot = loadFuncdrawPackageSnapshot(path.resolve(__dirname, '../../../packages/cartoon'));
    expect(snapshot).not.toBeNull();
    expect(snapshot?.landscape?.tree).toBeTruthy();
    const treeModule = snapshot.landscape.tree;
    expect(treeModule).toBeTruthy();
    expect(typeof treeModule).toBe('object');
    expect(typeof treeModule.evaluate).toBe('function');
  });
});

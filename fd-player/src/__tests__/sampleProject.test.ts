import { describe, expect, it, vi } from 'vitest';
vi.mock('../App.css', () => ({}));
import path from 'path';
import fs from 'fs';
import { createRequire } from 'module';
import { FuncDraw } from '@tewelde/funcdraw';
import { prepareProvider } from '../graphics';
import { applyProjectImportBindings } from '../importModuleFunction';

const requireModule = createRequire(import.meta.url);
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

describe('sample project integration', () => {
  it('allows JavaScript expressions to import common-graphic', () => {
    const result = evaluateProject(
      path.resolve(__dirname, '../../../projects/sample'),
      'common-graphic',
      () => requireModule('../../../packages/common-graphic')
    );
    expect(result).not.toBeNull();
    expect(result?.error).toBeNull();
  });
});

describe('landscape demo project', () => {
  it('imports the landscape-library package', () => {
    const result = evaluateProject(
      path.resolve(__dirname, '../../../projects/landscape_demo'),
      'landscape-library',
      () => requireModule('../../../packages/landscape-library')
    );
    expect(result).not.toBeNull();
    expect(result?.error).toBeNull();
  });
});

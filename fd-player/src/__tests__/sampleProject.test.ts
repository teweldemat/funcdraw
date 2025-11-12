import { describe, expect, it, vi } from 'vitest';
vi.mock('../App.css', () => ({}));
import path from 'path';
import fs from 'fs';
import { createRequire } from 'module';
import { FuncDraw } from '@tewelde/funcdraw';
import { prepareProvider } from '../graphics';
import { applyProjectImportBindings } from '../importModuleFunction';

const requireModule = createRequire(import.meta.url);
const PROJECT_ROOT = path.resolve(__dirname, '../../../projects/sample');
const SUPPORTED = [
  { extension: '.fs', language: 'funcscript' as const },
  { extension: '.js', language: 'javascript' as const }
];

const listItems = (segments: string[]) => {
  const target = path.resolve(PROJECT_ROOT, ...segments);
  if (!fs.existsSync(target) || !fs.statSync(target).isDirectory()) {
    return [];
  }
  const entries = fs.readdirSync(target, { withFileTypes: true });
  const items: Array<
    | { kind: 'folder'; name: string; createdAt: number }
    | { kind: 'expression'; name: string; createdAt: number; language: 'funcscript' | 'javascript' }
  > = [];
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
  const folderPath = path.resolve(PROJECT_ROOT, ...folderSegments);
  for (const candidate of SUPPORTED) {
    const filePath = path.resolve(folderPath, `${name}${candidate.extension}`);
    if (fs.existsSync(filePath) && fs.statSync(filePath).isFile()) {
      return fs.readFileSync(filePath, 'utf8');
    }
  }
  return null;
};

describe('sample project integration', () => {
  it('allows JavaScript expressions to import modules', () => {
    const resolver = {
      listItems,
      getExpression: readExpression
    };
    const provider = prepareProvider();
    const moduleValue = JSON.parse(
      JSON.stringify(requireModule('../../../packages/common-graphic'))
    );
    applyProjectImportBindings(provider, (specifier) => {
      if (specifier === 'common-graphic') {
        return moduleValue;
      }
      throw new Error(`Unknown module ${String(specifier)}`);
    });
    const funcDraw = FuncDraw.evaluate(resolver, undefined, { baseProvider: provider });
    const result = funcDraw.evaluateExpression(['main']);
    expect(result).not.toBeNull();
    expect(result?.error).toBeNull();
  });
});

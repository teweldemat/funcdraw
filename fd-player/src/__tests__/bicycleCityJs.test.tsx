import { describe, it, expect } from 'vitest';
import React from 'react';
import { render, screen } from '@testing-library/react';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';
import { FuncDraw } from '@tewelde/funcdraw';
import { interpretGraphics, evaluateExpression } from '../graphics';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const EXAMPLE_ROOT = path.resolve(__dirname, '../examples/ghost_bicyle_js');
const SUPPORTED = [
  { extension: '.fs', language: 'funcscript' as const },
  { extension: '.js', language: 'javascript' as const }
];

type ResolverItem =
  | { kind: 'folder'; name: string; createdAt: number }
  | { kind: 'expression'; name: string; createdAt: number; language: 'funcscript' | 'javascript' };

type Resolver = {
  listItems(pathSegments: string[]): ResolverItem[];
  getExpression(pathSegments: string[]): string | null;
};

const resolveFolder = (segments: string[]): string => path.resolve(EXAMPLE_ROOT, ...segments);

const listItems = (segments: string[]): ResolverItem[] => {
  const folderPath = resolveFolder(segments);
  if (!fs.existsSync(folderPath) || !fs.statSync(folderPath).isDirectory()) {
    return [];
  }
  const entries = fs.readdirSync(folderPath, { withFileTypes: true });
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

const readExpression = (segments: string[]): string | null => {
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

const createResolver = (): Resolver => ({
  listItems,
  getExpression: readExpression
});

const loadGraphicsValue = () => {
  const resolver = createResolver();
  const funcDraw = FuncDraw.evaluate(resolver);
  const provider = funcDraw.environmentProvider;
  const expression = '{\n  return model.graphics;\n}';
  const result = evaluateExpression(provider, expression, 'funcscript');
  if (result.error) {
    throw new Error(result.error);
  }
  return result.value;
};

const GraphicsWarningProbe = ({ value }: { value: unknown }) => {
  const interpretation = interpretGraphics(value);
  return <div data-testid="warnings">{interpretation.warning ?? ''}</div>;
};

describe('ghost_bicyle_js integration', () => {
  it('currently emits primitive warnings when FuncScript main returns model.graphics', () => {
    const graphicsValue = loadGraphicsValue();
    render(<GraphicsWarningProbe value={graphicsValue} />);
    const warningText = screen.getByTestId('warnings').textContent ?? '';
    expect(warningText).toBe('');
  });
});

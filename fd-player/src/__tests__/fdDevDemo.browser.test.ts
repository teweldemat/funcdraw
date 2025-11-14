import { describe, expect, it } from 'vitest';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { readFile, readdir, access } from 'node:fs/promises';
import { FuncdrawPlayer } from '../player.js';
import { MemoryExpressionResolver } from '../resolver.js';
import { normalizeWorkspaceFiles } from '../loader.js';
import type { ExpressionEntry, WorkspaceFile } from '../types.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const WORKSPACE_ROOT = path.resolve(__dirname, '../../..', 'projects', 'fd-dev-demo');
const IGNORED_FOLDERS = new Set(['node_modules', '.git', 'dist', 'build', '.turbo']);

const createStubContext = (): CanvasRenderingContext2D => {
  const noop = () => {};
  return {
    canvas: document.createElement('canvas'),
    save: noop,
    restore: noop,
    clearRect: noop,
    fillRect: noop,
    setLineDash: noop,
    beginPath: noop,
    moveTo: noop,
    lineTo: noop,
    stroke: noop,
    fill: noop,
    closePath: noop,
    arc: noop,
    ellipse: noop,
    rect: noop,
    fillText: noop,
    translate: noop,
    scale: noop,
    rotate: noop,
    strokeStyle: '#000',
    fillStyle: '#000',
    lineWidth: 1,
    lineCap: 'butt',
    lineJoin: 'miter',
    font: '',
    textAlign: 'left',
    textBaseline: 'alphabetic'
  } as unknown as CanvasRenderingContext2D;
};

const pathExists = async (target: string) => {
  try {
    await access(target);
    return true;
  } catch {
    return false;
  }
};

const readWorkspaceFiles = async (dir: string, prefix = ''): Promise<WorkspaceFile[]> => {
  const entries = await readdir(dir, { withFileTypes: true });
  const files: WorkspaceFile[] = [];
  await Promise.all(
    entries.map(async (entry) => {
      if (entry.name.startsWith('.')) {
        return;
      }
      if (IGNORED_FOLDERS.has(entry.name)) {
        return;
      }
      const fullPath = path.join(dir, entry.name);
      const relativePath = prefix ? `${prefix}/${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        const nested = await readWorkspaceFiles(fullPath, relativePath);
        files.push(...nested);
        return;
      }
      if (!/\.(fs|js)$/i.test(entry.name)) {
        return;
      }
      const content = await readFile(fullPath, 'utf8');
      files.push({ path: relativePath, content });
    })
  );
  return files;
};

const resolveWorkspaceRoot = async (root: string) => {
  const nested = path.join(root, 'workspace');
  return (await pathExists(nested)) ? nested : root;
};

const collectFileDependencies = async (projectRoot: string): Promise<Map<string, WorkspaceFile[]>> => {
  const packageJsonPath = path.join(projectRoot, 'package.json');
  let parsed: unknown;
  try {
    const raw = await readFile(packageJsonPath, 'utf8');
    parsed = JSON.parse(raw);
  } catch {
    return new Map();
  }
  if (!parsed || typeof parsed !== 'object') {
    return new Map();
  }
  const dependencies = (parsed as { dependencies?: Record<string, unknown> }).dependencies;
  if (!dependencies || typeof dependencies !== 'object') {
    return new Map();
  }
  const modules = new Map<string, WorkspaceFile[]>();
  await Promise.all(
    Object.entries(dependencies as Record<string, unknown>).map(async ([name, spec]) => {
      if (typeof spec !== 'string' || !spec.startsWith('file:')) {
        return;
      }
      const dependencyRoot = path.resolve(projectRoot, spec.slice(5));
      if (!(await pathExists(dependencyRoot))) {
        return;
      }
      const workspaceDir = await resolveWorkspaceRoot(dependencyRoot);
      try {
        const files = await readWorkspaceFiles(workspaceDir);
        if (files.length > 0) {
          modules.set(name, files);
        }
      } catch {
        // Ignore dependency read errors so one bad dependency does not fail the test.
      }
    })
  );
  return modules;
};

describe('fd-dev-demo workspace', () => {
  it('renders without throwing when module dependencies are available', async () => {
    const workspaceFiles = await readWorkspaceFiles(WORKSPACE_ROOT);
    const entries = normalizeWorkspaceFiles(workspaceFiles);
    const dependencyFiles = await collectFileDependencies(WORKSPACE_ROOT);
    const modules = new Map<string, ExpressionEntry[]>();
    dependencyFiles.forEach((files, specifier) => {
      const normalized = normalizeWorkspaceFiles(files, { stripCommonRoot: false });
      if (normalized.length > 0) {
        modules.set(specifier, normalized);
      }
    });

    const resolver = new MemoryExpressionResolver(entries);
    const canvas = document.createElement('canvas');
    canvas.width = 1280;
    canvas.height = 720;
    canvas.getContext = () => createStubContext();
    const player = new FuncdrawPlayer({ canvas });
    player.setResolver(resolver, modules);

    expect(() => player.render()).not.toThrow();
  });
});

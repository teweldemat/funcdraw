import { describe, expect, it } from 'vitest';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { access, readdir, readFile } from 'node:fs/promises';
import JSZip from 'jszip';
import { FuncdrawPlayer } from '../player';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const WORKSPACE_ROOT = path.resolve(__dirname, '../../..', 'projects', 'fd-dev-demo');
const IGNORED_FOLDERS = new Set(['node_modules', '.git', 'dist', 'build', '.turbo']);
const MODULE_FOLDER = '__modules__';

interface WorkspaceFile {
  path: string;
  content: string;
}

const encodeModuleSpecifier = (specifier: string) => encodeURIComponent(specifier);

const readWorkspaceFiles = async (dir: string, prefix = ''): Promise<WorkspaceFile[]> => {
  const entries = await readdir(dir, { withFileTypes: true });
  const files: WorkspaceFile[] = [];
  for (const entry of entries) {
    if (entry.name.startsWith('.')) {
      continue;
    }
    const fullPath = path.join(dir, entry.name);
    const relativePath = prefix ? `${prefix}/${entry.name}` : entry.name;
    if (entry.isDirectory()) {
      if (IGNORED_FOLDERS.has(entry.name)) {
        continue;
      }
      const nested = await readWorkspaceFiles(fullPath, relativePath);
      files.push(...nested);
    } else if (/\.(fs|js)$/i.test(entry.name)) {
      const content = await readFile(fullPath, 'utf8');
      files.push({ path: relativePath, content });
    }
  }
  return files;
};

const pathExists = async (target: string) => {
  try {
    await access(target);
    return true;
  } catch {
    return false;
  }
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
  for (const [name, spec] of Object.entries(dependencies as Record<string, unknown>)) {
    if (typeof spec !== 'string' || !spec.startsWith('file:')) {
      continue;
    }
    const dependencyRoot = path.resolve(projectRoot, spec.slice(5));
    if (!(await pathExists(dependencyRoot))) {
      continue;
    }
    const workspaceDir = await resolveWorkspaceRoot(dependencyRoot);
    try {
      const files = await readWorkspaceFiles(workspaceDir);
      if (files.length > 0) {
        modules.set(name, files);
      }
    } catch {
      // Ignore dependency read errors in the test harness.
    }
  }
  return modules;
};

const createModelArchive = async (root: string): Promise<Uint8Array> => {
  const files = await readWorkspaceFiles(root);
  const dependencyFiles = await collectFileDependencies(root);
  const zip = new JSZip();
  const rootFolder = path.basename(root) || 'workspace';
  files.forEach((file) => {
    zip.file(`${rootFolder}/${file.path}`, file.content);
  });
  dependencyFiles.forEach((moduleFiles, specifier) => {
    const encoded = encodeModuleSpecifier(specifier);
    moduleFiles.forEach((file) => {
      zip.file(`${rootFolder}/${MODULE_FOLDER}/${encoded}/${file.path}`, file.content);
    });
  });
  const buffer = await zip.generateAsync({
    type: 'uint8array',
    compression: 'DEFLATE',
    compressionOptions: { level: 6 }
  });
  return buffer;
};

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

describe('fd-dev-demo workspace', () => {
  it('renders without throwing once file-based dependencies are respected', async () => {
    const archive = await createModelArchive(WORKSPACE_ROOT);
    const canvas = document.createElement('canvas');
    canvas.width = 1280;
    canvas.height = 720;
    canvas.getContext = () => createStubContext();
    const player = new FuncdrawPlayer({ canvas });
    await player.loadArchiveFromBinary(archive);

    expect(() => player.render()).not.toThrow();
  });
});

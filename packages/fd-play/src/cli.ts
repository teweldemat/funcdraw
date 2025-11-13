#!/usr/bin/env node
import { Command } from 'commander';
import { createServer } from 'node:http';
import { readdir, readFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import esbuild from 'esbuild';
import open from 'open';
import { normalizeWorkspaceFiles } from '@funcdraw/fd-player/dist/loader.js';
import type { ExpressionEntry } from '@funcdraw/fd-player/dist/types.js';

const DEFAULT_PORT = 4123;
const IGNORED_FOLDERS = new Set(['node_modules', '.git', 'dist', 'build', '.turbo']);

const program = new Command();
program
  .name('fd-play')
  .description('Launch the FuncDraw player in your browser for a local workspace')
  .option('-r, --root <path>', 'Project root containing FuncDraw expressions', process.cwd())
  .option('-p, --port <number>', 'Port to listen on (default 4123)', (value) => parseInt(value, 10))
  .option('--no-open', 'Do not automatically open the default browser');

program.parse(process.argv);
const options = program.opts<Record<string, unknown>>();
const workspaceRoot = path.resolve(String(options.root ?? process.cwd()));
const port = Number(options.port) || DEFAULT_PORT;
const shouldOpen = options.open !== false;

if (!existsSync(workspaceRoot)) {
  console.error(`Workspace root ${workspaceRoot} does not exist.`);
  process.exit(1);
}

const templateHtml = () => `<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>FuncDraw Player</title>
    <style>
      * { box-sizing: border-box; }
      body {
        margin: 0;
        padding: 16px;
        background: #0f172a;
        color: #e2e8f0;
        font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
        display: flex;
        flex-direction: column;
        gap: 12px;
        min-height: 100vh;
      }
      header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 12px;
      }
      button {
        background: #22d3ee;
        color: #0f172a;
        border: none;
        padding: 8px 16px;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
      }
      button:hover {
        background: #67e8f9;
      }
      #fd-status[data-error="true"] {
        color: #fca5a5;
      }
      canvas {
        width: 100%;
        max-width: 960px;
        border-radius: 12px;
        align-self: center;
        box-shadow: 0 25px 50px -12px rgb(15 23 42 / 0.6);
      }
    </style>
  </head>
  <body>
    <header>
      <div>
        <strong>FuncDraw Player</strong>
        <div id="fd-status">Preparing workspace…</div>
      </div>
      <button id="fd-reload" type="button">Reload</button>
    </header>
    <canvas id="fd-canvas" width="1280" height="720"></canvas>
    <script type="module" src="/app.js"></script>
  </body>
</html>`;

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const clientEntry = path.resolve(__dirname, '../src/client/main.ts');

const buildClientBundle = async () => {
  const bundle = await esbuild.build({
    entryPoints: [clientEntry],
    bundle: true,
    format: 'esm',
    platform: 'browser',
    sourcemap: 'inline',
    write: false,
    target: 'es2020'
  });
  return bundle.outputFiles[0].text;
};

const readWorkspaceFiles = async (dir: string, prefix = ''): Promise<{ path: string; content: string }[]> => {
  const entries = await readdir(dir, { withFileTypes: true });
  const results: { path: string; content: string }[] = [];
  await Promise.all(
    entries.map(async (entry) => {
      if (entry.name.startsWith('.')) {
        return;
      }
      const fullPath = path.join(dir, entry.name);
      const relativePath = prefix ? `${prefix}/${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        if (IGNORED_FOLDERS.has(entry.name)) {
          return;
        }
        const nested = await readWorkspaceFiles(fullPath, relativePath);
        results.push(...nested);
      } else if (/\.(fs|js)$/i.test(entry.name)) {
        const content = await readFile(fullPath, 'utf8');
        results.push({ path: relativePath, content });
      }
    })
  );
  return results;
};

const resolveWorkspaceRoot = (packageRoot: string) => {
  const nestedWorkspace = path.join(packageRoot, 'workspace');
  return existsSync(nestedWorkspace) ? nestedWorkspace : packageRoot;
};

const collectFileDependencies = async (
  projectRoot: string
): Promise<Map<string, { path: string; content: string }[]>> => {
  const packageJsonPath = path.join(projectRoot, 'package.json');
  if (!existsSync(packageJsonPath)) {
    return new Map();
  }
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
  const modules = new Map<string, { path: string; content: string }[]>();
  const projectRequire = createRequire(packageJsonPath);

  const resolveDependencyRoot = (name: string, spec: unknown): string | null => {
    if (typeof spec === 'string' && spec.startsWith('file:')) {
      const resolved = path.resolve(projectRoot, spec.slice(5));
      return existsSync(resolved) ? resolved : null;
    }
    const nodeModulesPath = path.join(projectRoot, 'node_modules', name);
    if (existsSync(nodeModulesPath)) {
      return nodeModulesPath;
    }
    try {
      const resolvedPackage = projectRequire.resolve(`${name}/package.json`);
      return path.dirname(resolvedPackage);
    } catch {
      return null;
    }
  };

  for (const [name, spec] of Object.entries(dependencies as Record<string, unknown>)) {
    const dependencyRoot = resolveDependencyRoot(name, spec);
    if (!dependencyRoot) {
      continue;
    }
    const workspaceDir = resolveWorkspaceRoot(dependencyRoot);
    const hasManifest = existsSync(path.join(dependencyRoot, 'funcdraw.json')) || existsSync(path.join(workspaceDir, 'funcdraw.json'));
    if (!hasManifest) {
      continue;
    }
    try {
      const files = await readWorkspaceFiles(workspaceDir);
      if (files.length > 0) {
        modules.set(name, files);
      }
    } catch {
      // Ignore dependency read errors so one bad dependency does not stop playback.
    }
  }
  return modules;
};

const createWorkspacePayload = async (root: string) => {
  const files = await readWorkspaceFiles(root);
  if (files.length === 0) {
    throw new Error('No FuncDraw expressions (.fs or .js) were found.');
  }
  const entries = normalizeWorkspaceFiles(files);
  const dependencyFiles = await collectFileDependencies(root);
  const modules: Record<string, ExpressionEntry[]> = {};
  dependencyFiles.forEach((moduleFiles, specifier) => {
    const normalized = normalizeWorkspaceFiles(moduleFiles, { stripCommonRoot: false });
    if (normalized.length > 0) {
      modules[specifier] = normalized;
    }
  });
  return { entries, modules };
};

const clientBundlePromise = buildClientBundle();

const server = createServer(async (req, res) => {
  try {
    const url = req.url ? new URL(req.url, `http://localhost:${port}`) : null;
    const pathname = url?.pathname ?? '/';
    if (pathname === '/' || pathname === '/index.html') {
      res.setHeader('Content-Type', 'text/html; charset=utf-8');
      res.end(templateHtml());
      return;
    }
    if (pathname === '/app.js') {
      const bundle = await clientBundlePromise;
      res.setHeader('Content-Type', 'application/javascript; charset=utf-8');
      res.setHeader('Cache-Control', 'no-store');
      res.end(bundle);
      return;
    }
    if (pathname === '/workspace.json') {
      try {
        const payload = await createWorkspacePayload(workspaceRoot);
        res.setHeader('Content-Type', 'application/json; charset=utf-8');
        res.setHeader('Cache-Control', 'no-store');
        res.end(JSON.stringify(payload));
      } catch (err) {
        res.statusCode = 500;
        res.end(err instanceof Error ? err.message : 'Failed to prepare workspace payload');
      }
      return;
    }
    res.statusCode = 404;
    res.end('Not Found');
  } catch (err) {
    res.statusCode = 500;
    res.end(err instanceof Error ? err.message : 'Unexpected error');
  }
});

server.listen(port, () => {
  const url = `http://localhost:${port}/`;
  console.log(`FuncDraw player ready on ${url}`);
  if (shouldOpen) {
    void open(url).catch((err) => {
      console.warn('Failed to open browser:', err instanceof Error ? err.message : String(err));
    });
  }
});

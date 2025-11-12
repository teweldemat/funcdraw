#!/usr/bin/env node
import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const findRepoRoot = (startDir) => {
  let current = startDir;
  while (true) {
    const pkgPath = path.join(current, 'package.json');
    if (fs.existsSync(pkgPath)) {
      try {
        const contents = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
        const workspaces = contents?.workspaces;
        if (Array.isArray(workspaces) && workspaces.includes('fd-player')) {
          return current;
        }
        if (workspaces && typeof workspaces === 'object' && Array.isArray(workspaces.packages)) {
          if (workspaces.packages.includes('fd-player') || workspaces.packages.includes('fd-player/*')) {
            return current;
          }
        }
      } catch {
        // Ignore parse errors and continue walking up the tree.
      }
    }
    const parent = path.dirname(current);
    if (parent === current) {
      throw new Error('Unable to locate monorepo root containing fd-player workspace.');
    }
    current = parent;
  }
};

const projectRoot = process.cwd();
const configPath = path.join(projectRoot, 'funcdraw.json');

if (!fs.existsSync(configPath)) {
  console.error('Expected funcdraw.json in project root.');
  process.exit(1);
}

let repoRoot;
try {
  repoRoot = findRepoRoot(projectRoot);
} catch (err) {
  console.error(err instanceof Error ? err.message : String(err));
  process.exit(1);
}

const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';

const child = spawn(npmCommand, ['run', 'dev', '--workspace', 'fd-player'], {
  cwd: repoRoot,
  stdio: 'inherit',
  env: {
    ...process.env,
    FUNCPLAY_PROJECT_FILE: configPath,
    FUNCPLAY_PROJECT_ROOT: projectRoot
  }
});

child.on('exit', (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 0);
});

child.on('error', (err) => {
  console.error('Failed to launch fd-player dev server:', err instanceof Error ? err.message : String(err));
  process.exit(1);
});

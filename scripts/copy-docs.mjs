import { cp, rm, stat } from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');
const docsDir = path.join(rootDir, 'docs');
const siteDir = path.join(docsDir, 'site');
const playerDir = path.join(docsDir, 'player');
const targetDir = path.join(rootDir, 'fd-editor', 'dist', 'docs');

async function ensureDirExists(dir, label) {
  try {
    await stat(dir);
  } catch (err) {
    console.error(`${label} not found at ${dir}`);
    throw err;
  }
}

async function copyDocs() {
  await ensureDirExists(siteDir, 'Built docs');
  await rm(targetDir, { recursive: true, force: true });
  await cp(siteDir, targetDir, { recursive: true });
  try {
    await stat(playerDir);
    await cp(playerDir, path.join(targetDir, 'player'), { recursive: true });
  } catch (err) {
    if (err.code !== 'ENOENT') {
      throw err;
    }
  }
  console.log(`Synced docs to ${targetDir}`);
}

copyDocs().catch((err) => {
  console.error('Failed to sync docs:', err);
  process.exitCode = 1;
});

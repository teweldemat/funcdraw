import { spawn } from 'child_process';
import { mkdir } from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');
const cliPath = path.join(rootDir, 'fd-cli', 'bin', 'fd-cli.js');
const exampleRoot = path.join(rootDir, 'docs', 'examples', 'programming-model');
const outputDir = path.join(rootDir, 'docs', 'assets', 'programming-model');

const renders = [
  { expression: 'modules_preview', out: 'modules.png', width: 900, height: 300 },
  { expression: 'collections_preview', out: 'collections.png', width: 900, height: 320 },
  { expression: 'main', out: 'main.png', width: 1200, height: 720 }
];

async function runCli(args) {
  return new Promise((resolve, reject) => {
    const child = spawn('node', args, { stdio: 'inherit' });
    child.on('close', (code) => {
      if (code === 0) {
        resolve();
      } else {
        reject(new Error(`fd-cli exited with code ${code}`));
      }
    });
  });
}

async function render() {
  await mkdir(outputDir, { recursive: true });
  for (const renderJob of renders) {
    const outPath = path.join(outputDir, renderJob.out);
    const args = [
      cliPath,
      '--root',
      exampleRoot,
      '--expression',
      renderJob.expression,
      '--view',
      'view',
      '--format',
      'png',
      '--out',
      outPath,
      '--width',
      String(renderJob.width ?? 1280),
      '--height',
      String(renderJob.height ?? 720),
      '--background',
      '#0b1120'
    ];
    await runCli(args);
    console.log(`Rendered ${renderJob.expression} -> ${path.relative(rootDir, outPath)}`);
  }
}

render().catch((err) => {
  console.error('Failed to render doc previews:', err);
  process.exitCode = 1;
});

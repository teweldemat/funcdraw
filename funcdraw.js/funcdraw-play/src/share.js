'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const picocolors = require('picocolors');
const yargs = require('yargs/yargs');
const { hideBin } = require('yargs/helpers');
const archiver = require('archiver');
const { createArtResolver } = require('./art-resolver');
const { buildBootstrapPayload } = require('./package-snapshot');

async function sharePackage(cwd, argvInput) {
  const argv = yargs(hideBin(argvInput || process.argv))
    .option('server', {
      type: 'string',
      describe: 'FuncDraw share server base URL',
      default: process.env.FUNCDRAW_SHARE_SERVER || 'http://localhost:3040'
    })
    .option('restrict', {
      type: 'array',
      describe: 'Comma-separated (or repeatable) list of allowed Google emails'
    })
    .option('exp', {
      type: 'string',
      describe: 'FuncScript expression to evaluate (art refers to the loaded package)'
    })
    .help()
    .alias('help', 'h')
    .parseSync();

  const serverBase = normalizeServerBase(argv.server);
  const restrictList = normalizeRestrictList(argv.restrict);
  const expressionOverride = resolveExpressionOverride(argv);
  const pkg = readPackageManifest(cwd);
  const toolPkg = readOwnPackageManifest();

  const art = createArtResolver(cwd);
  if (!art) {
    throw new Error(`No art directory found in ${cwd}`);
  }

  const relativeArtPath = path.relative(cwd, art.watchPath) || art.watchPath;
  const resolver = expressionOverride
    ? wrapResolverWithExpression(art.resolver, expressionOverride)
    : art.resolver;
  const bootstrap = buildBootstrapPayload({
    resolver,
    sourceDescription: expressionOverride
      ? `art directory (${relativeArtPath}) via --exp`
      : `art directory (${relativeArtPath})`
  });

  const zipPath = await createPackageZip(cwd, pkg, { onLog: (line) => process.stderr.write(line + '\n') });
  const zipStats = fs.statSync(zipPath);
  const zipSha256 = await sha256File(zipPath);

  const meta = {
    package: {
      name: pkg.name || null,
      version: pkg.version || null
    },
    client: {
      tool: 'funcdraw-share',
      toolVersion: toolPkg.version || null,
      node: process.version,
      platform: process.platform,
      cwdBasename: path.basename(cwd)
    },
    source: {
      git: await tryGetGitInfo(cwd)
    },
    artifact: {
      filename: 'package.zip',
      contentType: 'application/zip',
      bytes: zipStats.size,
      sha256: zipSha256
    }
  };

  const playUrl = await uploadModel({
    serverBase,
    zipPath,
    meta,
    bootstrap,
    restrictList
  });

  try {
    fs.unlinkSync(zipPath);
  } catch {
    // ignore
  }

  process.stdout.write(playUrl + '\n');
}

module.exports = {
  sharePackage
};

function normalizeServerBase(value) {
  const raw = typeof value === 'string' ? value.trim() : '';
  if (!raw) {
    return 'http://localhost:8787';
  }
  const candidate = /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(raw) ? raw : `http://${raw}`;
  const url = new URL(candidate);
  return url.toString().replace(/\/+$/, '');
}

function normalizeRestrictList(value) {
  const collected = [];
  if (Array.isArray(value)) {
    for (const entry of value) {
      if (entry === undefined || entry === null) {
        continue;
      }
      const chunk = String(entry);
      for (const part of chunk.split(',')) {
        const trimmed = part.trim();
        if (trimmed) {
          collected.push(trimmed);
        }
      }
    }
  }
  const seen = new Set();
  const unique = [];
  for (const email of collected) {
    const key = email.toLowerCase();
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    unique.push(email);
  }
  return unique;
}

function readPackageManifest(cwd) {
  const manifestPath = path.resolve(cwd, 'package.json');
  if (!fs.existsSync(manifestPath)) {
    throw new Error(`Expected package.json at ${manifestPath}`);
  }
  const raw = fs.readFileSync(manifestPath, 'utf8');
  return JSON.parse(raw);
}

function readOwnPackageManifest() {
  try {
    const manifestPath = path.resolve(__dirname, '../package.json');
    return JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  } catch {
    return {};
  }
}

async function createPackageZip(cwd, pkg, { onLog } = {}) {
  const name = typeof pkg.name === 'string' && pkg.name.trim() ? pkg.name.trim() : 'funcdraw-package';
  const safeName = name.replace(/[^a-zA-Z0-9._-]+/g, '-');
  const tmpName = `${safeName}-${Date.now().toString(36)}.zip`;
  const outPath = path.join(os.tmpdir(), tmpName);

  onLog?.(picocolors.gray(`[funcdraw-share] Creating zip ${outPath}`));

  await new Promise((resolve, reject) => {
    const output = fs.createWriteStream(outPath);
    const archive = archiver('zip', { zlib: { level: 9 } });

    output.on('close', resolve);
    output.on('error', reject);
    archive.on('warning', (err) => {
      if (err.code === 'ENOENT') {
        onLog?.(picocolors.yellow(`[funcdraw-share] zip warning: ${err.message}`));
        return;
      }
      reject(err);
    });
    archive.on('error', reject);

    archive.pipe(output);

    archive.glob('**/*', {
      cwd,
      dot: true,
      ignore: [
        '**/node_modules/**',
        '**/.git/**',
        '**/.npmrc',
        '**/.DS_Store',
        '**/Thumbs.db',
        '**/.env',
        '**/.env.*',
        '**/dist/**',
        '**/build/**',
        '**/.next/**',
        '**/.turbo/**',
        '**/coverage/**',
        '**/artifacts/**',
        '**/*.log',
        '**/npm-debug.log*',
        '**/yarn-debug.log*',
        '**/yarn-error.log*'
      ]
    });

    archive.finalize();
  });

  return outPath;
}

async function sha256File(filePath) {
  const hash = crypto.createHash('sha256');
  await new Promise((resolve, reject) => {
    const stream = fs.createReadStream(filePath);
    stream.on('data', (chunk) => hash.update(chunk));
    stream.on('end', resolve);
    stream.on('error', reject);
  });
  return hash.digest('hex');
}

async function uploadModel({ serverBase, zipPath, meta, bootstrap, restrictList }) {
  const url = new URL('/api/models', serverBase);
  const zipBuffer = fs.readFileSync(zipPath);
  const form = new FormData();
  form.append('file', new Blob([zipBuffer], { type: 'application/zip' }), 'package.zip');
  form.append('meta', JSON.stringify(meta));
  form.append('bootstrap', JSON.stringify(bootstrap));
  if (Array.isArray(restrictList) && restrictList.length > 0) {
    form.append('restrict', restrictList.join(','));
  }

  const res = await fetch(url, { method: 'POST', body: form });
  if (!res.ok) {
    const text = await safeReadText(res);
    throw new Error(`Share upload failed (${res.status} ${res.statusText}): ${text || 'no body'}`);
  }
  const payload = await res.json();
  if (!payload || typeof payload.playUrl !== 'string' || payload.playUrl.trim().length === 0) {
    throw new Error('Share upload response missing playUrl');
  }
  return payload.playUrl.trim();
}

async function safeReadText(res) {
  try {
    return await res.text();
  } catch {
    return '';
  }
}

async function tryGetGitInfo(cwd) {
  const { execFile } = require('child_process');

  const run = (args) =>
    new Promise((resolve, reject) => {
      execFile('git', args, { cwd }, (err, stdout) => {
        if (err) {
          reject(err);
          return;
        }
        resolve(String(stdout || '').trim());
      });
    });

  try {
    const inside = await run(['rev-parse', '--is-inside-work-tree']);
    if (inside !== 'true') {
      return null;
    }
  } catch {
    return null;
  }

  let commit = null;
  let branch = null;
  let dirty = null;

  try {
    commit = await run(['rev-parse', 'HEAD']);
  } catch {
    // ignore
  }

  try {
    branch = await run(['rev-parse', '--abbrev-ref', 'HEAD']);
  } catch {
    // ignore
  }

  try {
    const status = await run(['status', '--porcelain']);
    dirty = status.length > 0;
  } catch {
    // ignore
  }

  return {
    commit,
    branch,
    dirty
  };
}

function resolveExpressionOverride(argv) {
  if (typeof argv.exp === 'string' && argv.exp.trim().length > 0) {
    return argv.exp.trim();
  }
  if (Array.isArray(argv._) && argv._.length > 0) {
    const candidate = argv._[0];
    if (typeof candidate === 'string') {
      const text = candidate.trim();
      if (text.length > 0) {
        return text;
      }
    }
  }
  return null;
}

function wrapResolverWithExpression(baseResolver, expressionText) {
  if (!baseResolver || typeof baseResolver.listChildren !== 'function') {
    return baseResolver;
  }
  const expression = expressionText;
  return {
    listChildren(pathSegments = []) {
      if (!Array.isArray(pathSegments) || pathSegments.length === 0) {
        return ['eval', 'art'];
      }
      if (pathSegments[0] === 'art') {
        return baseResolver.listChildren(pathSegments.slice(1));
      }
      return [];
    },
    getExpression(pathSegments = []) {
      if (pathSegments.length === 1 && pathSegments[0] === 'eval') {
        return {
          expression,
          language: 'funcscript'
        };
      }
      if (pathSegments[0] === 'art') {
        if (pathSegments.length === 1) {
          return null;
        }
        return baseResolver.getExpression(pathSegments.slice(1));
      }
      return null;
    },
    package(name) {
      return typeof baseResolver.package === 'function' ? baseResolver.package(name) : null;
    }
  };
}

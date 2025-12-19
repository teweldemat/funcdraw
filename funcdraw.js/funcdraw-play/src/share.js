'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const picocolors = require('picocolors');
const yargs = require('yargs/yargs');
const { hideBin } = require('yargs/helpers');
const archiver = require('archiver');
const open = require('open');
const readline = require('readline');
const { createArtResolver } = require('./art-resolver');
const { buildBootstrapPayload } = require('./package-snapshot');

const DEFAULT_SERVER = 'https://funcdraw.com';

async function sharePackage(cwd, argvInput, options = {}) {
  const argv = yargs(hideBin(argvInput || process.argv))
    .option('server', {
      type: 'string',
      describe: 'FuncDraw share server base URL',
      default: process.env.FUNCDRAW_SHARE_SERVER || DEFAULT_SERVER
    })
    .option('name', {
      type: 'string',
      describe: 'Public name (slug) for publishing: funcdraw.com/<handle>/<name>'
    })
    .option('slug', {
      type: 'string',
      describe: 'Alias for --name'
    })
    .option('handle', {
      type: 'string',
      describe: 'Override handle for publishing'
    })
    .option('token', {
      type: 'string',
      describe: 'Auth token for publishing'
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
  const authConfig = readAuthConfig();
  const authToken =
    argv.token ||
    process.env.FUNCDRAW_SHARE_TOKEN ||
    (authConfig && authConfig.serverBase === serverBase ? authConfig.token : null);
  let publishHandle = argv.handle || (authConfig && authConfig.serverBase === serverBase ? authConfig.handle : null);
  let publishName = argv.name || argv.slug || null;
  if (publishName) {
    publishName = slugifyName(publishName);
    if (!publishName) {
      throw new Error('Invalid --name for publishing.');
    }
  }
  const shouldPublish = Boolean(publishHandle || publishName);
  const toolName = options.toolName || 'funcdraw-share';

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
      tool: toolName,
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

  if (shouldPublish && !authToken) {
    throw new Error('Publishing requires login. Run `fd-share login` first.');
  }

  if (shouldPublish && !publishName) {
    publishName = await promptForName(pkg);
  }

  const playUrl = await uploadModel({
    serverBase,
    zipPath,
    meta,
    bootstrap,
    restrictList,
    authToken: shouldPublish ? authToken : null,
    publishHandle: shouldPublish ? publishHandle : null,
    publishName: shouldPublish ? publishName : null
  });

  try {
    fs.unlinkSync(zipPath);
  } catch {
    // ignore
  }

  process.stdout.write(playUrl + '\n');
}

async function login(argvInput) {
  const argv = yargs(hideBin(argvInput || process.argv))
    .option('server', {
      type: 'string',
      describe: 'FuncDraw share server base URL',
      default: process.env.FUNCDRAW_SHARE_SERVER || DEFAULT_SERVER
    })
    .option('open', {
      type: 'boolean',
      describe: 'Open the browser to complete login',
      default: true
    })
    .help()
    .alias('help', 'h')
    .parseSync();

  const serverBase = normalizeServerBase(argv.server);
  const startUrl = new URL('/api/cli/login/start', serverBase);
  const res = await fetch(startUrl, { method: 'POST' });
  if (!res.ok) {
    const text = await safeReadText(res);
    throw new Error(`Login start failed (${res.status} ${res.statusText}): ${text || 'no body'}`);
  }
  const payload = await res.json();
  const loginUrl = payload.loginUrl;
  const loginId = payload.loginId;
  if (!loginUrl || !loginId) {
    throw new Error('Login response missing loginUrl/loginId');
  }

  if (argv.open === false) {
    process.stdout.write(`${loginUrl}\n`);
  } else {
    await open(loginUrl);
    process.stdout.write(`Opened ${loginUrl}\n`);
  }

  const status = await pollLoginStatus(serverBase, loginId);
  if (status.status !== 'complete') {
    throw new Error(`Login did not complete (status: ${status.status})`);
  }

  writeAuthConfig({
    serverBase,
    token: status.token || null,
    handle: status.handle || null,
    email: status.email || null
  });

  process.stdout.write(`Logged in as ${status.handle || status.email}\n`);
}

module.exports = {
  sharePackage,
  login
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

function getAuthConfigPath() {
  const home = os.homedir();
  const base =
    process.env.XDG_CONFIG_HOME && process.env.XDG_CONFIG_HOME.trim()
      ? process.env.XDG_CONFIG_HOME.trim()
      : path.join(home, '.config');
  return path.join(base, 'funcdraw', 'share.json');
}

function readAuthConfig() {
  try {
    const configPath = getAuthConfigPath();
    if (!fs.existsSync(configPath)) {
      return null;
    }
    const payload = JSON.parse(fs.readFileSync(configPath, 'utf8'));
    if (!payload || typeof payload !== 'object') {
      return null;
    }
    if (!payload.serverBase || !payload.token) {
      return null;
    }
    return payload;
  } catch {
    return null;
  }
}

function writeAuthConfig(payload) {
  const configPath = getAuthConfigPath();
  fs.mkdirSync(path.dirname(configPath), { recursive: true });
  fs.writeFileSync(configPath, JSON.stringify(payload, null, 2) + '\n', 'utf8');
}

function slugifyName(name) {
  if (!name) {
    return null;
  }
  const text = String(name).trim().toLowerCase();
  if (!text) {
    return null;
  }
  const slug = text
    .replace(/[^a-z0-9-]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-+|-+$/g, '');
  if (!slug) {
    return null;
  }
  return slug;
}

async function promptForName(pkg) {
  if (!process.stdin.isTTY) {
    throw new Error('Missing --name for publishing (non-interactive shell)');
  }
  const suggestion = slugifyName(pkg && pkg.name ? pkg.name : '') || '';
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  const question = (prompt) =>
    new Promise((resolve) => {
      rl.question(prompt, (answer) => resolve(answer));
    });
  const answer = await question(`Choose a public name${suggestion ? ` (${suggestion})` : ''}: `);
  rl.close();
  const slug = slugifyName(answer || suggestion);
  if (!slug) {
    throw new Error('Invalid name for publishing.');
  }
  return slug;
}

async function pollLoginStatus(serverBase, loginId) {
  const statusUrl = new URL(`/api/cli/login/status/${loginId}`, serverBase);
  const timeoutMs = 5 * 60 * 1000;
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const res = await fetch(statusUrl);
    if (!res.ok) {
      const text = await safeReadText(res);
      throw new Error(`Login status failed (${res.status} ${res.statusText}): ${text || 'no body'}`);
    }
    const payload = await res.json();
    if (payload.status && payload.status !== 'pending') {
      return payload;
    }
    await new Promise((resolve) => setTimeout(resolve, 2000));
  }
  return { status: 'timeout' };
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

async function uploadModel({ serverBase, zipPath, meta, bootstrap, restrictList, authToken, publishHandle, publishName }) {
  const url = new URL('/api/models', serverBase);
  const zipBuffer = fs.readFileSync(zipPath);
  const form = new FormData();
  form.append('file', new Blob([zipBuffer], { type: 'application/zip' }), 'package.zip');
  form.append('meta', JSON.stringify(meta));
  form.append('bootstrap', JSON.stringify(bootstrap));
  if (Array.isArray(restrictList) && restrictList.length > 0) {
    form.append('restrict', restrictList.join(','));
  }
  if (publishHandle) {
    form.append('handle', publishHandle);
  }
  if (publishName) {
    form.append('name', publishName);
  }

  const headers = {};
  if (authToken) {
    headers.Authorization = `Bearer ${authToken}`;
  }
  const res = await fetch(url, { method: 'POST', body: form, headers });
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

const fs = require('fs');
const path = require('path');
const { createRequire } = require('module');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const webpack = require('webpack');

const publicPath = process.env.PUBLIC_PATH || '/';
const resolvePort = () => {
  const raw = process.env.FUNCPLAY_PORT || process.env.PORT;
  if (!raw) {
    return 5184;
  }
  const parsed = Number(raw);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 5184;
};
const devServerPort = resolvePort();

const projectConfigPath = process.env.FUNCPLAY_PROJECT_FILE
  ? path.resolve(process.env.FUNCPLAY_PROJECT_FILE)
  : null;
const projectRoot = process.env.FUNCPLAY_PROJECT_ROOT
  ? path.resolve(process.env.FUNCPLAY_PROJECT_ROOT)
  : null;

const moduleSpecifierPatterns = [
  /import\s*\(\s*(['"])([^'"\s]+)\1\s*\)/g,
  /require\s*\(\s*(['"])([^'"\s]+)\1\s*\)/g,
  /fdimport\s*\(\s*(['"])([^'"\s]+)\1\s*\)/g,
  /importModule\s*\(\s*(['"])([^'"\s]+)\1\s*\)/g
];

const EXPRESSION_FILE_TYPES = [
  { extension: '.fs', language: 'funcscript' },
  { extension: '.js', language: 'javascript' }
];

const ROOT_FOLDER_KEY = '__root__';
const RESERVED_ROOT_BASENAMES = new Set(['main', 'view']);
const IGNORED_DIRECTORIES = new Set([
  'node_modules',
  '.git',
  '.hg',
  '.svn',
  '.turbo',
  '.next',
  '.output',
  '.vercel'
]);

const isValidTabName = (name) => /^[A-Za-z_][A-Za-z0-9_]*$/.test(name);

const sanitizeFolderName = (segment) => {
  const trimmed = segment.trim().replace(/^[./]+|[./]+$/g, '');
  if (!trimmed) {
    return 'Folder';
  }
  const cleaned = trimmed.replace(/[^A-Za-z0-9 _-]+/g, ' ').replace(/\s+/g, ' ').trim();
  if (!cleaned) {
    return 'Folder';
  }
  return cleaned.replace(/\b\w/g, (char) => char.toUpperCase());
};

const sanitizeTabBaseName = (value) => value.replace(/[^A-Za-z0-9_]/g, '_');

const createTabNameAllocator = () => {
  const registry = new Map();
  let fallbackCounter = 1;
  return (folderKey, desired) => {
    const key = folderKey || ROOT_FOLDER_KEY;
    let set = registry.get(key);
    if (!set) {
      set = new Set();
      registry.set(key, set);
    }
    let base = sanitizeTabBaseName(desired || '');
    if (!base || !isValidTabName(base)) {
      base = `expression_${fallbackCounter++}`;
    }
    let candidate = base;
    let suffix = 2;
    while (set.has(candidate.toLowerCase()) || !isValidTabName(candidate)) {
      candidate = `${base}_${suffix}`;
      suffix += 1;
    }
    set.add(candidate.toLowerCase());
    return candidate;
  };
};

const readExpressionFilesFromDisk = (root) => {
  const files = [];
  const walk = (relative) => {
    const currentPath = relative ? path.join(root, relative) : root;
    let entries;
    try {
      entries = fs.readdirSync(currentPath, { withFileTypes: true });
    } catch {
      return;
    }
    entries
      .filter((entry) => entry && typeof entry.name === 'string')
      .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }))
      .forEach((entry) => {
        if (entry.name.startsWith('.')) {
          return;
        }
        const relativePath = relative ? path.join(relative, entry.name) : entry.name;
        if (entry.isSymbolicLink && entry.isSymbolicLink()) {
          return;
        }
        if (entry.isDirectory()) {
          if (IGNORED_DIRECTORIES.has(entry.name.toLowerCase())) {
            return;
          }
          walk(relativePath);
          return;
        }
        if (typeof entry.isFile !== 'function' || !entry.isFile()) {
          return;
        }
        const lowerName = entry.name.toLowerCase();
        const fileType = EXPRESSION_FILE_TYPES.find((type) => lowerName.endsWith(type.extension));
        if (!fileType) {
          return;
        }
        const absolutePath = path.join(root, relativePath);
        let content;
        try {
          content = fs.readFileSync(absolutePath, 'utf8');
        } catch {
          return;
        }
        const segments = relativePath
          .split(/[\\/]/)
          .map((segment) => segment.trim())
          .filter((segment) => segment.length > 0);
        if (segments.length === 0) {
          return;
        }
        const baseName = entry.name.slice(0, -fileType.extension.length);
        files.push({ segments, baseName, language: fileType.language, content });
      });
  };
  walk('');
  return files;
};

const createFolderBuilder = (name, pathKey) => ({
  name,
  pathKey,
  tabs: [],
  children: new Map()
});

const buildWorkspaceFromFilesystem = (root) => {
  const emptyResult = {
    workspace: null,
    hasWorkspaceRoots: false,
    foundMain: false,
    foundView: false
  };
  if (!root) {
    return emptyResult;
  }
  const files = readExpressionFilesFromDisk(root);
  if (!files || files.length === 0) {
    return emptyResult;
  }
  const rootBuilder = createFolderBuilder(null, '');
  const getTabName = createTabNameAllocator();
  const ensureFolder = (segments) => {
    if (!Array.isArray(segments) || segments.length === 0) {
      return rootBuilder;
    }
    let current = rootBuilder;
    let currentKey = '';
    for (const rawSegment of segments) {
      const normalized = rawSegment.trim();
      if (!normalized) {
        continue;
      }
      const key = normalized.toLowerCase();
      const nextKey = currentKey ? `${currentKey}/${key}` : key;
      let child = current.children.get(key);
      if (!child) {
        child = createFolderBuilder(sanitizeFolderName(normalized), nextKey);
        current.children.set(key, child);
      }
      current = child;
      currentKey = nextKey;
    }
    return current;
  };

  let graphics = null;
  let view = null;

  let foundMain = false;
  let foundView = false;

  files.forEach((file) => {
    const segments = file.segments;
    const filename = segments[segments.length - 1];
    const baseLower = file.baseName.trim().toLowerCase();
    if (segments.length === 1 && RESERVED_ROOT_BASENAMES.has(baseLower)) {
      if (baseLower === 'main') {
        graphics = { expression: file.content, language: file.language };
        foundMain = true;
      } else if (baseLower === 'view') {
        view = { expression: file.content, language: file.language };
        foundView = true;
      }
      return;
    }
    const folderSegments = segments.slice(0, -1);
    const builder = ensureFolder(folderSegments);
    const folderKey = builder.pathKey || ROOT_FOLDER_KEY;
    const tabName = getTabName(folderKey, file.baseName);
    builder.tabs.push({ name: tabName, expression: file.content, language: file.language });
  });

  if (!graphics || !view) {
    return {
      workspace: null,
      hasWorkspaceRoots: foundMain || foundView,
      foundMain,
      foundView
    };
  }

  const sortTabs = (list) =>
    [...list].sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }));

  const convertFolders = (builder) => {
    const folderEntries = [];
    for (const child of builder.children.values()) {
      const nested = convertFolders(child);
      const tabs = sortTabs(child.tabs);
      if (tabs.length === 0 && nested.length === 0) {
        continue;
      }
      folderEntries.push({ name: child.name, tabs, folders: nested });
    }
    return folderEntries.sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }));
  };

  return {
    workspace: {
      graphics,
      view,
      tabs: sortTabs(rootBuilder.tabs),
      folders: convertFolders(rootBuilder)
    },
    hasWorkspaceRoots: true,
    foundMain,
    foundView
  };
};

const sanitizeModuleValue = (value, seen = new Set()) => {
  if (value === null || value === undefined) {
    return null;
  }
  const valueType = typeof value;
  if (valueType === 'string' || valueType === 'number' || valueType === 'boolean') {
    return value;
  }
  if (valueType === 'bigint') {
    return Number.isSafeInteger(Number(value)) ? Number(value) : value.toString();
  }
  if (Array.isArray(value)) {
    if (seen.has(value)) {
      return null;
    }
    seen.add(value);
    const result = value
      .map((entry) => sanitizeModuleValue(entry, seen))
      .filter((entry) => entry !== undefined);
    seen.delete(value);
    return result;
  }
  if (valueType === 'object') {
    if (seen.has(value)) {
      return null;
    }
    seen.add(value);
    const entries = {};
    for (const [key, entry] of Object.entries(value)) {
      const sanitized = sanitizeModuleValue(entry, seen);
      if (sanitized !== undefined) {
        entries[key] = sanitized;
      }
    }
    seen.delete(value);
    return entries;
  }
  return undefined;
};

const collectModuleSpecifiers = (projectConfig) => {
  const specifiers = new Set();
  if (!projectConfig || typeof projectConfig !== 'object') {
    return specifiers;
  }
  const scanExpression = (expression) => {
    if (typeof expression !== 'string') {
      return;
    }
    for (const pattern of moduleSpecifierPatterns) {
      pattern.lastIndex = 0;
      let match;
      while ((match = pattern.exec(expression))) {
        const specifier = match[2];
        if (specifier) {
          specifiers.add(specifier);
        }
      }
    }
  };

  const visitWorkspaceExpression = (candidate) => {
    if (!candidate || typeof candidate !== 'object') {
      return;
    }
    if (typeof candidate.expression === 'string') {
      scanExpression(candidate.expression);
    }
  };

  if (Array.isArray(projectConfig.catalogEntries)) {
    for (const entry of projectConfig.catalogEntries) {
      if (entry && typeof entry === 'object') {
        scanExpression(entry.catalogExpression);
        if (entry.workspace && typeof entry.workspace === 'object') {
          visitWorkspaceExpression(entry.workspace.graphics);
          visitWorkspaceExpression(entry.workspace.view);
        }
      }
    }
  }

  if (projectConfig.workspace && typeof projectConfig.workspace === 'object') {
    visitWorkspaceExpression(projectConfig.workspace.graphics);
    visitWorkspaceExpression(projectConfig.workspace.view);
    if (Array.isArray(projectConfig.workspace.tabs)) {
      for (const tab of projectConfig.workspace.tabs) {
        if (tab && typeof tab === 'object' && typeof tab.expression === 'string') {
          scanExpression(tab.expression);
        }
      }
    }
  }

  return specifiers;
};

const readProjectConfig = () => {
  if (!projectConfigPath) {
    return { literal: 'null', modulesLiteral: 'null' };
  }
  try {
    const raw = fs.readFileSync(projectConfigPath, 'utf8');
    const parsed = JSON.parse(raw);
    if (projectRoot) {
      const workspaceResult = buildWorkspaceFromFilesystem(projectRoot);
      if (workspaceResult?.workspace) {
        parsed.workspace = workspaceResult.workspace;
      } else if (!parsed.workspace && workspaceResult?.hasWorkspaceRoots) {
        console.warn('FuncDraw project is missing workspace files (main.fs/js and view.fs/js).');
      }
    }
    const specifiers = collectModuleSpecifiers(parsed);
    const modules = {};
    if (specifiers.size > 0 && projectRoot) {
      try {
        const resolver = createRequire(path.join(projectRoot, 'package.json'));
        for (const specifier of specifiers) {
          try {
            const resolved = resolver(specifier);
            const sanitized = sanitizeModuleValue(resolved);
            if (sanitized !== undefined) {
              modules[specifier] = sanitized;
            }
          } catch (err) {
            console.warn(`Failed to load module "${specifier}" from project: ${err?.message ?? err}`);
          }
        }
      } catch (err) {
        console.warn(`Failed to create project resolver: ${err?.message ?? err}`);
      }
    }
    const configLiteral = JSON.stringify(JSON.stringify(parsed));
    const modulesLiteral = Object.keys(modules).length > 0 ? JSON.stringify(JSON.stringify(modules)) : 'null';
    return { literal: configLiteral, modulesLiteral };
  } catch (err) {
    console.warn(`Failed to read FuncDraw project config: ${err?.message ?? err}`);
    return { literal: 'null', modulesLiteral: 'null' };
  }
};

const projectPayload = readProjectConfig();

module.exports = {
  entry: path.resolve(__dirname, 'src/main.tsx'),
  output: {
    filename: 'assets/[name].[contenthash].js',
    path: path.resolve(__dirname, 'dist'),
    publicPath,
    clean: true
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
    alias: {
      react$: require.resolve('react'),
      'react-dom$': require.resolve('react-dom'),
      'react/jsx-runtime$': require.resolve('react/jsx-runtime'),
      'react/jsx-dev-runtime$': require.resolve('react/jsx-dev-runtime')
    }
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: {
          loader: 'ts-loader',
          options: {
            transpileOnly: false,
            compilerOptions: {
              noEmit: false
            }
          }
        },
        exclude: /node_modules/
      },
      {
        test: /\.fs$/i,
        type: 'asset/source'
      },
      {
        test: /\.js$/i,
        include: path.resolve(__dirname, 'src/examples'),
        type: 'asset/source'
      },
      {
        test: /\.css$/i,
        use: ['style-loader', 'css-loader']
      }
    ]
  },
  plugins: [
    new webpack.DefinePlugin({
      __FUNC_PROJECT_CONFIG__: projectPayload.literal,
      __FUNC_PROJECT_MODULES__: projectPayload.modulesLiteral
    }),
    new HtmlWebpackPlugin({
      template: path.resolve(__dirname, 'index.html'),
      favicon: path.resolve(__dirname, 'favicon.svg')
    })
  ],
  devtool: 'source-map',
  devServer: {
    historyApiFallback: {
      rewrites: [
        {
          from: /^\/docs\/([^/?]+)\.md$/,
          to: (context) => `/docs/${context.match[1]}.html`
        },
        {
          from: /^\/docs\/.*$/,
          to: (context) => context.parsedUrl.pathname
        }
      ]
    },
    hot: true,
    port: devServerPort,
    static: [
      {
        directory: path.resolve(__dirname, '..', 'docs', 'site'),
        publicPath: '/docs'
      }
    ]
  }
};

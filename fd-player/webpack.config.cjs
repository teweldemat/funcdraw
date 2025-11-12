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

const importPattern = /import\s*\(\s*(['"])([^'"\s]+)\1\s*\)/g;

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
    let match;
    while ((match = importPattern.exec(expression))) {
      const specifier = match[2];
      if (specifier) {
        specifiers.add(specifier);
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

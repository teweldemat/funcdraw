'use strict';

const fs = require('fs');
const path = require('path');
const picocolors = require('picocolors');
const yargs = require('yargs/yargs');
const { hideBin } = require('yargs/helpers');
const { createExpression: createFuncDrawExpression } = require('@funcdraw/core');
const { loadUserConfig } = require('./config');
const { startServer } = require('./server');

async function startPlayer(cwd, argvInput) {
  const argv = yargs(hideBin(argvInput || process.argv))
    .option('port', {
      alias: 'p',
      type: 'number',
      describe: 'Preferred port for the preview server'
    })
    .option('host', {
      type: 'string',
      describe: 'Host interface',
      default: '127.0.0.1'
    })
    .option('open', {
      type: 'boolean',
      describe: 'Open the default browser automatically',
      default: true
    })
    .option('debug', {
      type: 'boolean',
      describe: 'Print evaluated scene payload (including warnings) to the console',
      default: false
    })
    .option('dump', {
      type: 'boolean',
      describe: 'Evaluate once, dump the scene payload to the console, and exit (no server)',
      default: false
    })
    .option('svg', {
      type: 'boolean',
      describe: 'Include SVG output when running in dump mode',
      default: false
    })
    .option('t', {
      type: 'number',
      describe: 'Initial time hook value (seconds)'
    })
    .option('canvas', {
      type: 'array',
      describe: 'Initial canvas size in pixels (width height)'
    })
    .option('exp', {
      type: 'string',
      describe: 'FuncScript expression to evaluate (art refers to the loaded package)'
    })
    .help()
    .alias('help', 'h')
    .parseSync();

  const debugEnabled = Boolean(argv.debug || argv.dump);
  const expressionOverride = typeof argv.exp === 'string' ? argv.exp : null;
  let config = await loadUserConfig(cwd, { expression: expressionOverride });
  if (config.configPath) {
    console.log(
      picocolors.gray('Using config'),
      picocolors.white(path.relative(cwd, config.configPath))
    );
  } else if (config.sourceDescription) {
    console.log(picocolors.gray('Using'), picocolors.white(config.sourceDescription));
  } else {
    console.log(picocolors.gray('Using inline sample expression (art/ directory not found)'));
  }

  let currentExpression = buildExpression(config);
  const timelineState = {
    value: 0
  };
  const canvasState = {
    width: 40,
    height: 30
  };
  setTimelineValue(argv.t);
  if (Array.isArray(argv.canvas) && argv.canvas.length > 0) {
    setCanvasSize({
      width: argv.canvas[0],
      height: argv.canvas.length > 1 ? argv.canvas[1] : undefined
    });
  }
  function setTimelineValue(input) {
    if (input === undefined || input === null) {
      return;
    }
    const parsed = parseFloatValue(input);
    if (parsed !== null) {
      timelineState.value = parsed;
    }
  }
  function resetTimeline() {
    timelineState.value = 0;
  }

  function setCanvasSize({ width, height }) {
    const parsedWidth = parseFloatValue(width);
    const parsedHeight = parseFloatValue(height);
    if (parsedWidth !== null) {
      canvasState.width = parsedWidth;
    }
    if (parsedHeight !== null) {
      canvasState.height = parsedHeight;
    }
  }

  const evaluateScene = async ({ includeSvg, requestId, query } = {}) => {
    if (query && Object.prototype.hasOwnProperty.call(query, 'time')) {
      setTimelineValue(query.time);
    }
    if (query && (Object.prototype.hasOwnProperty.call(query, 'canvasWidth') || Object.prototype.hasOwnProperty.call(query, 'canvasHeight'))) {
      setCanvasSize({
        width: query.canvasWidth,
        height: query.canvasHeight
      });
    }
    const outputs = includeSvg ? ['raw', 'svg'] : ['raw'];
    const evalId = requestId || `eval-${Date.now().toString(36)}`;
    const outputLabel = outputs.join(', ');
    const start = Date.now();
    console.log(picocolors.gray(`[funcdraw-play] [${evalId}] Evaluating scene (outputs: ${outputLabel})`));
    try {
      const result = await currentExpression.evaluate({
        output: outputs,
        valueHooks: {
          t: () => timelineState.value,
          canvas: () => ({
            size: {
              width: canvasState.width,
              height: canvasState.height
            }
          })
        }
      });
      if (!includeSvg) {
        delete result.svg;
      }
      const warningsCount = Array.isArray(result.warnings) ? result.warnings.length : 0;
      const viewText = Array.isArray(result.view) ? result.view.join('×') : 'unknown';
      console.log(
        picocolors.gray(
          `[funcdraw-play] [${evalId}] Evaluation finished in ${Date.now() - start}ms (view: ${viewText}, warnings: ${warningsCount})`
        )
      );
      if (debugEnabled) {
        console.log(picocolors.yellow(`[funcdraw-play] [${evalId}] Scene payload:`));
        console.dir(result, { depth: null, colors: true });
      }
      result.timeline = { t: timelineState.value };
      result.canvas = { ...canvasState };
      return result;
    } catch (error) {
      console.error(picocolors.red(`[funcdraw-play] [${evalId}] Evaluation failed:`), error);
      throw error;
    }
  };

  if (argv.dump) {
    console.log(picocolors.cyan('FuncDraw Play dump mode'));
    try {
      await evaluateScene({ includeSvg: Boolean(argv.svg), requestId: 'dump-mode' });
      console.log(picocolors.green('Scene evaluation completed (dump mode).'));
      return;
    } catch (error) {
      console.error(picocolors.red('Dump evaluation failed:'), error.message || error);
      process.exitCode = 1;
      return;
    }
  }

  const server = await startServer({
    evaluateScene,
    host: argv.host,
    port: argv.port,
    openBrowser: argv.open
  });

  const reloadConfig = async () => {
    try {
      const updated = await loadUserConfig(cwd, { expression: expressionOverride });
      config = updated;
      currentExpression = buildExpression(config);
      resetTimeline();
      console.log(picocolors.green('FuncDraw scene reloaded'));
      const nextWatchPaths = Array.isArray(config.watchPaths) ? config.watchPaths : [];
      if (!pathsEqual(nextWatchPaths, watchedPaths)) {
        closeWatcher();
        watchedPaths = nextWatchPaths;
        closeWatcher = watchPaths(watchedPaths, reloadConfig);
      }
      server.broadcastReload();
    } catch (error) {
      console.error(picocolors.red('Failed to reload scene:'), error.message);
    }
  };

  let watchedPaths = Array.isArray(config.watchPaths) ? config.watchPaths : [];
  let closeWatcher = watchPaths(watchedPaths, reloadConfig);

  const shutdown = () => {
    closeWatcher();
    server.close();
    process.exit(0);
  };

  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);
}

function buildExpression(config) {
  return createFuncDrawExpression(config.resolver, config.options);
}

function parseFloatValue(value) {
  if (Array.isArray(value)) {
    return parseFloatValue(value[value.length - 1]);
  }
  if (value === undefined || value === null) {
    return null;
  }
  const num = Number(value);
  return Number.isFinite(num) ? num : null;
}

function watchPaths(paths, onChange) {
  if (!paths || paths.length === 0) {
    console.log(picocolors.gray('[funcdraw-play] No paths to watch for changes'));
    return () => {};
  }
  const watchers = [];
  for (const target of paths) {
    if (!target) {
      continue;
    }
    try {
      const stat = fs.existsSync(target) ? fs.statSync(target) : null;
      const options =
        stat && stat.isDirectory() && (process.platform === 'darwin' || process.platform === 'win32')
          ? { recursive: true }
          : undefined;
      console.log(picocolors.gray(`[funcdraw-play] Watching for changes: ${target}`));
      let timer = null;
      const watcher = fs.watch(target, options, () => {
        console.log(picocolors.gray(`[funcdraw-play] Change detected under: ${target}`));
        clearTimeout(timer);
        timer = setTimeout(onChange, 150);
      });
      watchers.push(() => {
        clearTimeout(timer);
        watcher.close();
      });
    } catch (error) {
      console.warn('[funcdraw-play] Unable to watch', target, error.message);
    }
  }
  return () => {
    for (const close of watchers) {
      close();
    }
  };
}

function pathsEqual(a, b) {
  const normalize = (arr) =>
    (arr || [])
      .filter(Boolean)
      .map((p) => path.resolve(p))
      .sort();
  const first = normalize(a);
  const second = normalize(b);
  if (first.length !== second.length) {
    return false;
  }
  for (let i = 0; i < first.length; i += 1) {
    if (first[i] !== second[i]) {
      return false;
    }
  }
  return true;
}

module.exports = {
  startPlayer
};

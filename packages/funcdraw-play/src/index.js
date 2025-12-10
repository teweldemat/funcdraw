'use strict';

const fs = require('fs');
const path = require('path');
const picocolors = require('picocolors');
const yargs = require('yargs/yargs');
const { hideBin } = require('yargs/helpers');
const funcscript = require('@tewelde/funcscript');
const { FuncScriptParser, DefaultFsDataProvider } = funcscript;
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
    .option('test', {
      type: 'boolean',
      describe: 'Run FuncScript package tests and exit (no server)'
    })
    .option('svg', {
      type: 'string',
      describe: 'Include SVG output when running in dump mode; optionally pass a file path to write it'
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
    .option('trace', {
      type: 'array',
      describe: 'Emit FuncScript package trace info; optionally pass "step-into" and an optional filter'
    })
    .option('trace-file', {
      type: 'string',
      describe: 'Optional file path to write FuncScript trace output (JSON)'
    })
    .help()
    .alias('help', 'h')
    .parseSync();

  const traceOptions = normalizeTraceOption(argv.trace);
  const traceFile = typeof argv['trace-file'] === 'string' ? argv['trace-file'] : null;
  const traceOutputPath = traceFile ? path.resolve(cwd, traceFile) : null;
  const svgOption = normalizeSvgOption(argv.svg, cwd);
  const traceRequested = Boolean(traceOptions && traceOptions.enabled) || Boolean(traceOutputPath);
  const dumpMode = Boolean(argv.dump);
  const traceOnlyMode = traceRequested && !dumpMode;
  const debugEnabled = !traceOnlyMode && Boolean(argv.debug || dumpMode);
  const dumpLoggingEnabled = dumpMode;
  const traceEnabled = traceRequested;
  const expressionOverride = resolveExpressionOverride(argv);
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

  if (argv.test) {
    const exitCode = await runPackageTests(config);
    if (exitCode !== 0) {
      process.exitCode = exitCode;
    }
    return;
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
      const dumpLogger = dumpLoggingEnabled ? createDumpLogger(evalId) : null;
      const result = await currentExpression.evaluate({
        output: outputs,
        trace: traceOptions || traceEnabled,
        dumpLogger,
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
        printSceneSummary(result, evalId);
      }
      result.timeline = { t: timelineState.value };
      result.canvas = { ...canvasState };
      if (traceEnabled && traceOutputPath) {
        writeTraceToFile(result && result.trace, traceOutputPath, cwd);
      }
      return result;
    } catch (error) {
      console.error(picocolors.red(`[funcdraw-play] [${evalId}] Evaluation failed:`), error);
      throw error;
    }
  };

  if (argv.dump) {
    console.log(picocolors.cyan('FuncDraw Play dump mode'));
    try {
      const dumpResult = await evaluateScene({
        includeSvg: svgOption.enabled,
        requestId: 'dump-mode'
      });
      if (svgOption.outputPath) {
        writeSvgToFile(dumpResult.svg, svgOption.outputPath, cwd);
      }
      if (traceEnabled) {
        printTraceEntries(dumpResult && dumpResult.trace);
      }
      console.log(picocolors.green('Scene evaluation completed (dump mode).'));
      return;
    } catch (error) {
      console.error(picocolors.red('Dump evaluation failed:'), error.message || error);
      process.exitCode = 1;
      return;
    }
  }

  if (traceOnlyMode) {
    console.log(picocolors.cyan('FuncDraw Play trace mode'));
    try {
      const traceResult = await evaluateScene({ includeSvg: false, requestId: 'trace-mode' });
      printTraceEntries(traceResult && traceResult.trace);
      console.log(picocolors.green('FuncScript trace completed.'));
      return;
    } catch (error) {
      console.error(picocolors.red('Trace run failed:'), error.message || error);
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

function normalizeSvgOption(raw, cwd) {
  if (raw === undefined || raw === null) {
    return { enabled: false, outputPath: null };
  }
  const outputPath = raw === '' ? null : path.resolve(cwd, String(raw));
  return { enabled: true, outputPath };
}

async function runPackageTests(config) {
  if (!config || !config.resolver) {
    console.error(picocolors.red('No FuncScript package resolver available for testing.'));
    return 1;
  }

  console.log(picocolors.cyan('FuncDraw Play test mode'));
  const start = Date.now();
  try {
    const preflight = runPreflightParseChecks(config.resolver);
    if (preflight.failures.length > 0) {
      console.error(
        picocolors.red(
          `Failed to parse ${preflight.failures.length} expression(s) before running tests:`
        )
      );
      preflight.failures.forEach((failure) => {
        console.error(
          picocolors.yellow(`  ${failure.kind} ${failure.path}: ${failure.message}`)
        );
      });
      return 1;
    }

    const result = funcscript.testPackage(config.resolver);
    const summary = normalizeTestSummary(result && result.summary);
    const failures = collectTestFailures(result && result.tests);

    if (summary.scripts === 0) {
      console.log(picocolors.yellow('No FuncScript test pairs found in the loaded package.'));
      return 0;
    }

    if (summary.failed === 0) {
      console.log(
        picocolors.green(
          `All ${summary.cases} case(s) passed across ${summary.scripts} script(s) in ${Date.now() - start}ms.`
        )
      );
      return 0;
    }

    console.error(
      picocolors.red(
        `FuncScript package tests failed (${summary.failed}/${summary.cases} case(s) across ${summary.scripts} script(s)).`
      )
    );
    const maxFailuresToShow = 10;
    failures.slice(0, maxFailuresToShow).forEach((failure) => {
      console.error(formatFailureMessage(failure));
      if (failure.error && failure.error.stack) {
        console.error(picocolors.gray(indentMultiline(failure.error.stack, 4)));
      }
    });
    if (failures.length > maxFailuresToShow) {
      console.error(
        picocolors.gray(
          `...and ${failures.length - maxFailuresToShow} more failure(s) not shown (limit ${maxFailuresToShow}).`
        )
      );
    }
    return 1;
  } catch (error) {
    console.error(picocolors.red('Failed to run FuncScript package tests:'), error.message || error);
    if (error && error.stack) {
      console.error(picocolors.gray(indentMultiline(error.stack, 2)));
    }
    return 1;
  }
}

function normalizeTestSummary(summary) {
  if (!summary || typeof summary !== 'object') {
    return { scripts: 0, suites: 0, cases: 0, passed: 0, failed: 0 };
  }
  return {
    scripts: Number(summary.scripts) || 0,
    suites: Number(summary.suites) || 0,
    cases: Number(summary.cases) || 0,
    passed: Number(summary.passed) || 0,
    failed: Number(summary.failed) || 0
  };
}

function runPreflightParseChecks(resolver) {
  const failures = [];
  const pairs = collectTestPairs(resolver);
  for (const pair of pairs) {
    const scriptPath = formatResolverPath(pair.folderPath.concat([pair.scriptName]));
    const testPath = formatResolverPath(pair.folderPath.concat([pair.testName]));
    const scriptParse = tryParseRawExpression(resolver, pair.folderPath, pair.scriptName);
    if (!scriptParse.ok) {
      failures.push({
        kind: 'expression',
        path: scriptPath,
        message: scriptParse.message
      });
    }
    const testParse = tryParseRawExpression(resolver, pair.folderPath, pair.testName);
    if (!testParse.ok) {
      failures.push({
        kind: 'test',
        path: testPath,
        message: testParse.message
      });
    }
  }
  return { failures };
}

function tryParseRawExpression(resolver, folderPath, name) {
  try {
    const expressionNode = resolver.getExpression(folderPath.concat([name]));
    if (!expressionNode || typeof expressionNode.expression !== 'string') {
      return { ok: false, message: 'Expression not found in resolver' };
    }
    if (
      expressionNode.language &&
      expressionNode.language !== 'funcscript' &&
      expressionNode.language !== 'fs' &&
      expressionNode.language !== 'fsx'
    ) {
      return { ok: true };
    }
    const expr = expressionNode.expression;
    const provider = new DefaultFsDataProvider();
    const errors = [];
    FuncScriptParser.parse(provider, expr, errors);
    if (errors.length > 0) {
      const first = errors[0];
      const loc = typeof first.Loc === 'number' ? ` at ${first.Loc}` : '';
      return { ok: false, message: `${first.Message || 'Parse error'}${loc}` };
    }
    return { ok: true };
  } catch (error) {
    return { ok: false, message: error && error.message ? error.message : String(error) };
  }
}

function collectTestPairs(resolver, pathSegments = [], accumulator = []) {
  const children = resolver.listChildren(pathSegments) || [];
  if (!Array.isArray(children) || children.length === 0) {
    return accumulator;
  }
  const nameMap = new Map();
  for (const entry of children) {
    const name = extractResolverName(entry);
    if (!name) {
      continue;
    }
    const lower = name.toLowerCase();
    if (!nameMap.has(lower)) {
      nameMap.set(lower, name);
    }
  }

  for (const [lower, actual] of nameMap.entries()) {
    if (!lower.endsWith('.test')) {
      continue;
    }
    const base = lower.slice(0, -5);
    if (nameMap.has(base)) {
      accumulator.push({
        folderPath: pathSegments.slice(),
        scriptName: nameMap.get(base),
        testName: actual
      });
    }
  }

  for (const actual of nameMap.values()) {
    const childPath = pathSegments.concat([actual]);
    const childEntries = resolver.listChildren(childPath) || [];
    if (Array.isArray(childEntries) && childEntries.length > 0) {
      collectTestPairs(resolver, childPath, accumulator);
    }
  }
  return accumulator;
}

function extractResolverName(entry) {
  if (entry == null) {
    return null;
  }
  if (typeof entry === 'string') {
    const trimmed = entry.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  if (typeof entry === 'object' && typeof entry.name === 'string') {
    const trimmed = entry.name.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  return null;
}

function formatResolverPath(segments = []) {
  if (!Array.isArray(segments) || segments.length === 0) {
    return '<root>';
  }
  return segments.join('/');
}

function collectTestFailures(tests) {
  const entries = Array.isArray(tests) ? tests : [];
  const failures = [];
  for (const entry of entries) {
    const suites = (entry && entry.result && Array.isArray(entry.result.suites)) ? entry.result.suites : [];
    for (const suite of suites) {
      const cases = Array.isArray(suite.cases) ? suite.cases : [];
      for (const caseResult of cases) {
        if (caseResult && caseResult.passed === false) {
          failures.push({
            scriptPath: entry ? entry.path : null,
            testPath: entry ? entry.testPath : null,
            suiteName: suite.name || suite.id,
            caseIndex: caseResult.index,
            error: caseResult.error
          });
        }
      }
    }
  }
  return failures;
}

function formatFailureMessage(failure) {
  const parts = [];
  if (failure.scriptPath) {
    parts.push(failure.scriptPath);
  }
  if (failure.testPath && failure.testPath !== failure.scriptPath) {
    parts.push(`test: ${failure.testPath}`);
  }
  if (failure.suiteName) {
    parts.push(`suite: ${failure.suiteName}`);
  }
  if (failure.caseIndex !== undefined && failure.caseIndex !== null) {
    parts.push(`case #${failure.caseIndex}`);
  }
  const location = parts.length > 0 ? parts.join(' · ') : 'Test';
  const message = formatCaseErrorMessage(failure.error);
  return picocolors.red(`- ${location} failed${message ? `: ${message}` : ''}`);
}

function formatCaseErrorMessage(error) {
  if (!error) {
    return '';
  }
  if (error.fsError) {
    const type = error.fsError.errorType || 'Error';
    const msg = error.fsError.errorMessage || '';
    const data = error.fsError.errorData;
    if (data !== undefined) {
      return `${type}: ${msg || 'FuncScript error'} (data: ${safeStringify(data)})`;
    }
    return `${type}: ${msg || 'FuncScript error'}`;
  }
  if (typeof error.message === 'string' && error.message.trim()) {
    return error.message.trim();
  }
  if (error.reason) {
    return String(error.reason);
  }
  if (typeof error === 'string') {
    return error;
  }
  return safeStringify(error);
}

function safeStringify(value) {
  try {
    if (typeof value === 'string') {
      return value;
    }
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

function indentMultiline(text, spaces = 2) {
  if (!text) {
    return '';
  }
  const padding = ' '.repeat(spaces);
  return String(text)
    .split('\n')
    .map((line) => padding + line)
    .join('\n');
}

function writeSvgToFile(svg, targetPath, cwd) {
  if (typeof svg !== 'string') {
    throw new Error('expected SVG output when --svg is provided');
  }
  fs.mkdirSync(path.dirname(targetPath), { recursive: true });
  fs.writeFileSync(targetPath, svg, 'utf8');
  const relative = path.relative(cwd, targetPath);
  const displayPath = relative && relative !== '' ? relative : targetPath;
  console.log(picocolors.gray(`[funcdraw-play] SVG written to ${displayPath}`));
}

function writeTraceToFile(entries, targetPath, cwd) {
  const payload = JSON.stringify(entries || [], null, 2);
  fs.mkdirSync(path.dirname(targetPath), { recursive: true });
  fs.writeFileSync(targetPath, payload, 'utf8');
  const relative = path.relative(cwd, targetPath);
  const displayPath = relative && relative !== '' ? relative : targetPath;
  console.log(picocolors.gray(`[funcdraw-play] Trace written to ${displayPath}`));
}

function printTraceEntries(entries) {
  if (!entries || entries.length === 0) {
    console.log(picocolors.gray('[funcdraw-play] No FuncScript trace entries recorded.'));
    return;
  }
  const nodeCount = countTraceNodes(entries);
  console.log(
    picocolors.cyan(
      `[funcdraw-play] FuncScript trace (${nodeCount} entr${nodeCount === 1 ? 'y' : 'ies'})`
    )
  );
  for (const entry of entries) {
    printTraceNode(entry, 0);
  }
}

function printTraceNode(entry, depth) {
  if (!entry) {
    return;
  }
  const indent = '  '.repeat(depth);
  const pathText = entry.path ? entry.path : '(root)';
  const location = formatTraceLocation(entry);
  const snippet = cleanSnippet(entry.snippet);
  const resultText = formatTraceResult(entry);
  console.log(picocolors.gray(`${indent}- ${pathText}${location ? ` ${location}` : ''}${snippet ? ` ${snippet}` : ''}`));
  if (resultText) {
    console.log(picocolors.gray(`${indent}  value: ${resultText}`));
  }
  if (Array.isArray(entry.children)) {
    entry.children.forEach((child) => printTraceNode(child, depth + 1));
  }
}

function countTraceNodes(entries) {
  let count = 0;
  for (const entry of entries || []) {
    count += 1;
    if (Array.isArray(entry.children)) {
      count += countTraceNodes(entry.children);
    }
  }
  return count;
}

function formatTraceLocation(entry) {
  if (!entry) {
    return '';
  }
  const startLine = Number(entry.startLine);
  const startColumn = Number(entry.startColumn);
  const endLine = Number(entry.endLine);
  const endColumn = Number(entry.endColumn);
  if (!Number.isFinite(startLine) || !Number.isFinite(startColumn)) {
    return '';
  }
  if (Number.isFinite(endLine) && Number.isFinite(endColumn)) {
    return `@${startLine}:${startColumn}-${endLine}:${endColumn}`;
  }
  return `@${startLine}:${startColumn}`;
}

function cleanSnippet(snippet) {
  if (!snippet) {
    return '';
  }
  const compact = String(snippet).replace(/\s+/g, ' ').trim();
  if (!compact) {
    return '';
  }
  const maxLength = 160;
  return compact.length > maxLength ? `${compact.slice(0, maxLength - 3)}...` : compact;
}

function formatTraceResult(entry) {
  if (!entry || !entry.resultKind) {
    return '';
  }
  if (entry.resultPreview !== undefined && entry.resultPreview !== null) {
    return entry.resultPreview;
  }
  if (entry.resultKind === 'atomic') {
    return '(atomic)';
  }
  if (entry.resultKind === 'error') {
    return 'error';
  }
  const placeholders = {
    function: '<function>',
    list: '<list>',
    kvc: '<kvc>',
    object: '<object>'
  };
  return placeholders[entry.resultKind] || `<${entry.resultKind}>`;
}

function createDumpLogger(evalId) {
  const prefix = `[funcdraw-play] [${evalId}] dump`;
  return (message) => {
    if (!message) {
      return;
    }
    console.log(picocolors.gray(`${prefix} ${message}`));
  };
}

function normalizeTraceOption(raw) {
  if (raw === undefined || raw === null) {
    return null;
  }
  if (Array.isArray(raw)) {
    const cleaned = raw.filter(
      (item) => item !== undefined && item !== null && String(item).trim() !== ''
    );
    if (cleaned.length === 0) {
      // yargs sets `[undefined]` when the option is not provided; treat that as "no trace".
      return raw.length === 0 ? { enabled: true, stepInto: false, filter: null } : null;
    }
    if (raw.length === 0) {
      return { enabled: true, stepInto: false, filter: null };
    }
    const first = String(cleaned[0] || '').toLowerCase();
    const stepInto = first === 'step-into';
    const filter = stepInto && cleaned.length > 1 ? String(cleaned[1]) : null;
    return { enabled: true, stepInto, filter };
  }
  if (typeof raw === 'boolean') {
    return { enabled: raw, stepInto: false, filter: null };
  }
  if (typeof raw === 'string') {
    const first = raw.toLowerCase();
    const stepInto = first === 'step-into';
    return { enabled: true, stepInto, filter: null };
  }
  if (typeof raw === 'object') {
    return {
      enabled: raw.enabled !== false,
      stepInto: Boolean(raw.stepInto),
      filter: raw.filter != null ? String(raw.filter) : null
    };
  }
  return { enabled: true, stepInto: false, filter: null };
}

function printSceneSummary(result, evalId) {
  console.log(picocolors.yellow(`[funcdraw-play] [${evalId}] Scene payload (graphics summary):`));
  const raw = result && result.raw;
  if (!raw || !raw.graphics) {
    console.log(picocolors.gray('  (no graphics payload)'));
    return;
  }
  const logLine = (depth, text) => {
    console.log(picocolors.gray(`${'  '.repeat(depth)}${text}`));
  };
  logLine(0, '[kvc]');
  logLine(1, 'graphics');
  printGraphicsNodes(raw.graphics, logLine, 2);
}

function printGraphicsNodes(nodes, logLine, depth) {
  if (Array.isArray(nodes)) {
    nodes.forEach((node, index) => {
      logLine(depth, `graphics[${index}]`);
      printGraphicsNodes(node, logLine, depth + 1);
    });
    return;
  }
  if (nodes && typeof nodes === 'object') {
    if (nodes.type) {
      const labelParts = [nodes.type];
      if (nodes.name) {
        labelParts.push(nodes.name);
      }
      if (nodes.tag) {
        labelParts.push(`tag:${nodes.tag}`);
      }
      const label = labelParts.join(':');
      logLine(depth, `-${label}`);
      if (nodes.graphics) {
        printGraphicsNodes(nodes.graphics, logLine, depth + 1);
      }
      return;
    }
  }
  logLine(depth, String(nodes));
}

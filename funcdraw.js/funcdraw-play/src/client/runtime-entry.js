import * as opentype from 'opentype.js';
import * as funcscript from '@tewelde/funcscript';
import { createExpression as createFuncDrawExpression } from '@funcdraw/core';

function createSnapshotResolver(snapshotRoot, snapshotsByPackageName) {
  if (!snapshotRoot || typeof snapshotRoot !== 'object') {
    throw new Error('createSnapshotResolver: expected snapshot root');
  }
  const children = snapshotRoot.children || {};
  const expressions = snapshotRoot.expressions || {};
  const packages = snapshotsByPackageName || {};

  const normalizeKey = (segments) => (Array.isArray(segments) && segments.length > 0 ? segments.join('/') : '');

  const listChildren = (path) => {
    const key = normalizeKey(path);
    return children[key] || [];
  };

  const getExpression = (path) => {
    const key = normalizeKey(path);
    return expressions[key] || null;
  };

  const packageFn = (name) => {
    const key = name == null ? '' : String(name);
    if (!key) {
      throw new Error('package requires a package name');
    }
    const pkgSnapshot = packages[key];
    if (!pkgSnapshot) {
      throw new Error(`Package '${key}' is not available in the browser snapshot`);
    }
    return createSnapshotResolver(pkgSnapshot, packages);
  };

  return {
    listChildren,
    getExpression,
    package: packageFn
  };
}

function normalizeStepResult(raw) {
  if (Array.isArray(raw) && raw.length >= 2) {
    return { state: raw[0], events: normalizeEventList(raw[1]) };
  }
  if (raw && typeof raw === 'object' && !Array.isArray(raw)) {
    const state = raw.nextState !== undefined ? raw.nextState : raw.state;
    const events = raw.events !== undefined ? raw.events : raw.outEvents;
    return { state, events: normalizeEventList(events) };
  }
  if (isIterable(raw)) {
    const flattened = Array.from(raw);
    if (flattened.length >= 2) {
      return { state: flattened[0], events: normalizeEventList(flattened[1]) };
    }
  }
  throw new Error('Stepper functions must return [nextState, events] or { state, events }.');
}

function normalizeEventList(value) {
  if (value === undefined || value === null) {
    return [];
  }
  if (Array.isArray(value)) {
    return value;
  }
  if (isIterable(value)) {
    return Array.from(value);
  }
  return [value];
}

function isIterable(value) {
  return Boolean(value && typeof value !== 'string' && typeof value[Symbol.iterator] === 'function');
}

export function createBrowserRuntime({ bootstrap, fontBuffer }) {
  if (!bootstrap || typeof bootstrap !== 'object' || !bootstrap.snapshot) {
    throw new Error('createBrowserRuntime: expected bootstrap payload with snapshot');
  }
  if (!fontBuffer || !(fontBuffer instanceof ArrayBuffer)) {
    throw new Error('createBrowserRuntime: expected fontBuffer ArrayBuffer');
  }

  const font = opentype.parse(fontBuffer);
  const snapshot = bootstrap.snapshot;
  const resolver = createSnapshotResolver(snapshot.root, snapshot.packages || {});
  const expression = createFuncDrawExpression(resolver, {
    engine: funcscript,
    font
  });

  let modelState = null;
  let retainedStepFn = null;
  let timelineValue = 0;
  let canvasSize = { width: 40, height: 30 };
  let lastEvalTime = null;
  let lastEvalCanvas = null;

  const setTimeline = (value) => {
    const num = Number(value);
    if (Number.isFinite(num)) {
      timelineValue = num;
    }
  };

  const setCanvasSize = ({ width, height }) => {
    const w = Number(width);
    const h = Number(height);
    if (Number.isFinite(w)) {
      canvasSize.width = w;
    }
    if (Number.isFinite(h)) {
      canvasSize.height = h;
    }
  };

  const evaluateOnce = ({ includeSvg }) => {
    const outputs = includeSvg ? ['raw', 'svg'] : ['raw'];
    const contextUsage = {
      t: { used: false },
      canvas: { used: false }
    };
    const result = expression.evaluate({
      output: outputs,
      stateArg: modelState,
      context: {
        t: timelineValue,
        canvas: {
          size: {
            width: canvasSize.width,
            height: canvasSize.height
          }
        }
      },
      createProvider: ({ engine, values }) => {
        class TrackingProvider extends engine.DefaultFsDataProvider {
          get(name) {
            if (name) {
              const key = String(name).toLowerCase();
              if (key === 't') {
                contextUsage.t.used = true;
              } else if (key === 'canvas') {
                contextUsage.canvas.used = true;
              }
            }
            return super.get(name);
          }
        }
        return new TrackingProvider(values);
      }
    });

    result.contextUsage = contextUsage;
    retainedStepFn = typeof result.step === 'function' ? result.step : null;
    lastEvalTime = timelineValue;
    lastEvalCanvas = { ...canvasSize };
    const stepIndicator = retainedStepFn ? '<step>' : null;
    if (stepIndicator) {
      result.step = stepIndicator;
      if (result.raw && typeof result.raw === 'object') {
        result.raw.step = stepIndicator;
      }
    } else {
      delete result.step;
      if (result.raw && typeof result.raw === 'object') {
        delete result.raw.step;
      }
    }
    result.timeline = { t: timelineValue };
    result.canvas = { ...canvasSize };
    result.state = modelState;
    return result;
  };

  const evaluateScene = ({ includeSvg, query, events, resetState } = {}) => {
    if (resetState) {
      modelState = null;
      retainedStepFn = null;
    }
    if (query && Object.prototype.hasOwnProperty.call(query, 'time')) {
      setTimeline(query.time);
    }
    if (
      query &&
      (Object.prototype.hasOwnProperty.call(query, 'canvasWidth') ||
        Object.prototype.hasOwnProperty.call(query, 'canvasHeight'))
    ) {
      setCanvasSize({
        width: query.canvasWidth,
        height: query.canvasHeight
      });
    }

    const queue = Array.isArray(events) ? [...events] : [];

    if (queue.length === 0) {
      return evaluateOnce({ includeSvg: Boolean(includeSvg) });
    }

    const needsPrime =
      !retainedStepFn ||
      lastEvalTime !== timelineValue ||
      !lastEvalCanvas ||
      lastEvalCanvas.width !== canvasSize.width ||
      lastEvalCanvas.height !== canvasSize.height;
    if (needsPrime) {
      evaluateOnce({ includeSvg: false });
      if (!retainedStepFn) {
        return null;
      }
    }

    let result = null;
    while (queue.length > 0) {
      if (!retainedStepFn) {
        queue.length = 0;
        break;
      }
      const nextEvent = queue.shift();
      const rawStepResult = retainedStepFn(nextEvent);
      if (rawStepResult === null) {
        continue;
      }
      const outcome = normalizeStepResult(rawStepResult);
      modelState = outcome.state;
      for (const evt of outcome.events) {
        queue.push(evt);
      }
      const includeSvgThisEval = Boolean(includeSvg) && queue.length === 0;
      result = evaluateOnce({ includeSvg: includeSvgThisEval });
    }

    if (result === null) {
      return null;
    }
    if (Boolean(includeSvg) && !result.svg) {
      result = evaluateOnce({ includeSvg: true });
    }
    return result;
  };

  return {
    evaluateScene
  };
}

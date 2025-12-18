'use strict';

function createHtmlTemplate({ initialTime = null, baseHref = '/', title = 'FuncDraw Play', embed = false } = {}) {
  const initialTimeLiteral = initialTime === null ? 'null' : String(initialTime);
  let safeBaseHref = typeof baseHref === 'string' && baseHref.trim().length > 0 ? baseHref.trim() : '/';
  if (!safeBaseHref.endsWith('/')) {
    safeBaseHref += '/';
  }
  const safeTitle = typeof title === 'string' && title.trim().length > 0 ? title.trim() : 'FuncDraw Play';
  const bodyClass = embed ? 'fd-embed' : '';
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <title>${escapeHtml(safeTitle)}</title>
  <meta name="viewport" content="width=device-width, initial-scale=1"/>
  <base href="${escapeHtmlAttribute(safeBaseHref)}"/>
  <style>
    @font-face {
      font-family: 'Inter';
      src: url('__funcdraw/assets/fonts/Inter-Regular.ttf') format('truetype');
      font-weight: 400;
      font-style: normal;
      font-display: swap;
    }
    :root {
      color-scheme: dark;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      background-color: #0f172a;
      color: #e2e8f0;
    }
    body {
      margin: 0;
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }
    header {
      padding: 12px 20px;
      background: rgba(15, 23, 42, 0.95);
      border-bottom: 1px solid rgba(148, 163, 184, 0.2);
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 20px;
    }
    body.fd-embed header {
      padding: 8px 10px;
    }
    body.fd-embed header h1 {
      display: none;
    }
    header h1 {
      font-size: 16px;
      margin: 0;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      color: #38bdf8;
    }
    #fd-toolbar {
      display: flex;
      gap: 12px;
      font-size: 13px;
      align-items: center;
    }
    #fd-toolbar button {
      padding: 6px 14px;
      border-radius: 999px;
      border: none;
      background: #38bdf8;
      color: #0f172a;
      font-weight: 600;
      cursor: pointer;
    }
    #fd-toolbar button:hover {
      filter: brightness(1.1);
    }
    #fd-time-controls {
      display: none;
      align-items: center;
      gap: 8px;
    }
    #fd-time-controls.active {
      display: flex;
    }
    #fd-time-scrub {
      width: 160px;
      accent-color: #38bdf8;
    }
    #fd-time-max {
      width: 84px;
      background: rgba(2, 6, 23, 0.55);
      color: #e2e8f0;
      border: 1px solid rgba(148, 163, 184, 0.2);
      border-radius: 10px;
      padding: 4px 8px;
      font-variant-numeric: tabular-nums;
      outline: none;
    }
    #fd-time-max:focus {
      border-color: rgba(56, 189, 248, 0.75);
    }
    #fd-time-label {
      font-variant-numeric: tabular-nums;
      color: #94a3b8;
      font-weight: 500;
    }
    #fd-time-speed {
      background: rgba(2, 6, 23, 0.55);
      color: #e2e8f0;
      border: 1px solid rgba(148, 163, 184, 0.2);
      border-radius: 999px;
      padding: 4px 10px;
      font-size: 12px;
      font-weight: 600;
      outline: none;
    }
    #fd-time-speed:focus {
      border-color: rgba(56, 189, 248, 0.75);
    }
    #fd-frame-time-label {
      font-variant-numeric: tabular-nums;
      color: #94a3b8;
      font-weight: 500;
    }
    #fd-warning {
      color: #fbbf24;
    }
    main {
      flex: 1;
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 24px;
    }
    body.fd-embed main {
      padding: 0;
    }
    canvas {
      background: #020617;
      border-radius: 12px;
      box-shadow: 0 20px 30px rgba(2, 6, 23, 0.65);
      max-width: 100%;
      max-height: calc(100vh - 140px);
    }
    body.fd-embed canvas {
      border-radius: 0;
      max-height: 100vh;
      box-shadow: none;
    }
  </style>
</head>
<body class="${bodyClass}">
  <header id="fd-header">
    <h1>${escapeHtml(safeTitle)}</h1>
    <div id="fd-toolbar">
      <span id="fd-stats">loading…</span>
      <span id="fd-warning"></span>
      <div id="fd-time-controls">
        <button id="fd-play-toggle">Play</button>
        <button id="fd-reset-timeline">Reset</button>
        <select id="fd-time-speed" title="Playback speed">
          <option value="2">2×</option>
          <option value="1" selected>1×</option>
          <option value="0.5">1/2×</option>
          <option value="0.25">1/4×</option>
          <option value="0.125">1/8×</option>
        </select>
        <input id="fd-time-scrub" type="range" min="0" max="10" step="0.01" value="0" />
        <input id="fd-time-max" type="number" min="0" step="1" value="10" title="Timeline max (seconds)" />
        <span id="fd-time-label">t=0.00s</span>
        <span id="fd-frame-time-label">avg10=—</span>
      </div>
      <button id="fd-refresh">Refresh</button>
    </div>
  </header>
  <main id="fd-stage">
    <canvas id="fd-canvas" width="640" height="360"></canvas>
  </main>
  <script src="__funcdraw/runtime.js"></script>
  <script>
    const INITIAL_TIME = ${initialTimeLiteral};
    const BOOTSTRAP_URL = '__funcdraw/bootstrap';
    const FONT_URL = '__funcdraw/assets/fonts/Inter-Regular.ttf';
    const canvas = document.getElementById('fd-canvas');
    const ctx = canvas.getContext('2d');
    const stats = document.getElementById('fd-stats');
    const warningsEl = document.getElementById('fd-warning');
    const refreshButton = document.getElementById('fd-refresh');
    const animationControls = {
      container: document.getElementById('fd-time-controls'),
      toggle: document.getElementById('fd-play-toggle'),
      reset: document.getElementById('fd-reset-timeline'),
      speed: document.getElementById('fd-time-speed'),
      scrub: document.getElementById('fd-time-scrub'),
      max: document.getElementById('fd-time-max'),
      label: document.getElementById('fd-time-label'),
      frameTimeLabel: document.getElementById('fd-frame-time-label')
    };
    const animationState = {
      enabled: false,
      playing: false,
      time: INITIAL_TIME === null ? 0 : INITIAL_TIME,
      pendingInitialTime: INITIAL_TIME,
      speed: 1,
      scrubMax: 10,
      scrubTimer: null,
      raf: null,
      lastTick: null,
      renderFrameTimes: []
    };
    const canvasHookState = {
      active: false
    };
    const headerEl = document.getElementById('fd-header');
    const logPrefix = '[FuncDraw]';
    const logDebug = (...args) => console.debug(logPrefix, ...args);
    const logInfo = (...args) => console.info(logPrefix, ...args);
    const logWarn = (...args) => console.warn(logPrefix, ...args);
    const logError = (...args) => console.error(logPrefix, ...args);
    let latestScene = null;
    let runtime = null;
    let projector = null;
    const pointerState = { down: new Set(), captured: new Set() };

    logInfo('Booting FuncDraw Play browser client');

    async function ensureRuntime() {
      if (runtime) {
        return runtime;
      }
      if (!window.FuncDrawPlayRuntime || typeof window.FuncDrawPlayRuntime.createBrowserRuntime !== 'function') {
        throw new Error('FuncDraw runtime did not load (missing createBrowserRuntime)');
      }

      const bootstrapResponse = await fetch(BOOTSTRAP_URL + '?_ts=' + Date.now().toString());
      if (!bootstrapResponse.ok) {
        throw new Error('Failed to load FuncDraw bootstrap payload');
      }
      const bootstrap = await bootstrapResponse.json();

      const fontResponse = await fetch(FONT_URL + '?_ts=' + Date.now().toString());
      if (!fontResponse.ok) {
        throw new Error('Failed to load FuncDraw font payload');
      }
      const fontBuffer = await fontResponse.arrayBuffer();

      runtime = window.FuncDrawPlayRuntime.createBrowserRuntime({
        bootstrap,
        fontBuffer
      });
      return runtime;
    }

    async function loadScene(reason = 'manual', loadOptions = {}) {
      const events = Array.isArray(loadOptions.events) ? loadOptions.events : [];
      const resetState = Boolean(loadOptions.resetState);
      const params = loadOptions.params && typeof loadOptions.params === 'object' ? loadOptions.params : {};
      const hasCustomTimeParam = Object.prototype.hasOwnProperty.call(params, 'time');
      const timeValue = hasCustomTimeParam
        ? params.time
        : animationState.pendingInitialTime !== null
          ? formatTimeParam(animationState.pendingInitialTime)
          : animationState.enabled
            ? animationState.time
            : undefined;
      const runtimeInstance = await ensureRuntime();
      logInfo('Evaluating scene (browser runtime)', { reason, resetState, eventCount: events.length });
      if (events.length > 0) {
        logInfo('Sending events', events);
      }
      try {
        const payload = runtimeInstance.evaluateScene({
          includeSvg: false,
          query: {
            time: timeValue,
            canvasWidth: canvas.width,
            canvasHeight: canvas.height
          },
          events,
          resetState
        });
        logDebug('Scene payload received', payload);
        if (payload === null) {
          logInfo('Scene ignored events (null payload)', { reason, eventCount: events.length });
          return null;
        }
        animationState.pendingInitialTime = null;
        const prevState = latestScene && latestScene.state;
        latestScene = payload;
        const logSceneDetails =
          reason === 'initial' ||
          reason === 'event-open' ||
          reason === 'server-reload' ||
          resetState ||
          events.length > 0 ||
          String(reason).startsWith('pointer-');
        if (logSceneDetails) {
          logInfo('Scene flags', {
            supportsStepper: supportsStepper(payload),
            step: payload && payload.step,
            rawStep: payload && payload.raw && payload.raw.step
          });
          logInfo('Scene state', { prevState, nextState: payload && payload.state });
        }
        renderScene(payload);
        if (payload.svg) {
          logDebug('SVG output available');
          console.groupCollapsed('[FuncDraw] SVG Output');
          console.log(payload.svg);
          console.groupEnd();
        }
        return payload;
      } catch (error) {
        logError('Scene load failed', error);
        stats.textContent = error.message;
        warningsEl.textContent = 'Load error';
        stopAnimation();
        return null;
      }
    }

    function renderScene(scene) {
      if (!scene) {
        return;
      }
      syncContextUsageFromPayload(scene);
      logDebug('Rendering scene', {
        view: scene.view,
        warnings: Array.isArray(scene.warnings) ? scene.warnings.length : 0,
        hasRaw: Boolean(scene.raw)
      });
      const viewBox = resolveViewBox(scene);
      resizeCanvasForView();
      projector = createProjector(viewBox);
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      const raw = scene.raw || {};
      drawNodes(raw.graphics || []);
      const primitiveCount = countPrimitives(raw.graphics || []);
      stats.textContent =
        primitiveCount + ' primitives · view ' + viewBox.width + '×' + viewBox.height;
      const warnings = scene.warnings || [];
      if (warnings.length > 0) {
        warningsEl.textContent = warnings.length + ' warning(s)';
        logWarn('Scene warnings', warnings);
      } else {
        warningsEl.textContent = '';
        logDebug('No warnings reported for scene');
      }
    }

    function resolveViewBox(scene) {
      const direct = parseViewBox(scene.view);
      if (direct) {
        return direct;
      }
      const rawView = parseViewBox(scene.raw && scene.raw.view);
      if (rawView) {
        return rawView;
      }
      const nestedView = parseViewBox(
        scene.raw && scene.raw.raw && scene.raw.raw.scene && scene.raw.raw.scene.view
      );
      if (nestedView) {
        return nestedView;
      }
      return defaultViewBox();
    }

    function parseViewBox(value) {
      if (!value) {
        return null;
      }
      if (Array.isArray(value) && value.length >= 2) {
        const width = Number(value[0]) || 0;
        const height = Number(value[1]) || 0;
        if (width > 0 && height > 0) {
          return {
            left: 0,
            bottom: 0,
            right: width,
            top: height,
            width,
            height
          };
        }
        return null;
      }
      if (typeof value === 'object') {
        const left = Number(value.left);
        const bottom = Number(value.bottom);
        const right = Number(value.right);
        const top = Number(value.top);
        if ([left, bottom, right, top].every(Number.isFinite) && right > left && top > bottom) {
          return {
            left,
            bottom,
            right,
            top,
            width: right - left,
            height: top - bottom
          };
        }
      }
      return null;
    }

    function defaultViewBox() {
      const width = canvas.width || 640;
      const height = canvas.height || 360;
      return {
        left: 0,
        bottom: 0,
        right: width,
        top: height,
        width,
        height
      };
    }

    function resizeCanvasForView() {
      const available = computeAvailableViewport();
      const pixelWidth = Math.max(1, Math.round(available.width));
      const pixelHeight = Math.max(1, Math.round(available.height));
      canvas.width = pixelWidth;
      canvas.height = pixelHeight;
      canvas.style.width = pixelWidth + 'px';
      canvas.style.height = pixelHeight + 'px';
      logDebug('Resized canvas to available viewport', {
        pixelWidth,
        pixelHeight
      });
    }

    function computeAvailableViewport() {
      const horizontalPadding = 80;
      const verticalPadding = 120;
      const headerHeight = headerEl ? headerEl.offsetHeight : 0;
      const width = Math.max(window.innerWidth - horizontalPadding, 320);
      const height = Math.max(window.innerHeight - headerHeight - verticalPadding, 240);
      return { width, height };
    }

    function createProjector(viewBox) {
      const width = Math.max(viewBox.width, 1);
      const height = Math.max(viewBox.height, 1);
      const scale = Math.min(canvas.width / width, canvas.height / height) || 1;
      const drawnWidth = width * scale;
      const drawnHeight = height * scale;
      const offsetX = (canvas.width - drawnWidth) / 2;
      const offsetY = (canvas.height - drawnHeight) / 2;
      const baseE = offsetX - viewBox.left * scale;
      const baseF = offsetY + viewBox.top * scale;
      return {
        scale,
        offsetX,
        offsetY,
        baseE,
        baseF,
        projectPoint(point) {
          const [mx, my] = toPoint(point);
          const x = offsetX + (mx - viewBox.left) * scale;
          const y = offsetY + (viewBox.top - my) * scale;
          return [x, y];
        },
        unprojectPoint(point) {
          const [cx, cy] = toPoint(point);
          const x = viewBox.left + (cx - offsetX) / scale;
          const y = viewBox.top - (cy - offsetY) / scale;
          return [x, y];
        },
        projectSize(size) {
          const [sx, sy] = toPoint(size);
          return [sx * scale, sy * scale];
        },
        projectScalarX(value) {
          return value * scale;
        },
        projectScalarY(value) {
          return value * scale;
        },
        projectScalar(value) {
          return value * scale;
        },
        projectMatrix(matrix) {
          if (!Array.isArray(matrix) || matrix.length !== 6) {
            throw new Error('transform.matrix must be [a, b, c, d, e, f]');
          }
          const [a, b, c, d, e, f] = matrix;
          if (![a, b, c, d, e, f].every((value) => typeof value === 'number' && Number.isFinite(value))) {
            throw new Error('transform.matrix must be [a, b, c, d, e, f]');
          }
          return [
            a,
            -b,
            -c,
            d,
            baseE * (1 - a) + c * baseF + scale * e,
            b * baseE + baseF * (1 - d) - scale * f
          ];
        }
      };
    }

    function countPrimitives(nodes) {
      if (!nodes) {
        return 0;
      }
      if (!Array.isArray(nodes)) {
        return 1;
      }
      let total = 0;
      for (const node of nodes) {
        if (Array.isArray(node)) {
          total += countPrimitives(node);
        } else if (node && typeof node === 'object') {
          if (Array.isArray(node.graphics)) {
            total += countPrimitives(node.graphics);
          } else {
            total += 1;
          }
        }
      }
      return total;
    }

    function drawNodes(nodes) {
      if (!nodes || !projector) {
        return;
      }
      if (Array.isArray(nodes)) {
        for (const node of nodes) {
          drawNodes(node);
        }
        return;
      }
      const type = (nodes.type || '').toLowerCase();
      if (type === 'transform') {
        drawTransform(nodes);
        return;
      }
      if (type === 'group') {
        drawGroup(nodes);
        return;
      }
      if (nodes.graphics) {
        drawWithComposite(nodes, () => drawNodes(nodes.graphics));
        return;
      }

      drawWithComposite(nodes, () => {
        switch (type) {
          case 'line':
            drawLine(nodes);
            break;
          case 'rect':
          case 'rectangle':
            drawRect(nodes);
            break;
          case 'circle':
            drawCircle(nodes);
            break;
          case 'ellipse':
            drawEllipse(nodes);
            break;
          case 'polygon':
            drawPolygon(nodes);
            break;
          case 'text':
            drawText(nodes);
            break;
          default:
            if (nodes && nodes.type) {
              logWarn('Unsupported node type skipped', nodes.type);
            } else {
              logWarn('Skipped node without a type definition', nodes);
            }
            break;
        }
      });
    }

    function drawTransform(node) {
      const matrix = projector.projectMatrix(node.matrix);
      ctx.save();
      ctx.transform(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5]);
      drawWithComposite(node, () => drawNodes(node.graphics));
      ctx.restore();
    }

    function drawGroup(node) {
      drawWithComposite(node, () => drawNodes(node.graphics));
    }

    function drawWithComposite(node, draw) {
      if (!node || typeof node !== 'object') {
        draw();
        return;
      }
      const hasOpacity = node.opacity !== undefined && node.opacity !== null;
      const hasBlend = node.blendMode !== undefined && node.blendMode !== null;
      if (!hasOpacity && !hasBlend) {
        draw();
        return;
      }
      ctx.save();
      if (hasOpacity) {
        const opacity = Number(node.opacity);
        if (!Number.isFinite(opacity)) {
          throw new Error('opacity must be a finite number');
        }
        ctx.globalAlpha *= opacity;
      }
      if (hasBlend) {
        const blendMode = String(node.blendMode).trim();
        if (blendMode) {
          ctx.globalCompositeOperation = blendMode;
        }
      }
      draw();
      ctx.restore();
    }

    function paintToCss(value) {
      if (value === undefined || value === null) {
        return null;
      }
      if (typeof value === 'string') {
        return value;
      }
      if (value && typeof value === 'object' && !Array.isArray(value)) {
        const type = String(value.type || '').toLowerCase();
        if (type === 'color') {
          const space = typeof value.space === 'string' ? value.space.toLowerCase() : '';
          if (space !== 'srgb') {
            throw new Error('Unsupported color space (expected srgb)');
          }
          const r = Number(value.r);
          const g = Number(value.g);
          const b = Number(value.b);
          const a = Number(value.a);
          if (![r, g, b, a].every(Number.isFinite)) {
            throw new Error('Invalid srgb color value (expected numbers r,g,b,a)');
          }
          return 'rgba(' + r + ', ' + g + ', ' + b + ', ' + a + ')';
        }
      }
      throw new Error('Unsupported paint value');
    }

    function drawLine(node) {
      const from = projector.projectPoint(node.from);
      const to = projector.projectPoint(node.to);
      const stroke = paintToCss(node.stroke) || '#38bdf8';
      if (stroke === 'none') {
        return;
      }
      ctx.strokeStyle = stroke;
      ctx.lineWidth = projectStrokeWidth(node.width);
      ctx.beginPath();
      ctx.moveTo(from[0], from[1]);
      ctx.lineTo(to[0], to[1]);
      ctx.stroke();
    }

    function drawRect(node) {
      const pos = toPoint(node.position);
      const size = toPoint(node.size || [1, 1]);
      const bottomLeft = projector.projectPoint(pos);
      const projectedSize = projector.projectSize(size);
      const width = projectedSize[0];
      const height = projectedSize[1];
      const x = bottomLeft[0];
      const y = bottomLeft[1] - height;
      const fill = paintToCss(node.fill);
      const hasFill = Boolean(fill && fill !== 'none');
      if (hasFill) {
        ctx.fillStyle = fill;
        ctx.fillRect(x, y, width, height);
      }
      const stroke = paintToCss(node.stroke);
      const hasStroke = Boolean(stroke && stroke !== 'none');
      if (hasStroke || !hasFill) {
        ctx.strokeStyle = hasStroke ? stroke : '#38bdf8';
        ctx.lineWidth = projectStrokeWidth(node.width);
        ctx.strokeRect(x, y, width, height);
      }
    }

    function drawCircle(node) {
      const center = projector.projectPoint(node.center);
      const radius = projector.projectScalarX(Math.abs(Number(node.radius) || 1));
      ctx.beginPath();
      ctx.arc(center[0], center[1], radius, 0, Math.PI * 2);
      const fill = paintToCss(node.fill);
      if (fill && fill !== 'none') {
        ctx.fillStyle = fill;
        ctx.fill();
      }
      const stroke = paintToCss(node.stroke) || '#38bdf8';
      if (stroke === 'none') {
        return;
      }
      ctx.strokeStyle = stroke;
      ctx.lineWidth = projectStrokeWidth(node.width);
      ctx.stroke();
    }

    function drawEllipse(node) {
      const center = projector.projectPoint(node.center);
      const rx = projector.projectScalarX(Math.abs(Number(node.radiusX) || Number(node.rx) || 1));
      const ry = projector.projectScalarY(Math.abs(Number(node.radiusY) || Number(node.ry) || 1));
      ctx.beginPath();
      ctx.ellipse(center[0], center[1], rx, ry, 0, 0, Math.PI * 2);
      const fill = paintToCss(node.fill);
      if (fill && fill !== 'none') {
        ctx.fillStyle = fill;
        ctx.fill();
      }
      const stroke = paintToCss(node.stroke) || '#38bdf8';
      if (stroke === 'none') {
        return;
      }
      ctx.strokeStyle = stroke;
      ctx.lineWidth = projectStrokeWidth(node.width);
      ctx.stroke();
    }

    function drawPolygon(node) {
      if (!Array.isArray(node.points) || node.points.length === 0) {
        return;
      }
      ctx.beginPath();
      node.points.forEach((point, index) => {
        const [x, y] = projector.projectPoint(point);
        if (index === 0) {
          ctx.moveTo(x, y);
        } else {
          ctx.lineTo(x, y);
        }
      });
      ctx.closePath();
      const fill = paintToCss(node.fill);
      if (fill && fill !== 'none') {
        ctx.fillStyle = fill;
        ctx.fill();
      }
      const stroke = paintToCss(node.stroke) || '#38bdf8';
      if (stroke === 'none') {
        return;
      }
      ctx.strokeStyle = stroke;
      ctx.lineWidth = projectStrokeWidth(node.width);
      ctx.stroke();
    }

    function drawText(node) {
      const pos = toPoint(node.position);
      const [x, yBase] = projector.projectPoint(pos);
      const fontSize = Math.abs(Number(node.fontSize) || 12);
      const fontSizePx = projector.projectScalarY(fontSize);
      ctx.fillStyle = paintToCss(node.color) || paintToCss(node.fill) || '#e2e8f0';
      ctx.font = fontSizePx + 'px Inter, sans-serif';
      ctx.textAlign = (node.align || 'left').toLowerCase();
      ctx.textBaseline = 'alphabetic';
      const safeText = node.text !== undefined && node.text !== null ? node.text : '';
      const lines = String(safeText).split(/\\r?\\n/);
      let offset = 0;
      const lineHeight = fontSizePx * 1.2;
      for (const line of lines) {
        ctx.fillText(line, x, yBase + offset);
        offset += lineHeight;
      }
    }

    function toPoint(value) {
      if (Array.isArray(value) && value.length >= 2) {
        return [Number(value[0]) || 0, Number(value[1]) || 0];
      }
      return [0, 0];
    }

    function projectStrokeWidth(value) {
      const base = Math.abs(Number(value)) || 0.25;
      const scale = projector ? projector.scale : 1;
      return Math.max(base * scale, 0.5);
    }

    function addCanvasSizeParams(params) {
      const size = getCanvasSize();
      params.set('canvasWidth', String(size.width));
      params.set('canvasHeight', String(size.height));
    }

    function getCanvasSize() {
      return {
        width: canvas.width || 0,
        height: canvas.height || 0
      };
    }

    refreshButton.addEventListener('click', () => {
      logInfo('Refresh button clicked');
      runtime = null;
      loadScene('button');
    });
    window.addEventListener('keydown', (event) => {
      if (event.key === 'r') {
        logInfo('Keyboard refresh triggered');
        loadScene('keyboard');
      }
    });

    animationControls.toggle.addEventListener('click', () => {
      if (!animationState.enabled) {
        return;
      }
      if (animationState.playing) {
        stopAnimation();
      } else {
        startAnimation();
      }
    });

    animationControls.reset.addEventListener('click', () => {
      if (!animationState.enabled) {
        return;
      }
      stopAnimation({ preserveTime: false });
      loadScene('timeline-reset', {
        params: { time: formatTimeParam(animationState.time) },
        resetState: true
      });
    });

    animationControls.speed.addEventListener('change', () => {
      const next = Number(animationControls.speed.value);
      if (!Number.isFinite(next) || next <= 0) {
        animationState.speed = 1;
        animationControls.speed.value = '1';
        return;
      }
      animationState.speed = next;
    });

    animationControls.scrub.addEventListener('input', () => {
      if (!animationState.enabled) {
        return;
      }
      stopAnimation();
      const next = Number(animationControls.scrub.value);
      if (Number.isFinite(next)) {
        animationState.time = Math.max(0, next);
        updateAnimationUi();
        scheduleTimelineEvaluation('timeline-scrub');
      }
    });

    animationControls.max.addEventListener('change', () => {
      if (!animationState.enabled) {
        return;
      }
      const parsed = Number(animationControls.max.value);
      if (!Number.isFinite(parsed)) {
        return;
      }
      const nextMax = Math.max(0, parsed);
      animationState.scrubMax = nextMax;
      if (animationState.time > nextMax) {
        animationState.time = nextMax;
        scheduleTimelineEvaluation('timeline-max-clamp');
      }
      updateAnimationUi();
    });

    canvas.addEventListener('pointerdown', (event) => {
      sendPointerEvent('down', event);
    });
    canvas.addEventListener('pointerup', (event) => {
      sendPointerEvent('up', event);
    });
    canvas.addEventListener('pointercancel', (event) => {
      sendPointerEvent('cancel', event);
    });
    canvas.addEventListener('pointermove', (event) => {
      sendPointerEvent('move', event);
    });
    canvas.addEventListener('pointerenter', (event) => {
      sendPointerEvent('enter', event);
    });
    canvas.addEventListener('pointerleave', (event) => {
      sendPointerEvent('leave', event);
    });
    canvas.addEventListener('pointerover', (event) => {
      sendPointerEvent('over', event);
    });
    canvas.addEventListener('pointerout', (event) => {
      sendPointerEvent('out', event);
    });
    canvas.addEventListener('gotpointercapture', (event) => {
      sendPointerEvent('gotcapture', event);
    });
    canvas.addEventListener('lostpointercapture', (event) => {
      sendPointerEvent('lostcapture', event);
    });
    canvas.addEventListener('pointerrawupdate', (event) => {
      sendPointerEvent('rawupdate', event);
    });

    const events = new EventSource('__funcdraw/events');
    events.addEventListener('reload', () => {
      logInfo('Reload event received from server');
      stopAnimation();
      runtime = null;
      loadScene('server-reload');
    });
    events.addEventListener('open', () => {
      logInfo('Connected to FuncDraw event stream');
      loadScene('event-open');
    });
    events.addEventListener('error', (event) => {
      logWarn('Event stream error', event);
    });

    window.addEventListener('resize', () => {
      resizeCanvasForView();
      if (!latestScene) {
        return;
      }
      if (canvasHookState.active) {
        logInfo('Canvas resized, reloading scene (canvas context used)');
        loadScene('canvas-resize');
      } else {
        logDebug('Window resized, re-rendering scene');
        renderScene(latestScene);
      }
    });

    function syncContextUsageFromPayload(scene) {
      const usage = (scene && scene.contextUsage) || {};
      syncAnimationFromContextUsage(usage, scene);
      syncCanvasUsageState(usage);
    }

    function syncAnimationFromContextUsage(usage, scene) {
      const timeUsage = usage.t;
      const usesTime = Boolean(timeUsage && timeUsage.used);
      if (!usesTime) {
        if (animationState.enabled) {
          stopAnimation({ preserveTime: false });
          animationState.enabled = false;
          animationControls.container.classList.remove('active');
          updateAnimationUi();
        }
        return;
      }
      animationState.enabled = true;
      animationControls.container.classList.add('active');
      if (scene.timeline && typeof scene.timeline.t === 'number' && Number.isFinite(scene.timeline.t)) {
        animationState.time = Number(scene.timeline.t);
      }
      updateAnimationUi();
    }

    function syncCanvasUsageState(usage) {
      const canvasUsage = usage.canvas;
      canvasHookState.active = Boolean(canvasUsage && canvasUsage.used);
    }

    function supportsStepper(scene) {
      const stepMarker = scene && (scene.step || (scene.raw && scene.raw.step));
      return stepMarker === '<step>';
    }

    function buildPointerEvent(action, event) {
      const rect = canvas.getBoundingClientRect();
      const scaleX = rect.width ? canvas.width / rect.width : 1;
      const scaleY = rect.height ? canvas.height / rect.height : 1;
      const canvasPoint = [(event.clientX - rect.left) * scaleX, (event.clientY - rect.top) * scaleY];
      const worldPoint = projector.unprojectPoint(canvasPoint);
      return {
        type: 'pointer',
        action,
        pointer: {
          id: event.pointerId,
          type: event.pointerType,
          isPrimary: event.isPrimary,
          down: pointerState.down.has(event.pointerId),
          captured: pointerState.captured.has(event.pointerId),
          pressure: event.pressure,
          tangentialPressure: event.tangentialPressure,
          tiltX: event.tiltX,
          tiltY: event.tiltY,
          twist: event.twist,
          width: event.width / projector.scale,
          height: event.height / projector.scale
        },
        button: event.button,
        buttons: event.buttons,
        modifiers: {
          alt: event.altKey,
          ctrl: event.ctrlKey,
          meta: event.metaKey,
          shift: event.shiftKey
        },
        point: {
          x: worldPoint[0],
          y: worldPoint[1]
        },
        time: animationState.time
      };
    }

    function sendPointerEvent(action, event) {
      if (action === 'up' || action === 'cancel') {
        pointerState.down.delete(event.pointerId);
        if (pointerState.captured.has(event.pointerId)) {
          canvas.releasePointerCapture(event.pointerId);
          pointerState.captured.delete(event.pointerId);
        }
      } else if (action === 'gotcapture') {
        pointerState.captured.add(event.pointerId);
      } else if (action === 'lostcapture') {
        pointerState.captured.delete(event.pointerId);
      }
      if (!latestScene) {
        logWarn('Pointer event ignored (no scene loaded yet)', action);
        return;
      }
      if (!supportsStepper(latestScene)) {
        logWarn('Pointer event ignored (scene does not expose a stepper)', {
          action,
          step: latestScene.step,
          rawStep: latestScene.raw && latestScene.raw.step
        });
        return;
      }
      if (!projector) {
        logWarn('Pointer event ignored (projector not ready yet)', action);
        return;
      }
      if (action === 'down') {
        pointerState.down.add(event.pointerId);
        canvas.setPointerCapture(event.pointerId);
        pointerState.captured.add(event.pointerId);
      }
      const payload = buildPointerEvent(action, event);
      logInfo('Pointer event', payload);
      loadScene('pointer-' + action, {
        events: [payload],
        params: { time: formatTimeParam(animationState.time) }
      });
    }

    function startAnimation() {
      if (!animationState.enabled || animationState.playing) {
        return;
      }
      animationState.playing = true;
      animationState.lastTick = null;
      animationState.raf = requestAnimationFrame(animationFrame);
      updateAnimationUi();
    }

    function stopAnimation(options = {}) {
      const preserveTime =
        options && Object.prototype.hasOwnProperty.call(options, 'preserveTime')
          ? Boolean(options.preserveTime)
          : true;
      if (animationState.raf !== null) {
        cancelAnimationFrame(animationState.raf);
        animationState.raf = null;
      }
      animationState.playing = false;
      animationState.lastTick = null;
      if (!preserveTime) {
        animationState.time = 0;
      }
      updateAnimationUi();
    }

    async function animationFrame(timestamp) {
      if (!animationState.playing) {
        animationState.raf = null;
        return;
      }
      if (animationState.lastTick === null) {
        animationState.lastTick = timestamp;
      }
      const delta = Math.max(0, timestamp - animationState.lastTick);
      animationState.lastTick = timestamp;
      animationState.time += (delta / 1000) * (animationState.speed || 1);
      updateAnimationUi();
      const frameStart = performance.now();
      const frameScene = await loadScene('animation', {
        params: { time: formatTimeParam(animationState.time) }
      });
      const frameDuration = performance.now() - frameStart;
      if (frameScene) {
        recordRenderFrameTime(frameDuration);
        updateAnimationUi();
      }
      if (animationState.playing) {
        animationState.raf = requestAnimationFrame(animationFrame);
      } else {
        animationState.raf = null;
      }
    }

    function updateAnimationUi() {
      if (!animationState.enabled) {
        animationControls.container.classList.remove('active');
        animationControls.frameTimeLabel.textContent = '';
        return;
      }
      animationControls.container.classList.add('active');
      animationControls.toggle.textContent = animationState.playing ? 'Pause' : 'Play';
      animationControls.reset.disabled = animationState.time === 0 && !animationState.playing;
      animationControls.label.textContent = 't=' + formatTimeDisplay(animationState.time);
      const max = resolveScrubMax(animationState.time, animationState.scrubMax);
      animationState.scrubMax = max;
      if (animationControls.scrub) {
        animationControls.scrub.max = String(max);
        animationControls.scrub.value = String(Math.min(Math.max(0, animationState.time), max));
      }
      if (animationControls.max) {
        animationControls.max.value = String(max);
      }
      const avgRenderTime = averageRenderFrameTime(animationState.renderFrameTimes);
      animationControls.frameTimeLabel.textContent =
        avgRenderTime === null ? 'avg10=—' : 'avg10=' + formatRenderTime(avgRenderTime);
    }

    function resolveScrubMax(time, currentMax) {
      const t = Number(time);
      const m = Number(currentMax);
      const base = Number.isFinite(m) && m > 0 ? m : 10;
      if (!Number.isFinite(t) || t <= base) {
        return base;
      }
      const next = Math.ceil(t / 5) * 5;
      return Math.max(base, next);
    }

    function scheduleTimelineEvaluation(reason) {
      if (animationState.scrubTimer !== null) {
        clearTimeout(animationState.scrubTimer);
        animationState.scrubTimer = null;
      }
      animationState.scrubTimer = setTimeout(() => {
        animationState.scrubTimer = null;
        loadScene(reason, { params: { time: formatTimeParam(animationState.time) } });
      }, 50);
    }

    function recordRenderFrameTime(value) {
      animationState.renderFrameTimes.push(value);
      if (animationState.renderFrameTimes.length > 10) {
        animationState.renderFrameTimes.shift();
      }
    }

    function averageRenderFrameTime(samples) {
      if (samples.length === 0) {
        return null;
      }
      let total = 0;
      for (const sample of samples) {
        total += sample;
      }
      return total / samples.length;
    }

    function formatRenderTime(value) {
      return Number(value).toFixed(1) + 'ms';
    }

    function formatTimeParam(value) {
      const num = Number(value);
      if (!Number.isFinite(num)) {
        return '0';
      }
      return num.toFixed(4);
    }

    function formatTimeDisplay(value) {
      const num = Number(value);
      if (!Number.isFinite(num)) {
        return '0.00s';
      }
      return num.toFixed(2) + 's';
    }

    resizeCanvasForView();
    loadScene('initial');
  </script>
</body>
</html>`;
}

module.exports = {
  createHtmlTemplate
};

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function escapeHtmlAttribute(value) {
  return escapeHtml(value).replace(/`/g, '&#96;');
}

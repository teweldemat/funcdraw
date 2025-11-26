'use strict';

function createHtmlTemplate() {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <title>FuncDraw Play</title>
  <meta name="viewport" content="width=device-width, initial-scale=1"/>
  <style>
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
    #fd-time-label {
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
    canvas {
      background: #020617;
      border-radius: 12px;
      box-shadow: 0 20px 30px rgba(2, 6, 23, 0.65);
      max-width: 100%;
      max-height: calc(100vh - 140px);
    }
  </style>
</head>
<body>
  <header id="fd-header">
    <h1>FuncDraw Play</h1>
    <div id="fd-toolbar">
      <span id="fd-stats">loading…</span>
      <span id="fd-warning"></span>
      <div id="fd-time-controls">
        <button id="fd-play-toggle">Play</button>
        <button id="fd-reset-timeline">Reset</button>
        <span id="fd-time-label">t=0.00s</span>
      </div>
      <button id="fd-refresh">Refresh</button>
    </div>
  </header>
  <main id="fd-stage">
    <canvas id="fd-canvas" width="640" height="360"></canvas>
  </main>
  <script>
    const API = '/__funcdraw/scene';
    const canvas = document.getElementById('fd-canvas');
    const ctx = canvas.getContext('2d');
    const stats = document.getElementById('fd-stats');
    const warningsEl = document.getElementById('fd-warning');
    const refreshButton = document.getElementById('fd-refresh');
    const animationControls = {
      container: document.getElementById('fd-time-controls'),
      toggle: document.getElementById('fd-play-toggle'),
      reset: document.getElementById('fd-reset-timeline'),
      label: document.getElementById('fd-time-label')
    };
    const animationState = {
      enabled: false,
      playing: false,
      time: 0,
      raf: null,
      lastTick: null
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
    let projector = null;

    logInfo('Booting FuncDraw Play browser client');

    async function loadScene(reason = 'manual', loadOptions = {}) {
      const params = new URLSearchParams();
      let hasCustomTimeParam = false;
      if (loadOptions.params && typeof loadOptions.params === 'object') {
        for (const [key, rawValue] of Object.entries(loadOptions.params)) {
          if (rawValue === undefined || rawValue === null) {
            continue;
          }
          params.append(key, String(rawValue));
          if (key === 'time') {
            hasCustomTimeParam = true;
          }
        }
      }
      if (!hasCustomTimeParam && animationState.enabled) {
        params.set('time', formatTimeParam(animationState.time));
      }
      addCanvasSizeParams(params);
      params.set('_ts', Date.now().toString());
      const requestUrl = API + '?' + params.toString();
      logInfo('Requesting scene', { reason, requestUrl });
      try {
        const response = await fetch(requestUrl);
        logDebug('Scene HTTP response', { status: response.status, ok: response.ok });
        if (!response.ok) {
          throw new Error('Failed to load scene');
        }
        const payload = await response.json();
        logDebug('Scene payload received', payload);
        latestScene = payload;
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
      syncValueHooksFromPayload(scene);
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
      return {
        scale,
        offsetX,
        offsetY,
        projectPoint(point) {
          const [mx, my] = toPoint(point);
          const x = offsetX + (mx - viewBox.left) * scale;
          const y = offsetY + (viewBox.top - my) * scale;
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
      if (nodes.graphics) {
        drawNodes(nodes.graphics);
        return;
      }
      switch ((nodes.type || '').toLowerCase()) {
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
    }

    function drawLine(node) {
      const from = projector.projectPoint(node.from);
      const to = projector.projectPoint(node.to);
      ctx.strokeStyle = node.stroke || '#38bdf8';
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
      if (node.fill) {
        ctx.fillStyle = node.fill;
        ctx.fillRect(x, y, width, height);
      }
      if (node.stroke || !node.fill) {
        ctx.strokeStyle = node.stroke || '#38bdf8';
        ctx.lineWidth = projectStrokeWidth(node.width);
        ctx.strokeRect(x, y, width, height);
      }
    }

    function drawCircle(node) {
      const center = projector.projectPoint(node.center);
      const radius = projector.projectScalarX(Math.abs(Number(node.radius) || 1));
      ctx.beginPath();
      ctx.arc(center[0], center[1], radius, 0, Math.PI * 2);
      if (node.fill) {
        ctx.fillStyle = node.fill;
        ctx.fill();
      }
      ctx.strokeStyle = node.stroke || '#38bdf8';
      ctx.lineWidth = projectStrokeWidth(node.width);
      ctx.stroke();
    }

    function drawEllipse(node) {
      const center = projector.projectPoint(node.center);
      const rx = projector.projectScalarX(Math.abs(Number(node.radiusX) || Number(node.rx) || 1));
      const ry = projector.projectScalarY(Math.abs(Number(node.radiusY) || Number(node.ry) || 1));
      ctx.beginPath();
      ctx.ellipse(center[0], center[1], rx, ry, 0, 0, Math.PI * 2);
      if (node.fill) {
        ctx.fillStyle = node.fill;
        ctx.fill();
      }
      ctx.strokeStyle = node.stroke || '#38bdf8';
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
      if (node.fill) {
        ctx.fillStyle = node.fill;
        ctx.fill();
      }
      ctx.strokeStyle = node.stroke || '#38bdf8';
      ctx.lineWidth = projectStrokeWidth(node.width);
      ctx.stroke();
    }

    function drawText(node) {
      const pos = toPoint(node.position);
      const [x, yBase] = projector.projectPoint(pos);
      const fontSize = Math.abs(Number(node.fontSize) || 12);
      const fontSizePx = projector.projectScalarY(fontSize);
      ctx.fillStyle = node.color || '#e2e8f0';
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
      loadScene('timeline-reset', { params: { time: formatTimeParam(animationState.time) } });
    });

    const events = new EventSource('/__funcdraw/events');
    events.addEventListener('reload', () => {
      logInfo('Reload event received from server');
      stopAnimation();
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
        logInfo('Canvas resized, reloading scene for canvas hook');
        loadScene('canvas-resize');
      } else {
        logDebug('Window resized, re-rendering scene');
        renderScene(latestScene);
      }
    });

    function syncValueHooksFromPayload(scene) {
      const hooks = (scene && scene.valueHooks) || {};
      syncAnimationFromHooks(hooks, scene);
      syncCanvasHookState(hooks);
    }

    function syncAnimationFromHooks(hooks, scene) {
      const timeHook = hooks.t;
      const usesTime = Boolean(timeHook && timeHook.used);
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

    function syncCanvasHookState(hooks) {
      const canvasHook = hooks.canvas;
      canvasHookState.active = Boolean(canvasHook && canvasHook.used);
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
      animationState.time += delta / 1000;
      updateAnimationUi();
      await loadScene('animation', {
        params: { time: formatTimeParam(animationState.time) }
      });
      if (animationState.playing) {
        animationState.raf = requestAnimationFrame(animationFrame);
      } else {
        animationState.raf = null;
      }
    }

    function updateAnimationUi() {
      if (!animationState.enabled) {
        animationControls.container.classList.remove('active');
        return;
      }
      animationControls.container.classList.add('active');
      animationControls.toggle.textContent = animationState.playing ? 'Pause' : 'Play';
      animationControls.reset.disabled = animationState.time === 0 && !animationState.playing;
      animationControls.label.textContent = 't=' + formatTimeDisplay(animationState.time);
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

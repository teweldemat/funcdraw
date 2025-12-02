const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const hasSteperManZoom = typeof stickmanModule?.steperManZoom === 'function';
const zoomBuilder = hasSteperManZoom
  ? stickmanModule.steperManZoom
  : () => ({ graphics: [], overlays: [], skeleton: {}, step: {} });

const view = { left: -24, bottom: -8, right: 24, top: 28 };
const anchorBase = [0, 10];
const groundY = 0;
const timeValue = typeof t === 'number' ? t : 0;

if (!hasSteperManZoom) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'package("@funcdraw/testlib").cartoon.stickman.steperManZoom is unavailable.',
        position: [0, 12],
        fill: '#ef4444',
        fontSize: 3,
        align: 'center'
      },
      {
        type: 'text',
        text: 'Make sure @funcdraw/testlib is installed and rebuilt.',
        position: [0, 8],
        fill: '#ef4444',
        fontSize: 2.4,
        align: 'center'
      }
    ]
  };
}

const progress = cycle01(timeValue * 0.65);
const zoomProgress = cycle01(timeValue * 0.33 + Math.PI * 0.5);
const direction = Math.floor(timeValue / 5) % 2 === 0 ? 'front' : 'back';

const hero = zoomBuilder({
  position: anchorBase,
  measurements: {
    torso: { direction },
    head: { direction }
  },
  palette: {
    overlayLeg: '#22d3ee',
    overlayHand: '#f97316'
  },
  progress,
  zoomProgress,
  zoomFactor: 1.65,
  swing: 0.4,
  handSwing: 0.25,
  direction
});

const walkerGraphics = Array.isArray(hero.graphics) ? hero.graphics : [];
const overlayGraphics = renderOverlays(hero.overlays);
const anchorPoint = hero.step?.anchorPoint ?? hero.sequenceState?.position ?? anchorBase;

return {
  view,
  graphics: [
    createGroundLine(view.left, view.right),
    ...walkerGraphics,
    ...overlayGraphics,
    ...createCross(anchorPoint, '#f59e0b', 1.5),
    createLabel(progress, zoomProgress, hero.step?.zoomFactor ?? 1, direction)
  ].filter(Boolean)
};

function cycle01(input) {
  return (Math.sin(input * Math.PI * 2) + 1) * 0.5;
}

function renderOverlays(overlays) {
  if (!Array.isArray(overlays)) {
    return [];
  }
  const graphics = [];
  for (const overlay of overlays) {
    if (!overlay || !isPoint(overlay.point)) {
      continue;
    }
    graphics.push(...createCross(overlay.point, overlay.color || '#ffffff', 1));
  }
  return graphics;
}

function createGroundLine(minX, maxX) {
  return {
    type: 'line',
    from: [minX, groundY],
    to: [maxX, groundY],
    stroke: '#94a3b8',
    width: 0.5
  };
}

function createCross(center, color, size = 1.2) {
  if (!isPoint(center)) {
    return [];
  }
  const half = size * 0.5;
  const width = Math.max(0.2, size * 0.12);
  return [
    {
      type: 'line',
      from: [center[0] - half, center[1]],
      to: [center[0] + half, center[1]],
      stroke: color,
      width
    },
    {
      type: 'line',
      from: [center[0], center[1] - half],
      to: [center[0], center[1] + half],
      stroke: color,
      width
    }
  ];
}

function createLabel(progressValue, zoomValue, zoomFactor, dir) {
  return {
    type: 'text',
    text: [
      `zoom stride • dir ${dir}`,
      `progress ${Math.round(progressValue * 100)}%`,
      `zoom ${Math.round(zoomValue * 100)}% of factor ${zoomFactor.toFixed(2)}`
    ].join('  |  '),
    position: [0, view.top - 2.5],
    fill: '#0f172a',
    fontSize: 2.2,
    align: 'center'
  };
}

function isPoint(value) {
  return (
    Array.isArray(value) &&
    value.length >= 2 &&
    typeof value[0] === 'number' &&
    typeof value[1] === 'number' &&
    Number.isFinite(value[0]) &&
    Number.isFinite(value[1])
  );
}

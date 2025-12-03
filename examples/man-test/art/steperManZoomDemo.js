const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const hasSteperManZoom = typeof stickmanModule?.steperManZoom === 'function';
const zoomBuilder = hasSteperManZoom
  ? stickmanModule.steperManZoom
  : () => ({ graphics: [], overlays: [], skeleton: {}, step: {} });

const view = constants.view;
const anchorBase = [0, 20];
const groundY = 0;
const timeValue = typeof t === 'number' ? t : 0;
const fixedLegDepth = -20;   // input depth for fixed leg (relative to anchor)
const movingLegDepth = -18;   // input depth for moving leg (relative to anchor)

if (!hasSteperManZoom) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'package("@funcdraw/testlib").cartoon.stickman.steperManZoom is unavailable.',
        position: [0, 12],
        fill: '#ef4444',
        fontSize: 12,
        align: 'center'
      },
      {
        type: 'text',
        text: 'Make sure @funcdraw/testlib is installed and rebuilt.',
        position: [0, 8],
        fill: '#ef4444',
        fontSize: 12,
        align: 'center'
      }
    ]
  };
}

const fixedSide = 'left';
const movingSide = 'right';
const stepDuration = 2; // loop the zoom stride so the demo keeps moving
const stepIndex = Math.floor(timeValue / stepDuration);
const progress = clamp01((timeValue % stepDuration) / stepDuration);
const direction = stepIndex % 2 === 0 ? 'front' : 'back';

const baseOptions = {
  position: anchorBase,
  measurements: {
    torso: { direction, height: 22, width: 12 },
    head: { direction, verticalExtent: 9 },
    hands: {
      left: { effectorCoordinate: [-7.8, 4.7] },
      right: { effectorCoordinate: [7.8, 4.7] }
    },
    legs: {
      left: { effectorCoordinate: fixedLegDepth },
      right: { effectorCoordinate: movingLegDepth }
    }
  },
  movingSide,
  movingFootTargetY: anchorBase[1] - 20,
  zoom: 1.65,
  progress
};

const startSample = zoomBuilder({ ...baseOptions, progress: 0 });
const endSample = zoomBuilder({ ...baseOptions, progress: 1 });

const hero = zoomBuilder({
  ...baseOptions,
  progress
});

const walkerGraphics = Array.isArray(hero.graphics) ? hero.graphics : [];
const overlayGraphics = renderOverlays(hero.overlays);
const anchorPoint = hero.step?.anchorPoint ?? hero.sequenceState?.position ?? anchorBase;
const fixedY = resolveFootY(startSample, fixedSide);
const movingStartY = resolveFootY(startSample, movingSide);
const movingEndY = resolveFootY(endSample, movingSide);

return {
  view,
  graphics: [
    createGroundLine(view.left, view.right),
    ...createYGuide(fixedY, '#22c55e'),
    ...createYGuide(movingStartY, '#ef4444'),
    ...createYGuide(movingEndY, '#f97316'),
    ...walkerGraphics,
    ...overlayGraphics,
    ...createCross(anchorPoint, '#f59e0b', 1.5),
    createLabel(progress, hero.step?.zoom ?? hero.step?.zoomFactor ?? 1.65, direction)
  ].filter(Boolean)
};

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

function createLabel(progressValue, zoomFactor, dir) {
  return {
    type: 'text',
    text: [
      `zoom stride • dir ${dir}`,
      `progress ${Math.round(progressValue * 100)}%`,
      `zoom factor ${zoomFactor.toFixed(2)}`
    ].join('  |  '),
    position: [0, view.top - 2.5],
    fill: '#0f172a',
    fontSize: 12,
    align: 'center'
  };
}

function createYGuide(y, color) {
  if (typeof y !== 'number' || !Number.isFinite(y)) {
    return [];
  }
  return [
    {
      type: 'line',
      from: [view.left, y],
      to: [view.right, y],
      stroke: color,
      width: 0.25,
      dash: [1.2, 0.8]
    }
  ];
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

function resolveFootY(sample, side) {
  if (!sample || !sample.sequenceState) {
    return null;
  }
  const anchor = Array.isArray(sample.sequenceState.position) ? sample.sequenceState.position : anchorBase;
  const leg = sample.sequenceState.measurements?.legs?.[side];
  if (!leg || !Array.isArray(leg.effectorCoordinate)) {
    return null;
  }
  return anchor[1] + leg.effectorCoordinate[1];
}

function clamp01(value) {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    value = Number(value);
  }
  if (!Number.isFinite(value)) {
    return 0;
  }
  if (value < 0) return 0;
  if (value > 1) return 1;
  return value;
}

const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const helperModule = cartoonLibrary?.helpers ?? {};
const hasSteperManProfile = typeof stickmanModule?.steperManProfile === 'function';
const steperBuilder = hasSteperManProfile
  ? stickmanModule.steperManProfile
  : () => ({ graphics: [], overlays: [], skeleton: {}, step: {} });
const measureDistance = typeof helperModule?.distance === 'function' ? helperModule.distance : fallbackDistance;

const view = { left: -36, bottom: -12, right: 36, top: 32 };
const groundY = 0;
const testingLegLengths = { upper: 6.2, lower: 5.8 };
const anchorBaseY = 9.3;
const timeValue = typeof t === 'number' ? t : 0;
const stepSpeed = 0.35;
const testerHandSwing = {
  amplitude: 2.2,
  lift: 0.55,
  forwardOffset: 0.2
};

if (!hasSteperManProfile) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'package("@funcdraw/testlib").cartoon.stickman.steperManProfile is unavailable.',
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

const leftRig = buildSteperRig({
  label: 'Left foot planted',
  fixedSide: 'left',
  fixedPoint: [-22, groundY],
  movingStartPoint: [-34, groundY],
  movingTargetPoint: [-10, groundY],
  progress: triangleWave(timeValue * stepSpeed),
  bobPhase: 0,
  palette: {
    overlayLeg: '#2563eb',
    overlayHand: '#7c3aed'
  },
  handSwing: testerHandSwing
});

const rightRig = buildSteperRig({
  label: 'Right foot planted',
  fixedSide: 'right',
  fixedPoint: [22, groundY],
  movingStartPoint: [10, groundY],
  movingTargetPoint: [34, groundY],
  progress: triangleWave(timeValue * stepSpeed + 1),
  bobPhase: Math.PI,
  palette: {
    overlayLeg: '#f97316',
    overlayHand: '#f472b6'
  },
  handSwing: testerHandSwing
});

return {
  view,
  graphics: [...leftRig, ...rightRig]
};

function buildSteperRig(options) {
  const fixedSide = options.fixedSide === 'right' ? 'right' : 'left';
  const progress = clamp01(options.progress);
  const fixedPoint = ensurePoint(options.fixedPoint, [-8, groundY]);
  const movingStart = ensurePoint(options.movingStartPoint, [-16, groundY]);
  const movingTarget = ensurePoint(options.movingTargetPoint, [0, groundY]);
  const anchorHintX = averageNumbers([fixedPoint[0], movingStart[0], movingTarget[0]]);
  const bobPhase = typeof options.bobPhase === 'number' ? options.bobPhase : 0;
  const anchorBob = Math.sin(timeValue * 1.2 + bobPhase) * 0.45;
  const targetAnchorY = anchorBaseY + anchorBob;

  const hero = steperBuilder({
    fixedFeet: fixedSide,
    fixedFeetPoint: fixedPoint,
    movingFeetStartPoint: movingStart,
    movingFeetTargetPoint: movingTarget,
    progress,
    position: [anchorHintX, targetAnchorY],
    measurements: {
      torso: { direction: 'right' },
      head: { direction: 'right' },
      legs: {
        left: {
          upperLength: testingLegLengths.upper,
          lowerLength: testingLegLengths.lower
        },
        right: {
          upperLength: testingLegLengths.upper,
          lowerLength: testingLegLengths.lower
        }
      }
    },
    palette: options.palette,
    handSwing: options.handSwing
  });

  const figureGraphics = Array.isArray(hero.graphics) ? hero.graphics : [];
  const overlayGraphics = renderOverlays(hero.overlays);
  const stepMeta = hero.step ?? {};
  const anchorPoint = stepMeta.anchorPoint ?? hero.sequenceState?.position ?? [anchorHintX, targetAnchorY];
  const movingPoint = stepMeta.movingPoint ?? computeArcPoint(movingStart, movingTarget, progress);
  const stepFixedPoint = stepMeta.fixedPoint ?? fixedPoint;
  const rigBounds = resolveBounds([fixedPoint, movingStart, movingTarget, movingPoint]);
  const rigCenterX = (rigBounds.minX + rigBounds.maxX) / 2;

  const graphics = [
    createGroundLine(rigBounds.minX - 2, rigBounds.maxX + 2),
    ...figureGraphics,
    ...overlayGraphics,
    ...createArcGuide(movingStart, movingTarget),
    createMarker(stepFixedPoint, '#10b981', '#064e3b', 0.7),
    createMarker(movingStart, '#d946ef', '#701a75', 0.5),
    createMarker(movingTarget, '#22d3ee', '#0f766e', 0.5),
    createMarker(movingPoint, '#fb923c', '#c2410c', 0.6),
    ...createCross(anchorPoint, '#0284c7', 1.6),
    createFootLabel('fixed', stepFixedPoint, '#0f172a'),
    createFootLabel('start', movingStart, '#0f172a'),
    createFootLabel('target', movingTarget, '#0f172a'),
    {
      type: 'text',
      text: options.label,
      position: [rigCenterX, -5],
      fill: '#0f172a',
      fontSize: 2.8,
      align: 'center'
    },
    {
      type: 'text',
      text: `progress ${Math.round(progress * 100)}%`,
      position: [rigCenterX, -7],
      fill: '#475569',
      fontSize: 2.2,
      align: 'center'
    },
    isPoint(anchorPoint)
      ? {
          type: 'text',
          text: `anchor (${anchorPoint[0].toFixed(1)}, ${anchorPoint[1].toFixed(1)})`,
          position: [rigCenterX, -9],
          fill: '#475569',
          fontSize: 1.8,
          align: 'center'
        }
      : null
  ];

  return graphics.filter(Boolean);
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

function createArcGuide(start, end) {
  if (!isPoint(start) || !isPoint(end)) {
    return [];
  }
  const steps = 16;
  const pieces = [];
  let previous = computeArcPoint(start, end, 0);
  for (let i = 1; i <= steps; i++) {
    const progress = i / steps;
    const current = computeArcPoint(start, end, progress);
    pieces.push({
      type: 'line',
      from: previous,
      to: current,
      stroke: '#f97316',
      width: 0.24,
      dash: [0.6, 0.4]
    });
    previous = current;
  }
  return pieces;
}

function computeArcPoint(start, end, progress) {
  const clamped = clamp01(progress);
  const base = lerpPoint(start, end, clamped);
  const height = resolveArcHeight(start, end);
  const lift = Math.sin(Math.PI * clamped) * height;
  return [base[0], base[1] + lift];
}

function resolveArcHeight(start, end) {
  const span = measureDistance(start, end);
  return Math.max(1.5, span * 0.25);
}

function lerpPoint(start, end, t) {
  return [start[0] + (end[0] - start[0]) * t, start[1] + (end[1] - start[1]) * t];
}

function createMarker(point, fill, stroke, radius = 0.5) {
  if (!isPoint(point)) {
    return null;
  }
  return {
    type: 'circle',
    center: point,
    radius,
    fill,
    stroke,
    width: Math.max(0.16, radius * 0.3)
  };
}

function createFootLabel(text, point, color) {
  if (!isPoint(point)) {
    return null;
  }
  return {
    type: 'text',
    text,
    position: [point[0], point[1] - 1.7],
    fill: color,
    fontSize: 1.6,
    align: 'center'
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

function ensurePoint(value, fallback) {
  if (Array.isArray(value) && value.length >= 2 && Number.isFinite(value[0]) && Number.isFinite(value[1])) {
    return [value[0], value[1]];
  }
  return Array.isArray(fallback) ? [...fallback] : [0, 0];
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

function triangleWave(value) {
  const cycle = positiveMod(value, 2);
  return cycle <= 1 ? cycle : 2 - cycle;
}

function positiveMod(value, modulus) {
  if (!Number.isFinite(value) || !Number.isFinite(modulus) || modulus === 0) {
    return 0;
  }
  let result = value % modulus;
  if (result < 0) {
    result += modulus;
  }
  return result;
}

function averageNumbers(values) {
  let sum = 0;
  let count = 0;
  for (const value of values) {
    if (typeof value === 'number' && Number.isFinite(value)) {
      sum += value;
      count += 1;
    }
  }
  return count > 0 ? sum / count : 0;
}

function resolveBounds(points) {
  let minX = Infinity;
  let maxX = -Infinity;
  for (const point of points) {
    if (!isPoint(point)) {
      continue;
    }
    minX = Math.min(minX, point[0]);
    maxX = Math.max(maxX, point[0]);
  }
  if (!Number.isFinite(minX) || !Number.isFinite(maxX)) {
    return { minX: -10, maxX: 10 };
  }
  return { minX, maxX };
}

function fallbackDistance(a, b) {
  if (!isPoint(a) || !isPoint(b)) {
    return 0;
  }
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  return Math.sqrt(dx * dx + dy * dy);
}

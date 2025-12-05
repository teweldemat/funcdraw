const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickman = cartoonLibrary?.stickman ?? {};
const consts = typeof constants === 'object' && constants ? constants : {};

const steperManProfile = typeof stickman?.steperManProfile === 'function' ? stickman.steperManProfile : null;
const staticMan = typeof stickman?.static === 'function' ? stickman.static : null;

const view =
  consts.profileTester?.view ??
  consts.zoomedInView ??
  consts.view ??
  { left: -120, right: 120, bottom: -40, top: 140 };

if (!steperManProfile || !staticMan) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'steperManProfile/static not available – install @funcdraw/testlib',
        position: [0, 8],
        align: 'center',
        fontSize: 12,
        fill: '#ef4444'
      }
    ]
  };
}

// Simple, static inputs to illustrate the API.
const progress = clamp01(typeof t === 'number' ? t : 0);
const anchor = consts.profileTester?.anchor ?? [0, 18.6];
const movingSide = 'left';
const legLengths = consts.profileTester?.legLengths ?? { upper: 12.4, lower: 11.6 };
const leftOffset = consts.profileTester?.leftOffset ?? [-4, -18.6];
const rightOffset = consts.profileTester?.rightOffset ?? [4, -18.6];
const fixedFoot = addPoints(anchor, leftOffset);
const movingStart = addPoints(anchor, rightOffset);
const movingTarget = consts.profileTester?.movingTarget ?? [12, 0];

const baseMeasurements = {
  torso: { direction: 'right', height: consts.shared?.torso?.height ?? 22, width: consts.shared?.torso?.width ?? 12 },
  head: { direction: 'right', verticalExtent: consts.shared?.head?.verticalExtent ?? 9 },
  hands: {
    left: { effectorCoordinate: consts.shared?.hands?.left ?? [-7.8, 4.7] },
    right: { effectorCoordinate: consts.shared?.hands?.right ?? [7.8, 4.7] }
  },
  legs: {
    left: { upperLength: legLengths.upper, lowerLength: legLengths.lower, effectorCoordinate: leftOffset },
    right: { upperLength: legLengths.upper, lowerLength: legLengths.lower, effectorCoordinate: rightOffset }
  }
};

const stepPose = steperManProfile({
  position: anchor,
  measurements: baseMeasurements,
  movingSide,
  movingFeetTargetPoint: movingTarget,
  progress,
});

const poseAnchor = isPoint(stepPose.position) ? stepPose.position : anchor;
const poseMeasurements = stepPose.measurements || baseMeasurements;
const posed = staticMan({ position: poseAnchor, measurements: poseMeasurements }) || {};

const movingPoint = isPoint(stepPose.step?.movingPoint)
  ? stepPose.step.movingPoint
  : computeArcPoint(movingStart, movingTarget, progress);

return {
  view,
  graphics: [
    createGroundLine(view.left, view.right),
    ...ensureArray(posed.graphics),
    createMarker(fixedFoot, '#10b981'),
    createMarker(movingStart, '#94a3b8'),
    createMarker(movingTarget, '#facc15'),
    createMarker(movingPoint, '#fb923c'),
    ...createCross(poseAnchor, '#0284c7', 1.2),
    createLabel(`progress ${Math.round(progress * 100)}%`, [0, view.bottom + 2]),
    createLabel(`movingSide: ${movingSide}`, [0, view.bottom + 5]),
    createLabel('Inputs: anchor + measurements + movingSide + movingFeetTargetPoint + progress', [0, view.top - 2], 1.6)
  ].filter(Boolean)
};

function createGroundLine(minX, maxX) {
  return { type: 'line', from: [minX, 0], to: [maxX, 0], stroke: '#94a3b8', width: 0.5 };
}

function createMarker(point, fill = '#0f172a', radius = 0.6) {
  if (!isPoint(point)) return null;
  return { type: 'circle', center: point, radius, fill, stroke: '#0f172a', width: Math.max(0.16, radius * 0.3) };
}

function createCross(center, color, size = 1.2) {
  if (!isPoint(center)) return [];
  const half = size * 0.5;
  const width = Math.max(0.2, size * 0.12);
  return [
    { type: 'line', from: [center[0] - half, center[1]], to: [center[0] + half, center[1]], stroke: color, width },
    { type: 'line', from: [center[0], center[1] - half], to: [center[0], center[1] + half], stroke: color, width }
  ];
}

function createLabel(text, position, fontSize = 12) {
  return { type: 'text', text, position, fill: '#0f172a', fontSize, align: 'center' };
}

function ensureArray(value) {
  return Array.isArray(value) ? value : [];
}

function addPoints(a, b) {
  return [a[0] + b[0], a[1] + b[1]];
}

function computeArcPoint(start, end, progress) {
  const clamped = clamp01(progress);
  const base = lerpPoint(start, end, clamped);
  const height = Math.max(1.5, fallbackDistance(start, end) * 0.25);
  const lift = Math.sin(Math.PI * clamped) * height;
  return [base[0], base[1] + lift];
}

function lerpPoint(start, end, t) {
  return [start[0] + (end[0] - start[0]) * t, start[1] + (end[1] - start[1]) * t];
}

function clamp01(value) {
  if (typeof value !== 'number' || !Number.isFinite(value)) value = Number(value);
  if (!Number.isFinite(value)) return 0;
  if (value < 0) return 0;
  if (value > 1) return 1;
  return value;
}

function isPoint(value) {
  return Array.isArray(value) && value.length >= 2 && Number.isFinite(value[0]) && Number.isFinite(value[1]);
}

function fallbackDistance(a, b) {
  if (!isPoint(a) || !isPoint(b)) return 0;
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  return Math.sqrt(dx * dx + dy * dy);
}

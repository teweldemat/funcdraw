const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const hasSteperManProfile = typeof stickmanModule?.steperManProfile === 'function';
const staticBuilder = typeof stickmanModule?.static === 'function' ? stickmanModule.static : null;
const steperBuilder = hasSteperManProfile ? stickmanModule.steperManProfile : null;

const view = { left: -400, bottom: -300, right: 400, top: 300 };
const groundY = 0;
const anchorBaseY = 18.6;
const testingLegLengths = { upper: 12.4, lower: 11.6 };
const timeValue = typeof t === 'number' ? t : 0;

if (!steperBuilder || !staticBuilder) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'package("@funcdraw/testlib").cartoon.stickman.steperManProfile/static are unavailable.',
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

return {
  view,
  graphics: buildWalkingScene()
};

function buildWalkingScene() {
  const strideLength = 12;
  const laneMargin = 2;
  const startX = view.left + laneMargin;
  const endX = view.right - laneMargin;
  const usableSpan = Math.max(strideLength * 3, endX - startX);
  const maxStepIndex = Math.max(2, Math.floor(usableSpan / strideLength) - 1);
  const totalSteps = maxStepIndex + 1;
  const stepSpeed = 3;
  const rawStep = positiveMod(timeValue * stepSpeed, totalSteps);
  const stepIndex = Math.floor(rawStep);
  const stepProgress = rawStep - stepIndex;
  const progress = clamp01(stepProgress);

  const fixedPoint = [startX + stepIndex * strideLength, groundY];
  const movingStartPoint = [startX + (stepIndex - 1) * strideLength, groundY];
  const movingTargetPoint = [startX + (stepIndex + 1) * strideLength, groundY];
  const movingSide = stepIndex % 2 === 0 ? 'right' : 'left';
  const fixedSide = movingSide === 'left' ? 'right' : 'left';
  const anchorProgressBias = 1 - progress;
  // Keep the torso a touch forward/up at the start of each step so the walk feels lighter.
  const forwardLean = 0;
  const liftBias = 0.6 * anchorProgressBias;
  // Add a gentle rise/fall around the middle of each step to mimic cresting an incline.
  const midStepLift = Math.sin(Math.PI * progress) * 1.8;
  const anchorX = averageNumbers([fixedPoint[0], movingStartPoint[0]]) + forwardLean;
  const anchorBob = Math.sin(timeValue * 1.4) * 0.9;
  const anchorGuess = [anchorX, anchorBaseY + liftBias + midStepLift + anchorBob];

  const worldLeftFoot = fixedSide === 'left' ? fixedPoint : movingStartPoint;
  const worldRightFoot = fixedSide === 'right' ? fixedPoint : movingStartPoint;
  const leftOffset = [worldLeftFoot[0] - anchorGuess[0], worldLeftFoot[1] - anchorGuess[1]];
  const rightOffset = [worldRightFoot[0] - anchorGuess[0], worldRightFoot[1] - anchorGuess[1]];

  const baseMeasurements = {
    torso: { direction: 'right', height: 22, width: 12 },
    head: { direction: 'right', verticalExtent: 9 },
    hands: {
      left: { effectorCoordinate: [-7.8, 4.7] },
      right: { effectorCoordinate: [7.8, 4.7] }
    },
    legs: {
      left: { upperLength: testingLegLengths.upper, lowerLength: testingLegLengths.lower, effectorCoordinate: leftOffset },
      right: { upperLength: testingLegLengths.upper, lowerLength: testingLegLengths.lower, effectorCoordinate: rightOffset }
    }
  };

  const stepPose = steperBuilder({
    position: anchorGuess,
    measurements: baseMeasurements,
    movingSide,
    movingFeetTargetPoint: movingTargetPoint,
    progress,
    handSwing: {
      amplitude: 6,
      lift: 0.23,
      forwardOffset: 0,
      mode: 'sine'
    }
  });

  const posed = staticBuilder({
    position: isPoint(stepPose.position) ? stepPose.position : anchorGuess,
    measurements: stepPose.measurements || baseMeasurements,
    palette: {
      overlayLeg: '#f97316',
      overlayHand: '#0ea5e9'
    }
  }) || {};

  const overlayGraphics = renderOverlays(posed.overlays || stepPose.overlays);
  const walkerGraphics = ensureArray(posed.graphics);
  const stepMeta = stepPose.step || {};
  const movingPoint = isPoint(stepMeta.movingPoint) ? stepMeta.movingPoint : computeArcPoint(movingStartPoint, movingTargetPoint, progress);
  const fixedMarker = isPoint(stepMeta.fixedPoint) ? stepMeta.fixedPoint : fixedPoint;
  const routeProgress = clamp01((fixedPoint[0] - startX) / Math.max(1, endX - startX));

  return [
    createGroundLine(view.left, view.right),
    ...createWalkwayGuides(startX, endX, strideLength),
    ...walkerGraphics,
    ...overlayGraphics,
    createMarker(fixedMarker, '#10b981', '#064e3b', 0.55),
    createMarker(movingStartPoint, '#94a3b8', '#475569', 0.45),
    createMarker(movingTargetPoint, '#facc15', '#854d0e', 0.55),
    createMarker(movingPoint, '#fb923c', '#c2410c', 0.5),
    createProgressLabel(stepIndex, totalSteps, routeProgress)
  ].filter(Boolean);
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

function createWalkwayGuides(start, end, stride) {
  const guides = [];
  guides.push({
    type: 'line',
    from: [start, groundY - 0.8],
    to: [end, groundY - 0.8],
    stroke: '#cbd5f5',
    width: 0.24,
    dash: [1.1, 0.6]
  });
  guides.push({
    type: 'line',
    from: [start, groundY + 0.6],
    to: [end, groundY + 0.6],
    stroke: '#cbd5f5',
    width: 0.24,
    dash: [1.1, 0.6]
  });
  for (let markerX = start; markerX <= end + 0.001; markerX += stride) {
    guides.push({
      type: 'line',
      from: [markerX, groundY - 1.6],
      to: [markerX, groundY + 1.6],
      stroke: '#94a3b8',
      width: 0.2
    });
  }
  guides.push(createFlag([start, groundY + 2.4], 'start'));
  guides.push(createFlag([end, groundY + 2.4], 'finish'));
  return guides.filter(Boolean);
}

function createFlag(point, label) {
  if (!isPoint(point)) {
    return null;
  }
  return {
    type: 'text',
    text: label,
    position: [point[0], point[1]],
    fill: '#0f172a',
    fontSize: 12,
    align: 'center'
  };
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

function createProgressLabel(stepIndex, totalSteps, routeProgress) {
  return {
    type: 'text',
    text: `walking step ${stepIndex + 1}/${totalSteps} • ${Math.round(routeProgress * 100)}% across`,
    position: [0, view.top - 3],
    fill: '#0f172a',
    fontSize: 12,
    align: 'center'
  };
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

function computeArcPoint(start, end, progress) {
  const clamped = clamp01(progress);
  const base = [start[0] + (end[0] - start[0]) * clamped, start[1] + (end[1] - start[1]) * clamped];
  const span = Math.max(1.5, fallbackDistance(start, end) * 0.25);
  const lift = Math.sin(Math.PI * clamped) * span;
  return [base[0], base[1] + lift];
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

function ensureArray(value) {
  return Array.isArray(value) ? value : [];
}

function fallbackDistance(a, b) {
  if (!isPoint(a) || !isPoint(b)) return 0;
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  return Math.sqrt(dx * dx + dy * dy);
}

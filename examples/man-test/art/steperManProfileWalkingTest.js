const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const hasSteperManProfile = typeof stickmanModule?.steperManProfile === 'function';
const steperBuilder = hasSteperManProfile
  ? stickmanModule.steperManProfile
  : () => ({ graphics: [], overlays: [], skeleton: {}, step: {} });

const view = { left: -36, bottom: -12, right: 36, top: 32 };
const groundY = 0;
const anchorBaseY = 9.3;
const testingLegLengths = { upper: 6.2, lower: 5.8 };
const timeValue = typeof t === 'number' ? t : 0;

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

return {
  view,
  graphics: buildWalkingScene()
};

function buildWalkingScene() {
  const strideLength = 6;
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
  const fixedFeet = stepIndex % 2 === 0 ? 'left' : 'right';
  const anchorProgressBias = 1 - progress;
  // Keep the torso a touch forward/up at the start of each step so the walk feels lighter.
  const forwardLean = 0.9 * anchorProgressBias;
  const liftBias = 0.6 * anchorProgressBias;
  // Add a gentle rise/fall around the middle of each step to mimic cresting an incline.
  const midStepLift = Math.sin(Math.PI * progress) * 0.9;
  const anchorX = averageNumbers([fixedPoint[0], movingStartPoint[0], movingTargetPoint[0]]) + forwardLean;
  const anchorBob = Math.sin(timeValue * 1.4) * 0.45;

  const hero = steperBuilder({
    fixedFeet,
    fixedFeetPoint: fixedPoint,
    movingFeetStartPoint: movingStartPoint,
    movingFeetTargetPoint: movingTargetPoint,
    progress,
    position: [anchorX, anchorBaseY + liftBias + midStepLift + anchorBob],
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
    palette: {
      overlayLeg: '#f97316',
      overlayHand: '#0ea5e9'
    },
    handSwing: {
      amplitude: 6,
      lift: 0.23,
      forwardOffset: 0,
      mode: 'sine'
    }
  });

  const walkerGraphics = Array.isArray(hero.graphics) ? hero.graphics : [];
  const overlayGraphics = renderOverlays(hero.overlays);
  const routeProgress = clamp01((fixedPoint[0] - startX) / Math.max(1, endX - startX));

  return [
    createGroundLine(view.left, view.right),
    ...createWalkwayGuides(startX, endX, strideLength),
    ...walkerGraphics,
    ...overlayGraphics,
    createMarker(fixedPoint, '#10b981', '#064e3b', 0.55),
    createMarker(movingStartPoint, '#94a3b8', '#475569', 0.45),
    createMarker(movingTargetPoint, '#facc15', '#854d0e', 0.55),
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
    fontSize: 1.8,
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
    fontSize: 2.4,
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

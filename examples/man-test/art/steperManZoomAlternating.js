const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const helperModule = cartoonLibrary?.helpers ?? {};
const zoomBuilder = typeof stickmanModule?.steperManZoom === 'function'
  ? stickmanModule.steperManZoom
  : null;
const clamp01 = typeof helperModule?.clamp === 'function'
  ? (value) => helperModule.clamp(value, 0, 1)
  : (value) => {
      if (typeof value !== 'number' || !Number.isFinite(value)) value = Number(value);
      if (!Number.isFinite(value)) return 0;
      if (value < 0) return 0;
      if (value > 1) return 1;
      return value;
    };

const view = constants.zoomedInView;
const fontSize = typeof constants?.fontSize === 'number' ? constants.fontSize : 12;
const anchorBase = [0, 20];
const groundY = 0;
const timeValue = typeof t === 'number' ? t : 0;

if (!zoomBuilder) {
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

const direction = Math.floor(timeValue / 4) % 2 === 0 ? 'front' : 'back';
const stepDuration = 1;
const stepIndex = Math.floor(timeValue / stepDuration);
const stepPhase = clamp01((timeValue % stepDuration) / stepDuration);
const fixedSide = stepIndex % 2 === 0 ? 'left' : 'right';
const movingSide = fixedSide === 'left' ? 'right' : 'left';

const fixedBase = -24;
const stepRise = 2.4;
const movingSpan = 6;
const zoomPerStep = 0.8;
const cumulativeZoom = Math.pow(zoomPerStep, stepIndex + stepPhase);
const scaleForStep = (index) => Math.pow(zoomPerStep, Math.max(0, index));
const stepScaleStart = scaleForStep(stepIndex);
const movingStartPlan = (fixedBase + stepIndex * stepRise) * stepScaleStart;
const movingEndPlan = movingStartPlan + movingSpan * stepScaleStart;
// Keep the newly fixed foot at the previous step's landing depth instead of resetting.
const fixedPlan =
  stepIndex > 0
    ? (fixedBase + (stepIndex - 1) * stepRise + movingSpan) * scaleForStep(stepIndex - 1)
    : fixedBase * stepScaleStart;
// Carry the torso anchor forward so it doesn't snap back when the fixed foot swaps.
const anchorStartY =
  anchorBase[1] +
  accumulateAnchorOffset(stepIndex, fixedBase, stepRise, movingSpan, zoomPerStep);
const anchorPosition = [anchorBase[0], anchorStartY];
const fixedDepth = anchorBase[1] + fixedPlan - anchorStartY;
const movingStartY = anchorBase[1] + movingStartPlan - anchorStartY;
const movingEndY = anchorBase[1] + movingEndPlan - anchorStartY;

const hero = zoomBuilder({
  position: anchorPosition,
  measurements: {
    torso: { direction, height: 22, width: 12 },
    head: { direction, verticalExtent: 9 },
    hands: {
      left: { effectorCoordinate: [-7.8, 4.7] },
      right: { effectorCoordinate: [7.8, 4.7] }
    },
    legs: {
      left: { effectorCoordinate: fixedSide === 'left' ? fixedDepth : movingStartY },
      right: { effectorCoordinate: fixedSide === 'right' ? fixedDepth : movingStartY }
    }
  },
  movingSide,
  movingFootTargetY: anchorPosition[1] + movingEndY,
  zoom: cumulativeZoom,
  progress: stepPhase
});

const walkerGraphics = Array.isArray(hero.graphics) ? hero.graphics : [];
const overlayGraphics = renderOverlays(hero.overlays);
const anchorPoint = hero.step?.anchorPoint ?? hero.sequenceState?.position ?? anchorPosition;
const fixedPoint = hero.step?.fixedPoint;
const movingPoint = hero.step?.movingPoint;
const plannedLines = renderPlannedSteps(
  fixedBase,
  stepRise,
  movingSpan,
  6,
  anchorBase[1],
  zoomPerStep
);

return {
  view,
  graphics: [
    createGroundLine(view.left, view.right),
    ...plannedLines,
    ...walkerGraphics,
    ...overlayGraphics,
    createFootMarker(fixedPoint, '#22c55e'),
    createFootMarker(movingPoint, '#f97316'),
    ...createCross(anchorPoint, '#f59e0b', 1.4),
    createLabel(stepPhase, hero.step?.zoom ?? hero.step?.zoomFactor ?? cumulativeZoom, fixedSide, movingSide)
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
      width: 0.22,
      dash: [1.1, 0.7]
    }
  ];
}

function renderPlannedSteps(startBase, rise, span, count, anchorOffset = 0, zoomFactorValue = 1) {
  const graphics = [];
  for (let i = 0; i < count; i += 1) {
    const scale = Math.pow(zoomFactorValue, Math.max(0, i));
    const startY = anchorOffset + (startBase + i * rise) * scale;
    const endY = startY + span * scale;
    const alpha = 0.15 + 0.12 * (count - i);
    const startColor = `rgba(239,68,68,${Math.min(0.8, alpha)})`;
    const endColor = `rgba(249,115,22,${Math.min(0.8, alpha)})`;
    const fixedColor = `rgba(34,197,94,${Math.min(0.8, alpha)})`;
    // Guides show world Y positions where each step starts/lands.
    graphics.push(...createYGuide(startY, fixedColor)); // moving start depth for this step
    graphics.push(...createYGuide(endY, endColor));
  }
  return graphics;
}

function createFootMarker(point, color) {
  if (!isPoint(point)) return null;
  return {
    type: 'circle',
    center: point,
    radius: 0.45,
    fill: color,
    stroke: '#0f172a',
    width: 0.18
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

function createLabel(progressValue, zoomFactor, fixed, moving) {
  return {
    type: 'text',
    text: [
      `zoom stride`,
      `fixed ${fixed}`,
      `moving ${moving}`,
      `progress ${Math.round(progressValue * 100)}%`,
      `zoom factor ${zoomFactor.toFixed(2)}`
    ].join('  |  '),
    position: [0, view.top - fontSize * 1.5],
    fill: '#0f172a',
    fontSize,
    align: 'center'
  };
}

function accumulateAnchorOffset(stepCount, fixedBaseValue, stepRiseValue, spanValue, zoomTargetValue) {
  if (!Number.isInteger(stepCount) || stepCount <= 0) {
    return 0;
  }
  // Sum the end-of-step anchor drift for each completed step using the torso/feet distance rule.
  let offset = 0;
  for (let i = 0; i < stepCount; i += 1) {
    const scaleStart = Math.pow(zoomTargetValue, Math.max(0, i));
    const scalePrev = i > 0 ? Math.pow(zoomTargetValue, i - 1) : scaleStart;
    const movingStartPlan = (fixedBaseValue + i * stepRiseValue) * scaleStart;
    const movingEndPlan = movingStartPlan + spanValue * scaleStart;
    const fixedPlan =
      i > 0 ? (fixedBaseValue + (i - 1) * stepRiseValue + spanValue) * scalePrev : fixedBaseValue * scaleStart;
    const averageStart = (fixedPlan - offset + movingStartPlan - offset) * 0.5;
    const initialDistance = -averageStart;
    const averageEnd = (fixedPlan - offset + movingEndPlan - offset) * 0.5;
    const desiredDistanceEnd = initialDistance * (1 + (zoomTargetValue - 1)); // progress = 1
    const anchorDriftEnd = desiredDistanceEnd + averageEnd;
    offset += anchorDriftEnd;
  }
  return offset;
}

function lerp(a, b, t) {
  return a + (b - a) * t;
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

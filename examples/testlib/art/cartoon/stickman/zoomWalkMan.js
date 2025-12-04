const DEFAULT_POSITION = [0, 10];

function zoomWalkMan(optionsInput = {}) {
  const options = ensureObject(optionsInput);
  const anchorBase = toPoint(options.initialPosition, DEFAULT_POSITION);
  const measurementsInput = ensureObject(options.initialMeasurements);
  const depthDelta = toNumber(options.depthDelta, -12); // total vertical change (world Y) for moving foot
  const progress = clamp01(toNumber(options.progress, 0));
  const direction = normalizeDirection(options.direction, "front");
  const zoomTarget = Math.max(0, toNumber(options.zoom, 1));
  const stepper = steperManZoom;

  const defaultOffsets = resolveDefaultLegOffsets(measurementsInput);
  const defaultStride = resolveStrideLength(measurementsInput, defaultOffsets);
  const strideDirection = depthDelta >= 0 ? 1 : -1;
  const spacing = Math.max(1e-6, defaultStride);
  const overreach = Math.max(spacing * 0.25, 0.75);
  const passDistance = spacing + overreach;
  const stepCount = Math.max(1, Math.ceil(Math.abs(depthDelta) / passDistance));
  const totalProgress = progress * stepCount;
  const activeStepIndex = Math.min(stepCount - 1, Math.floor(totalProgress));
  const activeStepPhase = totalProgress - activeStepIndex;

  let currentAnchor = anchorBase;
  let currentMeasurements = mergeFacing(measurementsInput, direction);
  let remainingDepth = Math.abs(depthDelta);
  let leftFoot = addPoints(currentAnchor, resolveLegOffset(currentMeasurements?.legs?.left, defaultOffsets.left));
  let rightFoot = addPoints(currentAnchor, resolveLegOffset(currentMeasurements?.legs?.right, defaultOffsets.right));

  // pick moving side: move the deeper foot first when zooming in (negative delta), shallower when zooming out
  const movingSideInitial = strideDirection >= 0
    ? (leftFoot[1] >= rightFoot[1] ? "left" : "right")
    : (leftFoot[1] <= rightFoot[1] ? "left" : "right");
  const fixedSideInitial = movingSideInitial === "left" ? "right" : "left";

  for (let i = 0; i < stepCount; i += 1) {
    const stepFixedSide = i % 2 === 0 ? fixedSideInitial : movingSideInitial;
    const stepMovingSide = stepFixedSide === "left" ? "right" : "left";
    const isActiveStep = i === activeStepIndex;
    const stepProgress = i < activeStepIndex ? 1 : isActiveStep ? activeStepPhase : 0;
    const remainingSteps = Math.max(1, stepCount - i);

    const fixedY = stepFixedSide === "left" ? leftFoot[1] : rightFoot[1];
    let strideMagnitude;
    if (remainingSteps === 1) {
      strideMagnitude = remainingDepth;
    } else {
      const reserve = (remainingSteps - 1) * passDistance;
      const allowed = Math.max(passDistance, remainingDepth - reserve);
      strideMagnitude = Math.min(allowed, remainingDepth);
    }
    const targetY = fixedY + strideMagnitude * strideDirection;

    const movingStart = stepMovingSide === "left" ? leftFoot : rightFoot;
    const stepFixedPoint = stepFixedSide === "left" ? leftFoot : rightFoot;

    const measurementsWithOffsets = applyLegOffsets(currentMeasurements, currentAnchor, leftFoot, rightFoot, defaultOffsets);

    const zoomPhase = (i + stepProgress) / stepCount;
    const currentZoom = 1 + (zoomTarget - 1) * zoomPhase;

    const result = stepper({
      position: currentAnchor,
      measurements: measurementsWithOffsets,
      movingSide: stepMovingSide,
      movingFootTargetY: targetY,
      zoom: currentZoom,
      progress: stepProgress
    });

    const resolvedMeasurements = ensureObject(
      result?.measurements || result?.sequenceState?.measurements
    ) || measurementsWithOffsets;
    const resolvedPosition = toPoint(
      result?.position || result?.sequenceState?.position,
      currentAnchor
    );

    currentMeasurements = resolvedMeasurements;
    currentAnchor = resolvedPosition;

    leftFoot = addPoints(currentAnchor, resolveLegOffset(currentMeasurements?.legs?.left, defaultOffsets.left));
    rightFoot = addPoints(currentAnchor, resolveLegOffset(currentMeasurements?.legs?.right, defaultOffsets.right));
    remainingDepth = Math.max(0, remainingDepth - strideMagnitude);

    if (isActiveStep) {
      break;
    }
  }

  return {
    measurements: cloneValue(currentMeasurements),
    position: cloneValue(currentAnchor)
  };
}

function resolveLegOffset(measurement, fallback) {
  return toPoint(measurement?.effectorCoordinate, fallback);
}

function resolveDefaultLegOffsets(measurements, fallback = { left: [-2, -11], right: [2, -11] }) {
  const legs = ensureObject(measurements?.legs);
  return {
    left: toPoint(legs.left?.effectorCoordinate, fallback.left),
    right: toPoint(legs.right?.effectorCoordinate, fallback.right)
  };
}

function resolveStrideLength(measurements, defaults) {
  const legs = ensureObject(measurements?.legs);
  const left = ensureObject(legs.left);
  const right = ensureObject(legs.right);
  const leftLength = Math.max(0, toNumber(left.upperLength, 0) + toNumber(left.lowerLength, 0));
  const rightLength = Math.max(0, toNumber(right.upperLength, 0) + toNumber(right.lowerLength, 0));
  const avgLength = averageNumbers([leftLength, rightLength]);
  if (avgLength > 0) {
    return Math.max(1, avgLength * 0.35);
  }
  const defaultSpacing = Math.abs(defaults.right[1] - defaults.left[1]) || 1;
  return defaultSpacing;
}

function applyLegOffsets(measurements, anchor, leftFoot, rightFoot, defaults) {
  const base = ensureObject(measurements);
  const legs = ensureObject(base.legs);
  const leftBase = ensureObject(legs.left);
  const rightBase = ensureObject(legs.right);
  return {
    ...base,
    legs: {
      left: {
        ...leftBase,
        effectorCoordinate: subtractPoints(leftFoot, anchor) || defaults.left
      },
      right: {
        ...rightBase,
        effectorCoordinate: subtractPoints(rightFoot, anchor) || defaults.right
      }
    }
  };
}

function mergeFacing(measurements, direction) {
  const base = ensureObject(measurements);
  const torso = ensureObject(base.torso);
  const head = ensureObject(base.head);
  const resolvedDirection = normalizeDirection(torso.direction || head.direction, direction);
  return {
    ...base,
    torso: { ...torso, direction: resolvedDirection },
    head: { ...head, direction: resolvedDirection }
  };
}

function normalizeDirection(value, fallback = "front") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "front" || text === "back" || text === "left" || text === "right") {
    return text;
  }
  return fallback;
}

function clamp01(value) {
  const num = toNumber(value, 0);
  if (!Number.isFinite(num)) return 0;
  if (num < 0) return 0;
  if (num > 1) return 1;
  return num;
}

function toNumber(value, fallback = 0) {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toPoint(value, fallback = [0, 0]) {
  if (Array.isArray(value) && value.length >= 2) {
    return [toNumber(value[0], fallback[0]), toNumber(value[1], fallback[1])];
  }
  if (value && typeof value === "object") {
    if ("x" in value || "y" in value) {
      return [toNumber(value.x, fallback[0]), toNumber(value.y, fallback[1])];
    }
    if ("left" in value || "top" in value) {
      return [toNumber(value.left, fallback[0]), toNumber(value.top, fallback[1])];
    }
  }
  return Array.isArray(fallback) ? [...fallback] : [0, 0];
}

function addPoints(a, b) {
  return [toNumber(a[0], 0) + toNumber(b[0], 0), toNumber(a[1], 0) + toNumber(b[1], 0)];
}

function subtractPoints(a, b) {
  return [toNumber(a[0], 0) - toNumber(b[0], 0), toNumber(a[1], 0) - toNumber(b[1], 0)];
}

function averageNumbers(values) {
  let sum = 0;
  let count = 0;
  for (const value of values) {
    if (typeof value === "number" && Number.isFinite(value)) {
      sum += value;
      count += 1;
    }
  }
  return count > 0 ? sum / count : 0;
}

function ensureObject(value) {
  return value && typeof value === "object" ? value : {};
}

function cloneValue(value) {
  if (Array.isArray(value)) {
    return value.map(cloneValue);
  }
  if (value && typeof value === "object") {
    const clone = {};
    for (const key of Object.keys(value)) {
      clone[key] = cloneValue(value[key]);
    }
    return clone;
  }
  return value;
}



if (typeof module !== "undefined") {
  module.exports = zoomWalkMan;
}

return zoomWalkMan;

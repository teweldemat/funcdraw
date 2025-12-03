const DEFAULT_POSITION = [0, 10];
const DEFAULT_LEFT_OFFSET = [-2, -11];
const DEFAULT_RIGHT_OFFSET = [2, -11];

function sideWalkMan(optionsInput = {}) {
  const options = ensureObject(optionsInput);
  const anchorBase = toPoint(options.initialPosition, DEFAULT_POSITION);
  const measurementsInput = ensureObject(options.initialMeasurements);
  const displacement = toNumber(options.displacement, 0);
  const progress = clamp01(toNumber(options.progress, 0));
  const direction = normalizeDirection(options.direction, "right");
  const strideDirection = displacement >= 0 ? 1 : -1;

  const defaultOffsets = {
    left: DEFAULT_LEFT_OFFSET,
    right: DEFAULT_RIGHT_OFFSET
  };
  const initialLeftFoot = addPoints(anchorBase, resolveLegOffset(measurementsInput?.legs?.left, defaultOffsets.left));
  const initialRightFoot = addPoints(anchorBase, resolveLegOffset(measurementsInput?.legs?.right, defaultOffsets.right));
  const movingSide = strideDirection >= 0
    ? (initialLeftFoot[0] <= initialRightFoot[0] ? "left" : "right") // move the back foot when heading right
    : (initialLeftFoot[0] >= initialRightFoot[0] ? "left" : "right"); // move the back foot when heading left
  const fixedSide = movingSide === "left" ? "right" : "left"; // plant the front foot first
  const fixedOffset = resolveLegOffset(measurementsInput?.legs?.[fixedSide], defaultOffsets[fixedSide]);
  const movingOffset = resolveLegOffset(measurementsInput?.legs?.[movingSide], defaultOffsets[movingSide]);

  const fixedPoint = addPoints(anchorBase, fixedOffset);
  const movingStartPoint = addPoints(anchorBase, movingOffset);
  const movingTargetPoint = [movingStartPoint[0] + displacement, movingStartPoint[1]];
  const strideOverride = toNumber(options.strideLength, NaN);
  const strideLength = Number.isFinite(strideOverride) && strideOverride > 0
    ? strideOverride
    : resolveStrideLength(measurementsInput, defaultOffsets);
  const spacing = Math.max(1e-6, strideLength);
  const overreach = Math.max(spacing * 0.25, 0.75);
  const passDistance = spacing + overreach; // mid-step stride target (passes the fixed foot)
  const stepCount = Math.max(1, Math.ceil(Math.abs(displacement) / passDistance));
  const totalProgress = progress * stepCount;
  const activeStepIndex = Math.min(stepCount - 1, Math.floor(totalProgress));
  const activeStepPhase = totalProgress - activeStepIndex;

  const mergedMeasurements = mergeFacing(measurementsInput, direction);
  const stepper = typeof steperManProfile === "function" ? steperManProfile : null;
  if (!stepper) {
    return { measurements: mergedMeasurements, position: anchorBase };
  }

  let leftFoot = addPoints(anchorBase, resolveLegOffset(measurementsInput?.legs?.left, defaultOffsets.left));
  let rightFoot = addPoints(anchorBase, resolveLegOffset(measurementsInput?.legs?.right, defaultOffsets.right));
  let currentAnchor = anchorBase;
  let currentMeasurements = mergedMeasurements;
  let remainingDistance = Math.abs(displacement);

  for (let i = 0; i < stepCount; i += 1) {
    const stepFixedSide = i % 2 === 0 ? fixedSide : movingSide;
    const stepMovingSide = stepFixedSide === "left" ? "right" : "left";
    const isActiveStep = i === activeStepIndex;
    const stepProgress = i < activeStepIndex ? 1 : isActiveStep ? activeStepPhase : 0;
    const remainingSteps = Math.max(1, stepCount - i);
    const fixedX = stepFixedSide === "left" ? leftFoot[0] : rightFoot[0];
    const movingX = stepMovingSide === "left" ? leftFoot[0] : rightFoot[0];

    let strideMagnitude;
    if (remainingSteps === 1) {
      strideMagnitude = remainingDistance;
    } else {
      const reserve = (remainingSteps - 1) * passDistance;
      const allowed = Math.max(passDistance, remainingDistance - reserve);
      strideMagnitude = Math.min(allowed, remainingDistance);
    }
    const targetX = fixedX + strideMagnitude * strideDirection;

    const movingStart = stepMovingSide === "left" ? leftFoot : rightFoot;
    const movingTarget = [targetX, movingStart[1]];
    const stepFixedPoint = stepFixedSide === "left" ? leftFoot : rightFoot;

    const measurementsWithOffsets = applyLegOffsets(currentMeasurements, currentAnchor, leftFoot, rightFoot, defaultOffsets);

    const result = stepper({
      position: currentAnchor,
      measurements: measurementsWithOffsets,
      handSwing: options.handSwing,
      movingSide: stepMovingSide,
      movingFeetTargetPoint: movingTarget,
      progress: stepProgress
    });

    currentMeasurements = ensureObject(result?.measurements) || measurementsWithOffsets;
    currentAnchor = toPoint(result?.position, currentAnchor);

    leftFoot = addPoints(currentAnchor, resolveLegOffset(currentMeasurements?.legs?.left, defaultOffsets.left));
    rightFoot = addPoints(currentAnchor, resolveLegOffset(currentMeasurements?.legs?.right, defaultOffsets.right));
    remainingDistance = Math.max(0, remainingDistance - strideMagnitude);

    // Stop after the active step so we don't override its pose with future zero-progress steps.
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
  const defaultSpacing = Math.abs(defaults.right[0] - defaults.left[0]) || 1;
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

function normalizeDirection(value, fallback = "right") {
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

return sideWalkMan;

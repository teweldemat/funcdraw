const DEFAULT_POSITION = [0, 10];
const DEFAULT_LEFT_LEG_OFFSET = [-2, -11];
const DEFAULT_RIGHT_LEG_OFFSET = [2, -11];
const DEFAULT_LEFT_HAND_OFFSET = [-3.9, 2.35];
const DEFAULT_RIGHT_HAND_OFFSET = [3.9, 2.35];
const DEFAULT_TORSO_WIDTH = 6;
const DEFAULT_TORSO_HEIGHT = 11;
const DEFAULT_HEAD_HEIGHT = 4.5;
const DEFAULT_ARM_UPPER_LENGTH = 4;
const DEFAULT_ARM_LOWER_LENGTH = 3;
const DEFAULT_LEG_UPPER_LENGTH = 5.2;
const DEFAULT_LEG_LOWER_LENGTH = 4.8;
const DEFAULT_SWING = 0.25; // 25% depth swing for legs by default
const TWO_PI = Math.PI * 2;

const baseStaticBuilder = typeof staticMan === "function" ? staticMan : () => ({ graphics: [] });
const baseSkeleton = typeof baseStaticBuilder.skeleton === "function" ? baseStaticBuilder.skeleton() : null;
const skeletonPosition = isPoint(baseSkeleton?.position) ? baseSkeleton.position : DEFAULT_POSITION;
const defaultLeftFoot = isPoint(baseSkeleton?.legs?.left?.targetPoint)
  ? baseSkeleton.legs.left.targetPoint
  : addPoints(skeletonPosition, DEFAULT_LEFT_LEG_OFFSET);
const defaultRightFoot = isPoint(baseSkeleton?.legs?.right?.targetPoint)
  ? baseSkeleton.legs.right.targetPoint
  : addPoints(skeletonPosition, DEFAULT_RIGHT_LEG_OFFSET);
const defaultLeftOffset = subtractPoints(defaultLeftFoot, skeletonPosition);
const defaultRightOffset = subtractPoints(defaultRightFoot, skeletonPosition);
const defaultOffsetsBySide = {
  left: defaultLeftOffset,
  right: defaultRightOffset
};
const defaultLeftHandPoint = isPoint(baseSkeleton?.hands?.left?.targetPoint)
  ? baseSkeleton.hands.left.targetPoint
  : addPoints(skeletonPosition, DEFAULT_LEFT_HAND_OFFSET);
const defaultRightHandPoint = isPoint(baseSkeleton?.hands?.right?.targetPoint)
  ? baseSkeleton.hands.right.targetPoint
  : addPoints(skeletonPosition, DEFAULT_RIGHT_HAND_OFFSET);
const defaultLeftHandOffset = subtractPoints(defaultLeftHandPoint, skeletonPosition);
const defaultRightHandOffset = subtractPoints(defaultRightHandPoint, skeletonPosition);
const defaultHandOffsetsBySide = {
  left: defaultLeftHandOffset,
  right: defaultRightHandOffset
};
const defaultTorsoMeasurements = ensureObject(baseSkeleton?.torso);
const defaultTorsoWidth = toNumber(defaultTorsoMeasurements.width, DEFAULT_TORSO_WIDTH);
const defaultTorsoHeight = toNumber(defaultTorsoMeasurements.height, DEFAULT_TORSO_HEIGHT);
const defaultShoulderExtension = Math.max(
  toNumber(defaultTorsoMeasurements.shoulderExtension, defaultTorsoWidth * 0.15),
  0
);
const defaultHandLengthsBySide = {
  left: {
    upper: toNumber(baseSkeleton?.hands?.left?.lengths?.upper, DEFAULT_ARM_UPPER_LENGTH),
    lower: toNumber(baseSkeleton?.hands?.left?.lengths?.lower, DEFAULT_ARM_LOWER_LENGTH)
  },
  right: {
    upper: toNumber(baseSkeleton?.hands?.right?.lengths?.upper, DEFAULT_ARM_UPPER_LENGTH),
    lower: toNumber(baseSkeleton?.hands?.right?.lengths?.lower, DEFAULT_ARM_LOWER_LENGTH)
  }
};
const defaultLegLengthsBySide = {
  left: {
    upper: toNumber(baseSkeleton?.legs?.left?.lengths?.upper, DEFAULT_LEG_UPPER_LENGTH),
    lower: toNumber(baseSkeleton?.legs?.left?.lengths?.lower, DEFAULT_LEG_LOWER_LENGTH)
  },
  right: {
    upper: toNumber(baseSkeleton?.legs?.right?.lengths?.upper, DEFAULT_LEG_UPPER_LENGTH),
    lower: toNumber(baseSkeleton?.legs?.right?.lengths?.lower, DEFAULT_LEG_LOWER_LENGTH)
  }
};

function steperManZoom(optionsInput = {}) {
  const options = ensureObject(optionsInput);
  const measurementInput = ensureObject(options.measurements || options.baseMeasurements);
  const progress = clamp01(toNumber(options.progress, 0));
  const zoomProgress = clamp01(toNumber(options.zoomProgress, progress));
  const zoomFactor = Math.max(0, toNumber(options.zoomFactor, 1));
  const bodyScale = lerp(1, zoomFactor, zoomProgress);
  const swingAmount = clamp01(toNumber(options.swing, DEFAULT_SWING));
  const handSwingAmount = clamp01(toNumber(options.handSwing, swingAmount * 0.6));
  const phase = progress * TWO_PI;
  const torsoBase = ensureObject(measurementInput.torso);
  const headBase = ensureObject(measurementInput.head);
  const direction = normalizeDirection(
    options.direction || torsoBase.direction || headBase.direction || baseSkeleton?.torso?.direction,
    "front"
  );
  const anchorPoint = toPoint(options.position, skeletonPosition);

  const legOffsets = {
    left: readEffectorOffset(measurementInput?.legs?.left, defaultOffsetsBySide.left),
    right: readEffectorOffset(measurementInput?.legs?.right, defaultOffsetsBySide.right)
  };
  const handOffsets = {
    left: readEffectorOffset(measurementInput?.hands?.left, defaultHandOffsetsBySide.left),
    right: readEffectorOffset(measurementInput?.hands?.right, defaultHandOffsetsBySide.right)
  };

  const legLengths = {
    left: scaleLimbLengths(readLimbLengths(measurementInput?.legs?.left, defaultLegLengthsBySide.left), bodyScale),
    right: scaleLimbLengths(readLimbLengths(measurementInput?.legs?.right, defaultLegLengthsBySide.right), bodyScale)
  };
  const handLengths = {
    left: scaleLimbLengths(readLimbLengths(measurementInput?.hands?.left, defaultHandLengthsBySide.left), bodyScale),
    right: scaleLimbLengths(readLimbLengths(measurementInput?.hands?.right, defaultHandLengthsBySide.right), bodyScale)
  };

  const legEffectors = computeSwingOffsets(legOffsets, bodyScale, swingAmount, phase, {
    left: legLengths.left.upper + legLengths.left.lower,
    right: legLengths.right.upper + legLengths.right.lower
  });
  const handEffectors = computeSwingOffsets(handOffsets, bodyScale, handSwingAmount, phase, {
    left: handLengths.left.upper + handLengths.left.lower,
    right: handLengths.right.upper + handLengths.right.lower
  });

  const resolvedLegs = resolveStraightLimbs(legEffectors, legLengths, { left: true, right: true });
  const resolvedHands = resolveStraightLimbs(handEffectors, handLengths, {
    left: toBoolean(measurementInput?.hands?.left?.positiveBend, false),
    right: toBoolean(measurementInput?.hands?.right?.positiveBend, true)
  });

  const legsBase = ensureObject(measurementInput.legs);
  const handsBase = ensureObject(measurementInput.hands);

  const updatedMeasurements = {
    ...measurementInput,
    torso: {
      ...torsoBase,
      width: toNumber(torsoBase.width, defaultTorsoWidth) * bodyScale,
      height: toNumber(torsoBase.height, defaultTorsoHeight) * bodyScale,
      shoulderExtension: Math.max(toNumber(torsoBase.shoulderExtension, defaultShoulderExtension), 0) * bodyScale,
      direction
    },
    head: {
      ...headBase,
      verticalExtent: toNumber(headBase.verticalExtent, DEFAULT_HEAD_HEIGHT) * bodyScale,
      angle: toNumber(headBase.angle, 90),
      direction: normalizeDirection(headBase.direction, direction)
    },
    legs: {
      ...legsBase,
      left: buildLegSide("left"),
      right: buildLegSide("right")
    },
    hands: {
      ...handsBase,
      left: buildHandSide("left"),
      right: buildHandSide("right")
    }
  };

  const forwardedOptions = {
    ...options,
    position: anchorPoint,
    measurements: updatedMeasurements
  };
  delete forwardedOptions.progress;
  delete forwardedOptions.zoomProgress;
  delete forwardedOptions.zoomFactor;
  delete forwardedOptions.swing;
  delete forwardedOptions.handSwing;
  delete forwardedOptions.direction;
  delete forwardedOptions.baseMeasurements;

  const figure = baseStaticBuilder(forwardedOptions);
  figure.sequenceState = {
    position: [...anchorPoint],
    measurements: cloneValue(updatedMeasurements)
  };
  figure.step = {
    mode: "zoom",
    progress,
    zoomProgress,
    zoomFactor,
    swing: swingAmount,
    handSwing: handSwingAmount,
    anchorPoint,
    direction
  };
  return figure;

  function buildLegSide(side) {
    const base = ensureObject(legsBase[side]);
    const resolved = resolvedLegs[side];
    const baseFoot = ensureObject(base.foot);
    const scaledFootLength =
      typeof baseFoot.length === "number" && Number.isFinite(baseFoot.length)
        ? baseFoot.length * bodyScale
        : baseFoot.length;
    return {
      ...base,
      effectorCoordinate: resolved.effectorCoordinate,
      upperLength: resolved.upperLength,
      lowerLength: resolved.lowerLength,
      positiveBend: resolved.positiveBend,
      foot: Object.keys(baseFoot).length
        ? { ...baseFoot, length: scaledFootLength }
        : baseFoot
    };
  }

  function buildHandSide(side) {
    const base = ensureObject(handsBase[side]);
    const resolved = resolvedHands[side];
    return {
      ...base,
      effectorCoordinate: resolved.effectorCoordinate,
      upperLength: resolved.upperLength,
      lowerLength: resolved.lowerLength,
      positiveBend: resolved.positiveBend
    };
  }
}

function computeSwingOffsets(baseOffsets, bodyScale, swingAmount, phase, targetLengthBySide = null) {
  const result = {};
  for (const side of ["left", "right"]) {
    const base = toPoint(baseOffsets[side], [0, 0]);
    const wave = Math.sin(phase + (side === "right" ? Math.PI : 0));
    const depthScale = Math.max(0, 1 - wave * swingAmount);
    const raw = [
      base[0] * bodyScale,
      base[1] * bodyScale * depthScale
    ];
    const targetLength = targetLengthBySide && typeof targetLengthBySide[side] === "number"
      ? Math.max(1e-6, targetLengthBySide[side] * depthScale)
      : null;
    result[side] = targetLength ? scaleToLength(raw, targetLength) : raw;
  }
  return result;
}

function resolveStraightLimbs(effectorBySide, baseLengthsBySide, bendFallback = {}) {
  const result = {};
  for (const side of ["left", "right"]) {
    const effector = toPoint(effectorBySide[side], [0, 0]);
    const lengths = baseLengthsBySide[side] || { upper: 1, lower: 1 };
    const upperBase = toNumber(lengths.upper, 0);
    const lowerBase = toNumber(lengths.lower, 0);
    const totalBase = Math.max(1e-6, upperBase + lowerBase);
    const effectorLength = Math.max(1e-6, distanceFromOrigin(effector));
    const upperRatio = upperBase / totalBase;
    const lowerRatio = lowerBase / totalBase;
    const targetTotal = effectorLength;
    result[side] = {
      effectorCoordinate: effector,
      upperLength: targetTotal * upperRatio,
      lowerLength: targetTotal * lowerRatio,
      positiveBend: toBoolean(lengths.positiveBend, bendFallback[side])
    };
  }
  return result;
}

function readEffectorOffset(measurement, fallback) {
  return toPoint(measurement?.effectorCoordinate, fallback);
}

function readLimbLengths(measurement, defaults) {
  const upper = Math.max(0, toNumber(measurement?.upperLength, defaults?.upper));
  const lower = Math.max(0, toNumber(measurement?.lowerLength, defaults?.lower));
  const positiveBend = toBoolean(
    measurement?.positiveBend,
    measurement?.positiveBend === true || defaults?.positiveBend === true
  );
  return { upper, lower, positiveBend };
}

function scaleLimbLengths(lengths, scale) {
  const factor = Math.max(0, toNumber(scale, 1));
  return {
    upper: toNumber(lengths?.upper, 0) * factor,
    lower: toNumber(lengths?.lower, 0) * factor,
    positiveBend: toBoolean(lengths?.positiveBend, false)
  };
}

function normalizeDirection(value, fallback = "front") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "front" || text === "back" || text === "left" || text === "right") {
    return text;
  }
  return fallback;
}

function isPoint(value) {
  return (
    Array.isArray(value) &&
    value.length >= 2 &&
    typeof value[0] === "number" &&
    typeof value[1] === "number" &&
    Number.isFinite(value[0]) &&
    Number.isFinite(value[1])
  );
}

function toBoolean(value, fallback = false) {
  if (value === true) return true;
  if (value === false) return false;
  return !!fallback;
}

function clamp01(value) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    value = Number(value);
  }
  if (!Number.isFinite(value)) {
    return 0;
  }
  if (value < 0) return 0;
  if (value > 1) return 1;
  return value;
}

function toNumber(value, fallback = 0) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
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
  return [a[0] + b[0], a[1] + b[1]];
}

function subtractPoints(a, b) {
  return [a[0] - b[0], a[1] - b[1]];
}

function scaleToLength(point, targetLength) {
  const current = distanceFromOrigin(point);
  if (!Number.isFinite(current) || current < 1e-6) {
    return [0, -targetLength];
  }
  const scale = targetLength / current;
  return [point[0] * scale, point[1] * scale];
}

function distanceFromOrigin(point) {
  return Math.sqrt(point[0] * point[0] + point[1] * point[1]);
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

return steperManZoom;

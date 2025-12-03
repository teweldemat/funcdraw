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
  const measurementInput = ensureObject(options.measurements || options.initialMeasurements);
  const movingSide = normalizeSide(options.movingSide || options.movingFeet || options.movingFoot, "left");
  const fixedSide = movingSide === "left" ? "right" : "left";
  const progress = clamp01(toNumber(options.progress, 0));
  const zoomTarget = Math.max(0, toNumber(options.zoom ?? options.zoomFactor, 1));
  const zoomProgress = clamp01(toNumber(options.zoomProgress, 1));
  const bodyScale = lerp(1, zoomTarget, zoomProgress);
  const torsoBase = ensureObject(measurementInput.torso);
  const headBase = ensureObject(measurementInput.head);
  const anchorBase = toPoint(options.position, skeletonPosition);

  const legOffsets = {
    left: readEffectorOffset(measurementInput?.legs?.left, defaultOffsetsBySide.left),
    right: readEffectorOffset(measurementInput?.legs?.right, defaultOffsetsBySide.right)
  };
  const handOffsets = {
    left: readEffectorOffset(measurementInput?.hands?.left, defaultHandOffsetsBySide.left),
    right: readEffectorOffset(measurementInput?.hands?.right, defaultHandOffsetsBySide.right)
  };
  const legLengths = {
    left: readLimbLengths(measurementInput?.legs?.left, defaultLegLengthsBySide.left),
    right: readLimbLengths(measurementInput?.legs?.right, defaultLegLengthsBySide.right)
  };
  const handLengths = {
    left: readLimbLengths(measurementInput?.hands?.left, defaultHandLengthsBySide.left),
    right: readLimbLengths(measurementInput?.hands?.right, defaultHandLengthsBySide.right)
  };

  const fixedWorldY = anchorBase[1] + legOffsets[fixedSide][1];
  const movingStartWorldY = anchorBase[1] + legOffsets[movingSide][1];
  const movingTargetWorldY = toNumber(
    options.movingFootTargetY ?? options.movingFeetTargetY ?? options.targetY ?? movingStartWorldY,
    movingStartWorldY
  );
  const movingWorldY = lerp(movingStartWorldY, movingTargetWorldY, progress);
  const deltaY = movingWorldY - movingStartWorldY;
  const baseDirection = normalizeDirection(
    torsoBase.direction || headBase.direction || baseSkeleton?.torso?.direction,
    "front"
  );
  const direction = deltaY > 0 ? "back" : deltaY < 0 ? "front" : baseDirection;

  const averageStartY = (fixedWorldY + movingStartWorldY) * 0.5;
  const initialDistanceRaw = anchorBase[1] - averageStartY;
  const initialDistance = Math.abs(initialDistanceRaw) > 1e-6 ? initialDistanceRaw : defaultTorsoHeight;
  const distanceSign = initialDistance >= 0 ? 1 : -1;
  const averageCurrentY = (fixedWorldY + movingWorldY) * 0.5;
  const baseDistance = Math.max(Math.abs(initialDistance), defaultTorsoHeight);
  const desiredDistance = baseDistance * bodyScale * distanceSign;
  const anchorPoint = [anchorBase[0], averageCurrentY + desiredDistance];

  const torsoDimensions = resolveTorsoDimensions(torsoBase, bodyScale, direction);
  const attachments = resolveAttachments(torsoDimensions);
  const straighten = clamp01(Math.abs(zoomTarget - 1) * zoomProgress);

  const hipX = {
    left: Array.isArray(attachments.legs?.left) ? attachments.legs.left[0] : 0,
    right: Array.isArray(attachments.legs?.right) ? attachments.legs.right[0] : 0
  };

  const legEffectors = {
    [fixedSide]: [hipX[fixedSide], fixedWorldY - anchorPoint[1]],
    [movingSide]: [hipX[movingSide], movingWorldY - anchorPoint[1]]
  };

  const handEffectors = {
    left: [lerp(handOffsets.left[0] * bodyScale, 0, straighten), handOffsets.left[1] * bodyScale],
    right: [lerp(handOffsets.right[0] * bodyScale, 0, straighten), handOffsets.right[1] * bodyScale]
  };

  const legLengthsScaled = {
    left: scaleLengths(legLengths.left, bodyScale),
    right: scaleLengths(legLengths.right, bodyScale)
  };
  const handLengthsScaled = {
    left: scaleLengths(handLengths.left, bodyScale),
    right: scaleLengths(handLengths.right, bodyScale)
  };

  const resolvedLegs = resolveStraightLimbs(legEffectors, legLengthsScaled, { left: true, right: true }, attachments.legs);
  const resolvedHands = resolveStraightLimbs(
    handEffectors,
    handLengthsScaled,
    {
      left: toBoolean(measurementInput?.hands?.left?.positiveBend, false),
      right: toBoolean(measurementInput?.hands?.right?.positiveBend, true)
    },
    attachments.hands
  );

  const legsBase = ensureObject(measurementInput.legs);
  const handsBase = ensureObject(measurementInput.hands);

  const updatedMeasurements = {
    ...measurementInput,
    torso: {
      ...torsoBase,
      width: torsoDimensions.width,
      height: torsoDimensions.height,
      shoulderExtension: torsoDimensions.shoulderExtension,
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

  const figure = baseStaticBuilder({
    position: anchorPoint,
    measurements: updatedMeasurements
  });
  figure.sequenceState = {
    position: [...anchorPoint],
    measurements: cloneValue(updatedMeasurements)
  };
  figure.step = {
    mode: "zoom",
    progress,
    zoomProgress,
    zoom: zoomTarget,
    zoomFactor: zoomTarget,
    anchorPoint,
    direction,
    fixedSide,
    movingSide,
    fixedPoint: addPoints(anchorPoint, legEffectors[fixedSide]),
    movingPoint: addPoints(anchorPoint, legEffectors[movingSide])
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

function resolveStraightLimbs(effectorBySide, lengthsBySide, bendFallback = {}, attachmentOffsets = null) {
  const result = {};
  for (const side of ["left", "right"]) {
    const effector = toPoint(effectorBySide[side], [0, 0]);
    const attachment = attachmentOffsets && toPoint(attachmentOffsets[side], [0, 0]);
    const dx = attachment ? effector[0] - attachment[0] : effector[0];
    const dy = attachment ? effector[1] - attachment[1] : effector[1];
    const effectorLength = Math.max(1e-6, Math.sqrt(dx * dx + dy * dy));
    const lengths = lengthsBySide[side] || { upper: 1, lower: 1 };
    const upperBase = Math.max(0, toNumber(lengths.upper, 0));
    const lowerBase = Math.max(0, toNumber(lengths.lower, 0));
    const totalBase = Math.max(1e-6, upperBase + lowerBase);
    const upperRatio = upperBase / totalBase;
    const lowerRatio = lowerBase / totalBase;
    result[side] = {
      effectorCoordinate: effector,
      upperLength: effectorLength * upperRatio,
      lowerLength: effectorLength * lowerRatio,
      positiveBend: toBoolean(lengths.positiveBend, bendFallback[side])
    };
  }
  return result;
}

function readEffectorOffset(measurement, fallback) {
  const raw = measurement?.effectorCoordinate;
  if (typeof raw === "number") {
    const base = toPoint(fallback, [0, 0]);
    return [base[0], toNumber(raw, base[1])];
  }
  return toPoint(raw, fallback);
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

function scaleLengths(lengths, scale) {
  const factor = Math.max(0, toNumber(scale, 1));
  return {
    upper: toNumber(lengths?.upper, 0) * factor,
    lower: toNumber(lengths?.lower, 0) * factor,
    positiveBend: toBoolean(lengths?.positiveBend, false)
  };
}

function readDepth(input, fallbackY) {
  if (Array.isArray(input) && input.length > 1 && Number.isFinite(input[1])) {
    return toNumber(input[1], fallbackY);
  }
  if (input && typeof input === "object") {
    if (typeof input.y === "number") return toNumber(input.y, fallbackY);
    if (typeof input.top === "number") return toNumber(input.top, fallbackY);
  }
  return toNumber(fallbackY, 0);
}

function normalizeDirection(value, fallback = "front") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "front" || text === "back" || text === "left" || text === "right") {
    return text;
  }
  return fallback;
}

function normalizeSide(value, fallback = "left") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "left" || text === "right") {
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

function resolveTorsoDimensions(torsoBase, bodyScale, direction) {
  const width = toNumber(torsoBase.width, defaultTorsoWidth) * bodyScale;
  const height = toNumber(torsoBase.height, defaultTorsoHeight) * bodyScale;
  const shoulderExtension = Math.max(toNumber(torsoBase.shoulderExtension, defaultShoulderExtension), 0) * bodyScale;
  return { width, height, shoulderExtension, direction };
}

function resolveAttachments(torso) {
  const halfWidth = torso.width / 2;
  const handOffset = halfWidth + torso.shoulderExtension;
  const handsY = torso.height * 0.85;
  const legOffset = torso.width * 0.25;
  if (torso.direction === "left" || torso.direction === "right") {
    return {
      hands: { left: [0, handsY], right: [0, handsY] },
      legs: { left: [0, 0], right: [0, 0] }
    };
  }
  return {
    hands: {
      left: [-handOffset, handsY],
      right: [handOffset, handsY]
    },
    legs: {
      left: [-legOffset, 0],
      right: [legOffset, 0]
    }
  };
}

function scalePoint(point, scale) {
  return [point[0] * scale, point[1] * scale];
}

function distanceFromOrigin(point) {
  return Math.sqrt(point[0] * point[0] + point[1] * point[1]);
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

return steperManZoom;

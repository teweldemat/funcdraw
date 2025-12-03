const DEFAULT_POSITION = [0, 10];
const DEFAULT_LEFT_OFFSET = [-2, -11];
const DEFAULT_RIGHT_OFFSET = [2, -11];
const DEFAULT_HAND_FORWARD = 3.9;
const DEFAULT_HAND_DROP = 2.35;
const DEFAULT_LEFT_HAND_OFFSET = [-DEFAULT_HAND_FORWARD, DEFAULT_HAND_DROP];
const DEFAULT_RIGHT_HAND_OFFSET = [DEFAULT_HAND_FORWARD, DEFAULT_HAND_DROP];

const baseStaticBuilder = typeof staticMan === "function" ? staticMan : () => ({ graphics: [] });
const distanceBetweenPoints = typeof distance === "function" ? distance : fallbackDistance;
const baseSkeleton = typeof baseStaticBuilder.skeleton === "function" ? baseStaticBuilder.skeleton() : null;
const skeletonPosition = isPoint(baseSkeleton?.position) ? baseSkeleton.position : DEFAULT_POSITION;
const defaultLeftFoot = isPoint(baseSkeleton?.legs?.left?.targetPoint)
  ? baseSkeleton.legs.left.targetPoint
  : addPoints(skeletonPosition, DEFAULT_LEFT_OFFSET);
const defaultRightFoot = isPoint(baseSkeleton?.legs?.right?.targetPoint)
  ? baseSkeleton.legs.right.targetPoint
  : addPoints(skeletonPosition, DEFAULT_RIGHT_OFFSET);
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
const baseTorsoDirection = normalizeDirectionValue(baseSkeleton?.torso?.direction);
const baseTorsoMeasurements = ensureObject(baseSkeleton?.torso);
const DEFAULT_TORSO_WIDTH = toNumber(baseTorsoMeasurements.width, 6);
const DEFAULT_TORSO_HEIGHT = toNumber(baseTorsoMeasurements.height, 11);
const DEFAULT_SHOULDER_EXTENSION = Math.max(
  toNumber(baseTorsoMeasurements.shoulderExtension, DEFAULT_TORSO_WIDTH * 0.15),
  0
);
const defaultShoulderOffsetsBySide = resolveShoulderOffsets({
  width: DEFAULT_TORSO_WIDTH,
  height: DEFAULT_TORSO_HEIGHT,
  shoulderExtension: DEFAULT_SHOULDER_EXTENSION,
  direction: baseTorsoDirection
});
const defaultHandReachBySide = {
  left: resolveDefaultHandReach("left", defaultShoulderOffsetsBySide),
  right: resolveDefaultHandReach("right", defaultShoulderOffsetsBySide)
};
const MIN_REACH_RATIO = 0.9;
const MAX_VERTICAL_ANCHOR_DELTA = 1.2;

function steperManProfile(optionsInput = {}) {
  const options = ensureObject(optionsInput);
  const anchorBase = toPoint(options.position, DEFAULT_POSITION);
  const measurementInput = ensureObject(options.measurements);
  const progress = clamp01(toNumber(options.progress, 0));
  const movingSideFromOptions = normalizeSide(options.movingSide || options.movingFeet, null);
  const fixedSideFromOptions = normalizeSide(options.fixedFeet, null);
  const movingSide = movingSideFromOptions || (fixedSideFromOptions === "left" ? "right" : fixedSideFromOptions === "right" ? "left" : "left");
  const fixedSide = movingSide === "left" ? "right" : "left";

  const baseLegs = ensureObject(measurementInput.legs);
  const legOffsets = {
    left: toPoint(baseLegs.left?.effectorCoordinate, defaultOffsetsBySide.left),
    right: toPoint(baseLegs.right?.effectorCoordinate, defaultOffsetsBySide.right)
  };

  const defaultFixedWorld = addPoints(anchorBase, legOffsets[fixedSide]);
  const defaultMovingWorld = addPoints(anchorBase, legOffsets[movingSide]);

  const fixedWorld = toPoint(options.fixedFeetPoint || options.fixedFeetTargetPoint, defaultFixedWorld);
  const movingStartWorld = toPoint(options.movingFeetStartPoint || options.movingFeetStart, defaultMovingWorld);
  const movingTargetWorld = toPoint(
    options.movingFeetTargetPoint || options.movingFeetTarget || options.targetFeetPoint || options.targetFootPoint,
    movingStartWorld
  );
  const arcHeight = Number.isFinite(options.arcHeight) ? options.arcHeight : resolveArcHeight(movingStartWorld, movingTargetWorld);
  const movingWorld = computeArcPoint(movingStartWorld, movingTargetWorld, progress, arcHeight);

  const anchorCandidates = [
    subtractPoints(fixedWorld, legOffsets[fixedSide]),
    subtractPoints(movingWorld, legOffsets[movingSide])
  ];
  const averagedAnchor = averagePoints(anchorCandidates) ?? anchorBase;
  const anchorPosition = clampAnchorVerticalDrift(averagedAnchor, anchorBase);

  const updatedLegOffsets = {
    left: subtractPoints(fixedSide === "left" ? fixedWorld : movingWorld, anchorPosition),
    right: subtractPoints(fixedSide === "right" ? fixedWorld : movingWorld, anchorPosition)
  };

  const torsoMeasurements = ensureObject(measurementInput.torso);
  const headMeasurements = ensureObject(measurementInput.head);
  const baseHands = ensureObject(measurementInput.hands);
  const torsoDirection = normalizeDirectionValue(torsoMeasurements.direction || headMeasurements.direction, baseTorsoDirection);
  const swing = resolveHandSwingOptions(options.handSwing);

  const updatedMeasurements = {
    ...measurementInput,
    torso: { ...torsoMeasurements, direction: torsoDirection },
    head: { ...headMeasurements, direction: torsoDirection },
    legs: {
      ...baseLegs,
      left: { ...ensureObject(baseLegs.left), effectorCoordinate: updatedLegOffsets.left },
      right: { ...ensureObject(baseLegs.right), effectorCoordinate: updatedLegOffsets.right }
    }
  };

  const hands = applyHandSwing(baseHands, {
    swing,
    progress,
    torsoDirection,
    torsoMeasurements: updatedMeasurements.torso,
    movingSide,
    legOffsets: updatedLegOffsets
  });

  const finalMeasurements = {
    ...updatedMeasurements,
    hands
  };

  const staticResult = baseStaticBuilder({
    position: anchorPosition,
    measurements: finalMeasurements
  }) || {};

  const sequenceState = {
    position: cloneValue(anchorPosition),
    measurements: cloneValue(finalMeasurements)
  };

  const step = {
    fixedSide,
    movingSide,
    fixedPoint: cloneValue(fixedWorld),
    movingPoint: cloneValue(movingWorld),
    anchorPoint: cloneValue(anchorPosition),
    progress
  };

  return {
    ...staticResult,
    ...sequenceState,
    finalPosition: sequenceState.position,
    finalMeasurements: sequenceState.measurements,
    sequenceState,
    step
  };
}

function computeArcPoint(start, end, progress, height) {
  const t = clamp01(progress);
  const basePoint = lerpPoint(start, end, t);
  const lift = Math.sin(Math.PI * t) * height;
  return [basePoint[0], basePoint[1] + lift];
}

function resolveArcHeight(start, end) {
  const span = distanceBetweenPoints(start, end);
  return Math.max(1.5, span * 0.25);
}

function normalizeSide(value, fallback = "left") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "left" || text === "right") {
    return text;
  }
  return fallback;
}

function normalizeDirectionValue(value, fallback = "front") {
  if (typeof value === "string") {
    const text = value.trim().toLowerCase();
    if (text === "left" || text === "right" || text === "front" || text === "back") {
      return text;
    }
  }
  return fallback;
}

function lerpPoint(start, end, t) {
  return [
    start[0] + (end[0] - start[0]) * t,
    start[1] + (end[1] - start[1]) * t
  ];
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

function toPoint(value, fallback = DEFAULT_POSITION) {
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

function addPoints(a, b) {
  return [a[0] + b[0], a[1] + b[1]];
}

function subtractPoints(a, b) {
  return [a[0] - b[0], a[1] - b[1]];
}

function averagePoints(points) {
  if (!Array.isArray(points) || points.length === 0) {
    return null;
  }
  let sumX = 0;
  let sumY = 0;
  let count = 0;
  for (const point of points) {
    if (!isPoint(point)) {
      continue;
    }
    sumX += point[0];
    sumY += point[1];
    count += 1;
  }
  if (count === 0) {
    return null;
  }
  return [sumX / count, sumY / count];
}

function clampAnchorVerticalDrift(candidate, baseline) {
  if (!isPoint(candidate)) {
    return Array.isArray(baseline) ? [...baseline] : DEFAULT_POSITION.slice();
  }
  const reference = isPoint(baseline) ? baseline : DEFAULT_POSITION;
  const minY = reference[1] - MAX_VERTICAL_ANCHOR_DELTA;
  const maxY = reference[1] + MAX_VERTICAL_ANCHOR_DELTA;
  const clampedY = clampRange(candidate[1], minY, maxY);
  return [candidate[0], clampedY];
}

function clampRange(value, min, max) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return clampRange(0, min, max);
  }
  if (value < min) return min;
  if (value > max) return max;
  return value;
}

function fallbackDistance(a, b) {
  if (!isPoint(a) || !isPoint(b)) {
    return 0;
  }
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  return Math.sqrt(dx * dx + dy * dy);
}

function resolveHandSwingOptions(value) {
  const options = ensureObject(value);
  const enabled = options.enabled !== false;
  const amplitude = Math.max(0, toNumber(options.amplitude, 1.4));
  const lift = Math.max(0, toNumber(options.lift, 0.35));
  const forwardOffset = toNumber(options.forwardOffset, 0);
  const phase = toNumber(options.phase, 0);
  const mode = normalizeSwingMode(options.mode);
  return { enabled, amplitude, lift, forwardOffset, phase, mode };
}

function normalizeSwingMode(value) {
  if (typeof value === "string") {
    const text = value.trim().toLowerCase();
    if (text === "mirror" || text === "sine") {
      return text;
    }
  }
  return "mirror";
}

function resolveShoulderOffsetsFromContext(context) {
  const torso = ensureObject(context.torsoMeasurements);
  const width = toNumber(torso.width, DEFAULT_TORSO_WIDTH);
  const height = toNumber(torso.height, DEFAULT_TORSO_HEIGHT);
  const shoulderExtension = Math.max(
    toNumber(
      torso.shoulderExtension,
      DEFAULT_SHOULDER_EXTENSION != null ? DEFAULT_SHOULDER_EXTENSION : width * 0.15
    ),
    0
  );
  const direction = normalizeDirectionValue(torso.direction, context.torsoDirection);
  const resolved = resolveShoulderOffsets({ width, height, shoulderExtension, direction });
  return {
    left: resolved.left || defaultShoulderOffsetsBySide.left || [0, 0],
    right: resolved.right || defaultShoulderOffsetsBySide.right || [0, 0]
  };
}

function resolveShoulderOffsets(torsoMeasurements = {}) {
  const width = toNumber(torsoMeasurements.width, DEFAULT_TORSO_WIDTH);
  const height = toNumber(torsoMeasurements.height, DEFAULT_TORSO_HEIGHT);
  const shoulderExtension = Math.max(toNumber(torsoMeasurements.shoulderExtension, width * 0.15), 0);
  const direction = normalizeDirectionValue(torsoMeasurements.direction, baseTorsoDirection);
  const halfWidth = width / 2;
  const handsY = height * 0.85;
  const handOffset = halfWidth + shoulderExtension;
  if (direction === "left" || direction === "right") {
    const center = [0, handsY];
    return { left: center, right: center };
  }
  return {
    left: [-handOffset, handsY],
    right: [handOffset, handsY]
  };
}

function resolveDefaultHandReach(side, shoulderOffsets) {
  const baseHand = baseSkeleton?.hands?.[side];
  if (isPoint(baseHand?.attachmentPoint) && isPoint(baseHand?.targetPoint)) {
    return distanceBetweenPoints(baseHand.attachmentPoint, baseHand.targetPoint);
  }
  const lengths = ensureObject(baseHand?.lengths);
  const upper = toNumber(lengths.upper, NaN);
  const lower = toNumber(lengths.lower, NaN);
  if (Number.isFinite(upper) && Number.isFinite(lower)) {
    return Math.max(upper + lower, 0);
  }
  if (isPoint(shoulderOffsets?.[side]) && isPoint(defaultHandOffsetsBySide[side])) {
    const reach = distanceBetweenPoints(shoulderOffsets[side], defaultHandOffsetsBySide[side]);
    if (reach > 0) {
      return reach;
    }
  }
  return 7;
}

function resolveHandReachLength(side, shoulderOffsets, handMeasurements = null) {
  const baseReach = defaultHandReachBySide[side] || 0;
  const sideMeasurements = ensureObject(handMeasurements && handMeasurements[side]);
  const upper = toNumber(sideMeasurements.upperLength, NaN);
  const lower = toNumber(sideMeasurements.lowerLength, NaN);
  if (Number.isFinite(upper) && Number.isFinite(lower)) {
    const measured = Math.max(upper + lower, 0);
    if (measured > 0) {
      return measured;
    }
  }
  if (isPoint(shoulderOffsets?.[side]) && isPoint(defaultHandOffsetsBySide[side])) {
    const reach = distanceBetweenPoints(shoulderOffsets[side], defaultHandOffsetsBySide[side]);
    if (reach > 0) {
      return Math.max(baseReach, reach);
    }
  }
  return Math.max(baseReach, 1);
}

function constrainEffectorReach(effectorCoordinate, shoulderOffset, reach, baseLength = null, minRatio = MIN_REACH_RATIO) {
  const shoulder = isPoint(shoulderOffset) ? shoulderOffset : [0, 0];
  const targetReach = Math.max(toNumber(reach, 0), 1e-6);
  const dx = effectorCoordinate[0] - shoulder[0];
  const dy = effectorCoordinate[1] - shoulder[1];
  const distance = Math.sqrt(dx * dx + dy * dy) || 1;
  const resolvedBase = typeof baseLength === "number" && Number.isFinite(baseLength) ? Math.max(baseLength, 0) : targetReach;
  const minReach = clampRange(resolvedBase * clampRange(minRatio, 0, 1), 0, targetReach);
  const clampedDistance = clampRange(distance, minReach, targetReach);
  const scale = clampedDistance / distance;
  return [
    shoulder[0] + dx * scale,
    shoulder[1] + dy * scale
  ];
}

function applyHandSwing(handMeasurements, context) {
  const swing = context.swing;
  if (!swing.enabled) {
    return handMeasurements;
  }
  const baseHands = ensureObject(handMeasurements);
  const shoulderOffsets = resolveShoulderOffsetsFromContext(context);
  const reachBySide = {
    left: resolveHandReachLength("left", shoulderOffsets, baseHands),
    right: resolveHandReachLength("right", shoulderOffsets, baseHands)
  };
  const swingContext = {
    ...context,
    shoulderOffsets,
    reachBySide
  };
  if (swing.mode === "sine") {
    return applySineHandSwing(baseHands, swingContext);
  }
  return applyMirrorHandSwing(baseHands, swingContext);
}

function applySineHandSwing(baseHands, context) {
  const swing = context.swing;
  const angle = Math.PI * clamp01(context.progress) + swing.phase;
  const swingSignal = Math.cos(angle);
  const liftSignal = Math.sin(angle);
  const forwardSign = resolveForwardSign(context.torsoDirection);
  const amplitude = swing.amplitude * forwardSign;
  const liftAmount = swing.lift;
  const forwardOffset = swing.forwardOffset * forwardSign;
  const shoulderOffsets = ensureObject(context.shoulderOffsets);
  const reachBySide = ensureObject(context.reachBySide);

  const result = {
    ...baseHands,
    left: applySwingToSide("left", ensureObject(baseHands.left)),
    right: applySwingToSide("right", ensureObject(baseHands.right))
  };

  return result;

  function applySwingToSide(side, input) {
    const fallback = defaultHandOffsetsBySide[side] || [0, 0];
    const baseEffector = toPoint(input.effectorCoordinate, fallback);
    const shoulderOffset = shoulderOffsets[side] || defaultShoulderOffsetsBySide[side] || [0, 0];
    const reach = reachBySide[side]
      || defaultHandReachBySide[side]
      || distanceBetweenPoints(baseEffector, shoulderOffset);
    const isMoving = side === context.movingSide;
    const horizontalSwing = (isMoving ? swingSignal : -swingSignal) * amplitude + forwardOffset;
    const verticalSwing = (isMoving ? liftSignal : -liftSignal) * liftAmount;
    const candidate = [horizontalSwing, baseEffector[1] + verticalSwing];
    const targetEffector = scaleVectorToLength(candidate, shoulderOffset, reach);
    return {
      ...input,
      effectorCoordinate: targetEffector
    };
  }
}

function applyMirrorHandSwing(baseHands, context) {
  const swing = context.swing;
  const legOffsets = ensureObject(context.legOffsets);
  const forwardSign = resolveForwardSign(context.torsoDirection);
  const depthScale = resolveLegDepthScale(legOffsets);
  const averageY = resolveAverageY(legOffsets);

  const result = {
    ...baseHands,
    left: applySwingToSide("left", ensureObject(baseHands.left)),
    right: applySwingToSide("right", ensureObject(baseHands.right))
  };
  return result;

  function applySwingToSide(side, input) {
    const fallback = defaultHandOffsetsBySide[side] || [0, 0];
    const baseEffector = toPoint(input.effectorCoordinate, fallback);
    const mirroredLegSide = side === "left" ? "right" : "left";
    const sourceLeg = legOffsets[mirroredLegSide];

    if (!isPoint(sourceLeg)) {
      return {
        ...input,
        effectorCoordinate: baseEffector
      };
    }

    const radius = Math.max(1e-6, distanceBetweenPoints([0, 0], baseEffector));

    const normalizedHorizontal = clampSymmetric(sourceLeg[0] / depthScale);
    const normalizedVertical = clampSymmetric((sourceLeg[1] - averageY) / depthScale);

    const horizontalSwing =
      normalizedHorizontal * swing.amplitude * forwardSign +
      swing.forwardOffset * forwardSign;

    const verticalSwing = normalizedVertical * swing.lift;

    const candidateX = baseEffector[0] + horizontalSwing;
    const candidateY = baseEffector[1] + verticalSwing;

    const candidateLen = Math.sqrt(candidateX * candidateX + candidateY * candidateY) || 1;
    const scale = radius / candidateLen;

    return {
      ...input,
      effectorCoordinate: [candidateX * scale, candidateY * scale]
    };
  }
}

function resolveLegDepthScale(legOffsets) {
  let maxDepth = 0;
  for (const side of ["left", "right"]) {
    const leg = legOffsets[side];
    if (isPoint(leg)) {
      maxDepth = Math.max(maxDepth, Math.abs(leg[1]));
    }
  }
  return Math.max(1, maxDepth);
}

function resolveAverageY(legOffsets) {
  let sum = 0;
  let count = 0;
  for (const side of ["left", "right"]) {
    const leg = legOffsets[side];
    if (isPoint(leg)) {
      sum += leg[1];
      count += 1;
    }
  }
  return count > 0 ? sum / count : 0;
}

function clampSymmetric(value, limit = 1) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return 0;
  }
  if (value > limit) return limit;
  if (value < -limit) return -limit;
  return value;
}

function resolveForwardSign(direction) {
  return direction === "left" ? -1 : 1;
}

function scaleVectorToLength(point, origin, length) {
  const ox = Array.isArray(origin) ? toNumber(origin[0], 0) : 0;
  const oy = Array.isArray(origin) && origin.length > 1 ? toNumber(origin[1], 0) : 0;
  const dx = toNumber(point?.[0], 0) - ox;
  const dy = toNumber(point?.[1], 0) - oy;
  const distance = Math.sqrt(dx * dx + dy * dy);
  if (distance < 1e-6) {
    const target = Math.max(0, toNumber(length, 0));
    return [ox, oy - target];
  }
  const targetLength = Math.max(0, toNumber(length, 0));
  const scale = targetLength / distance;
  return [ox + dx * scale, oy + dy * scale];
}

return steperManProfile;

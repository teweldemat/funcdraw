const DEFAULT_TORSO_WIDTH = 6;
const DEFAULT_TORSO_HEIGHT = 11;
const DEFAULT_ARM_UPPER_LENGTH = 4;
const DEFAULT_ARM_LOWER_LENGTH = 3;
const DEFAULT_LEG_UPPER_LENGTH = 5.2;
const DEFAULT_LEG_LOWER_LENGTH = 4.8;
const DEFAULT_SHOULDER_EXTENSION = DEFAULT_TORSO_WIDTH * 0.15;
const DEFAULT_HAND_OFFSET = DEFAULT_TORSO_WIDTH / 2 + DEFAULT_SHOULDER_EXTENSION;
const DEFAULT_HAND_DROP = DEFAULT_TORSO_HEIGHT * 0.85 - (DEFAULT_ARM_UPPER_LENGTH + DEFAULT_ARM_LOWER_LENGTH);
const DEFAULT_LEG_OFFSET = DEFAULT_TORSO_WIDTH * 0.25;
const DEFAULT_LEG_TOTAL = DEFAULT_LEG_UPPER_LENGTH + DEFAULT_LEG_LOWER_LENGTH;
const DEFAULT_FOOT_THICKNESS = 0.5;
const DEFAULT_POSITION_Y = DEFAULT_LEG_TOTAL + DEFAULT_FOOT_THICKNESS;
const PROFILE_FOOT_LINE_LENGTH = DEFAULT_TORSO_WIDTH * 0.12;
const IK_EPSILON = 1e-6;

const defaultMeasurements = {
  torso: {
    width: DEFAULT_TORSO_WIDTH,
    height: DEFAULT_TORSO_HEIGHT,
    shoulderExtension: DEFAULT_SHOULDER_EXTENSION,
    direction: "front"
  },
  head: {
    verticalExtent: 4.5,
    angle: 90,
    direction: "front"
  },
  hands: {
    left: {
      upperLength: DEFAULT_ARM_UPPER_LENGTH,
      lowerLength: DEFAULT_ARM_LOWER_LENGTH,
      effectorCoordinate: [-DEFAULT_HAND_OFFSET, DEFAULT_HAND_DROP],
      positiveBend: false
    },
    right: {
      upperLength: DEFAULT_ARM_UPPER_LENGTH,
      lowerLength: DEFAULT_ARM_LOWER_LENGTH,
      effectorCoordinate: [DEFAULT_HAND_OFFSET, DEFAULT_HAND_DROP],
      positiveBend: true
    }
  },
  legs: {
    left: {
      upperLength: DEFAULT_LEG_UPPER_LENGTH,
      lowerLength: DEFAULT_LEG_LOWER_LENGTH,
      effectorCoordinate: [-DEFAULT_LEG_OFFSET, -DEFAULT_LEG_TOTAL],
      positiveBend: false
    },
    right: {
      upperLength: DEFAULT_LEG_UPPER_LENGTH,
      lowerLength: DEFAULT_LEG_LOWER_LENGTH,
      effectorCoordinate: [DEFAULT_LEG_OFFSET, -DEFAULT_LEG_TOTAL],
      positiveBend: true
    }
  }
};

const helperCollection = typeof helpers === "object" ? helpers : null;
const clamp = helperCollection?.clamp;
const normalizePoint = helperCollection?.normalizePoint;
const addOffset = helperCollection?.addOffset;
const resolveNumber = helperCollection?.resolveNumber;
const resolveOptionalNumber = helperCollection?.resolveOptionalNumber;
const resolveBoolean = helperCollection?.resolveBoolean;
const mergeDeep = helperCollection?.mergeDeep;
const normalizeInput = helperCollection?.normalizeInput;

function requireHelper(fn, name) {
  if (typeof fn !== "function") {
    throw new Error(`cartoon/helpers/${name}.js must export a function as helpers.${name}`);
  }
}

requireHelper(clamp, "clamp");
requireHelper(normalizePoint, "normalizePoint");
requireHelper(addOffset, "addOffset");
requireHelper(resolveNumber, "resolveNumber");
requireHelper(resolveOptionalNumber, "resolveOptionalNumber");
requireHelper(resolveBoolean, "resolveBoolean");
requireHelper(mergeDeep, "mergeDeep");
requireHelper(normalizeInput, "normalizeInput");

function normalizeDirection(value, fallback = "front") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "left" || text === "right" || text === "back" || text === "front") {
    return text;
  }
  return fallback;
}

function normalizeFootDirection(value, fallback = "left") {
  if (typeof value === "string") {
    const lowered = value.trim().toLowerCase();
    if (lowered === "left" || lowered === "right") {
      return lowered;
    }
  }
  return fallback;
}

function buildStickManSkeleton(optionsInput = {}) {
  const normalizedOptions = normalizeInput(optionsInput, {});
  const measurementOverrides = normalizeInput(normalizedOptions.measurements, {});
  const measurements = mergeDeep(defaultMeasurements, measurementOverrides);
  const positionFallback = [0, DEFAULT_POSITION_Y];
  const position = normalizePoint(normalizedOptions.position, positionFallback);
  const handOverrideInputs = normalizeInput(measurementOverrides.hands, null);
  const legOverrideInputs = normalizeInput(measurementOverrides.legs, null);

  const torso = computeTorsoFrame(position, measurements.torso);
  const head = buildHeadSkeleton(torso, measurements.head);
  const hands = buildHandSkeleton(
    position,
    torso.handAttachmentPoints,
    measurements.hands,
    torso.direction,
    handOverrideInputs
  );
  const legs = buildLegSkeleton(
    position,
    torso.legAttachmentPoints,
    measurements.legs,
    torso.direction,
    legOverrideInputs
  );

  return {
    skeleton: {
      position,
      torso,
      head,
      hands,
      legs
    },
    normalizedOptions
  };
}

function computeTorsoFrame(position, torsoMeasurements = {}) {
  const width = resolveNumber(torsoMeasurements.width, defaultMeasurements.torso.width);
  const height = resolveNumber(torsoMeasurements.height, defaultMeasurements.torso.height);
  const rawShoulderExtension = resolveNumber(torsoMeasurements.shoulderExtension, null);
  const direction = normalizeDirection(torsoMeasurements.direction, defaultMeasurements.torso.direction);
  const [centerX, bottomY] = position;
  const halfWidth = width / 2;
  const topY = bottomY + height;
  const handsY = topY - height * 0.15;
  const shoulderExtension = Math.max(rawShoulderExtension != null ? rawShoulderExtension : width * 0.15, 0);
  const handOffset = halfWidth + shoulderExtension;
  const legOffset = width * 0.25;
  let leftHandPoint = [centerX - handOffset, handsY];
  let rightHandPoint = [centerX + handOffset, handsY];
  let leftLegPoint = [centerX - legOffset, bottomY];
  let rightLegPoint = [centerX + legOffset, bottomY];

  if (direction === "left" || direction === "right") {
    const centerHandPoint = [centerX, handsY];
    const centerLegPoint = [centerX, bottomY];
    leftHandPoint = centerHandPoint;
    rightHandPoint = centerHandPoint;
    leftLegPoint = centerLegPoint;
    rightLegPoint = centerLegPoint;
  }

  return {
    centerBottomPoint: position,
    width,
    height,
    shoulderExtension,
    direction,
    headAttachmentPoint: [centerX, topY],
    handAttachmentPoints: {
      left: leftHandPoint,
      right: rightHandPoint
    },
    legAttachmentPoints: {
      left: leftLegPoint,
      right: rightLegPoint
    }
  };
}

function buildHeadSkeleton(torso, headMeasurements = {}) {
  return {
    attachmentPoint: torso.headAttachmentPoint,
    verticalExtent: resolveNumber(headMeasurements.verticalExtent, defaultMeasurements.head.verticalExtent),
    angle: resolveNumber(headMeasurements.angle, defaultMeasurements.head.angle),
    direction: normalizeDirection(headMeasurements.direction, torso.direction || defaultMeasurements.head.direction)
  };
}

function adjustEffectorForProfile(effector, torsoDirection) {
  if (torsoDirection === "left" || torsoDirection === "right") {
    return [0, effector[1]];
  }
  return effector;
}

function buildHandSkeleton(
  position,
  attachmentPoints,
  handMeasurements = {},
  torsoDirection = "front",
  handOverrideInputs = null
) {
  const defaults = defaultMeasurements.hands;
  const handOverrides = normalizeInput(handOverrideInputs, null);
  return {
    left: buildHandSide("left"),
    right: buildHandSide("right")
  };

  function buildHandSide(side) {
    const measurement = handMeasurements[side] || {};
    const sideDefaults = defaults[side];
    const overrideInput = handOverrides ? normalizeInput(handOverrides[side], null) : null;
    const hasCustomEffector = overrideInput && overrideInput.effectorCoordinate != null;
    const upper = resolveNumber(measurement.upperLength, sideDefaults.upperLength);
    const lower = resolveNumber(measurement.lowerLength, sideDefaults.lowerLength);
    const attachmentOffset = [
      attachmentPoints[side][0] - position[0],
      attachmentPoints[side][1] - position[1] - (upper + lower)
    ];
    const baseDefaultEffector = normalizePoint(sideDefaults.effectorCoordinate, attachmentOffset);
    const fallbackEffector = adjustEffectorForProfile(baseDefaultEffector, torsoDirection);
    const effector = hasCustomEffector
      ? normalizePoint(measurement.effectorCoordinate, fallbackEffector)
      : fallbackEffector;
    const targetPoint = addOffset(position, effector);
    const positiveBend = resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend);
    const ik = solveLimbPose(attachmentPoints[side], targetPoint, upper, lower, positiveBend);
    return {
      attachmentPoint: attachmentPoints[side],
      targetPoint,
      reachTarget: ik.reachTarget,
      bendPoint: ik.hingePoint,
      reachDirection: ik.reachDirection,
      bendDirection: ik.bendSign,
      lengths: {
        upper,
        lower
      },
      positiveBend,
      joints: {
        attachment: attachmentPoints[side],
        hinge: ik.hingePoint,
        effector: ik.reachTarget
      }
    };
  }
}

function buildLegSkeleton(
  position,
  attachmentPoints,
  legMeasurements = {},
  torsoDirection = "front",
  legOverrideInputs = null
) {
  const defaults = defaultMeasurements.legs;
  const legOverrides = normalizeInput(legOverrideInputs, null);
  return {
    left: buildLegSide("left"),
    right: buildLegSide("right")
  };

  function buildLegSide(side) {
    const measurement = legMeasurements[side] || {};
    const sideDefaults = defaults[side];
    const overrideInput = legOverrides ? normalizeInput(legOverrides[side], null) : null;
    const hasCustomEffector = overrideInput && overrideInput.effectorCoordinate != null;
    const upperLength = resolveNumber(measurement.upperLength, sideDefaults.upperLength);
    const lowerLength = resolveNumber(measurement.lowerLength, sideDefaults.lowerLength);
    const attachmentOffset = [
      attachmentPoints[side][0] - position[0],
      -(upperLength + lowerLength)
    ];
    const baseDefaultEffector = normalizePoint(sideDefaults.effectorCoordinate, attachmentOffset);
    const fallbackEffector = adjustEffectorForProfile(baseDefaultEffector, torsoDirection);
    const effector = hasCustomEffector
      ? normalizePoint(measurement.effectorCoordinate, fallbackEffector)
      : fallbackEffector;

    const positiveBend = resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend);
    const profileFootLength =
      torsoDirection === "left" || torsoDirection === "right" ? PROFILE_FOOT_LINE_LENGTH : null;
    const foot = resolveFootMeasurement(measurement, positiveBend, profileFootLength);
    const targetPoint = addOffset(position, effector);
    const ik = solveLimbPose(attachmentPoints[side], targetPoint, upperLength, lowerLength, positiveBend);

    return {
      attachmentPoint: attachmentPoints[side],
      targetPoint,
      reachTarget: ik.reachTarget,
      bendPoint: ik.hingePoint,
      reachDirection: ik.reachDirection,
      bendDirection: ik.bendSign,
      lengths: {
        upper: upperLength,
        lower: lowerLength
      },
      positiveBend,
      foot,
      joints: {
        attachment: attachmentPoints[side],
        hinge: ik.hingePoint,
        effector: ik.reachTarget
      }
    };
  }
}

function resolveFootMeasurement(measurement, positiveBend, defaultLengthOverride = null) {
  const defaultDirection = positiveBend ? "right" : "left";
  const normalizedFoot = normalizeInput(measurement.foot, null);
  if (normalizedFoot) {
    return {
      length: resolveOptionalNumber(normalizedFoot.length),
      direction: normalizeFootDirection(normalizedFoot.direction, defaultDirection)
    };
  }
  return {
    length: defaultLengthOverride != null ? defaultLengthOverride : null,
    direction: defaultDirection
  };
}

function solveLimbPose(attachmentPoint, targetPoint, upperLength, lowerLength, positiveBend) {
  const safeUpper = Math.max(Math.abs(upperLength), IK_EPSILON);
  const safeLower = Math.max(Math.abs(lowerLength), IK_EPSILON);
  const dx = targetPoint[0] - attachmentPoint[0];
  const dy = targetPoint[1] - attachmentPoint[1];
  const distance = Math.sqrt(dx * dx + dy * dy);
  const maxReach = safeUpper + safeLower;
  const minReach = Math.abs(safeUpper - safeLower) + IK_EPSILON;
  const direction = distance > IK_EPSILON ? [dx / distance, dy / distance] : [0, 1];
  const reach = clamp(distance, minReach, maxReach);
  const reachTarget = [
    attachmentPoint[0] + direction[0] * reach,
    attachmentPoint[1] + direction[1] * reach
  ];
  const cosShoulder = clamp(
    (safeUpper * safeUpper + reach * reach - safeLower * safeLower) / (2 * safeUpper * reach),
    -1,
    1
  );
  const sinShoulder = Math.sqrt(Math.max(0, 1 - cosShoulder * cosShoulder));
  const perp = [-direction[1], direction[0]];
  const bendSign = positiveBend ? 1 : -1;
  const hingePoint = [
    attachmentPoint[0] + direction[0] * (safeUpper * cosShoulder) + perp[0] * (bendSign * safeUpper * sinShoulder),
    attachmentPoint[1] + direction[1] * (safeUpper * cosShoulder) + perp[1] * (bendSign * safeUpper * sinShoulder)
  ];
  const finalVec = [
    reachTarget[0] - hingePoint[0],
    reachTarget[1] - hingePoint[1]
  ];
  const finalMag = Math.sqrt(finalVec[0] * finalVec[0] + finalVec[1] * finalVec[1]);
  const reachDirection = finalMag > IK_EPSILON
    ? [finalVec[0] / finalMag, finalVec[1] / finalMag]
    : direction.slice();

  return {
    hingePoint,
    reachTarget,
    reachDirection,
    bendSign
  };
}

return {
  build: buildStickManSkeleton,
  defaultMeasurements,
  normalizeInput,
  mergeDeep
};

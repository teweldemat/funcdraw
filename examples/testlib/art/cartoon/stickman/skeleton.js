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

function normalizeInput(value, fallback) {
  return value != null && typeof value === "object" ? value : fallback;
}

function normalizePoint(value, fallback) {
  if (!value) {
    return fallback;
  }
  if (Array.isArray(value) && value.length >= 2) {
    const x = typeof value[0] === "number" ? value[0] : Number(value[0]);
    const y = typeof value[1] === "number" ? value[1] : Number(value[1]);
    return [
      Number.isFinite(x) ? x : fallback[0],
      Number.isFinite(y) ? y : fallback[1]
    ];
  }
  if (typeof value === "object") {
    if ("x" in value && "y" in value) {
      const x = typeof value.x === "number" ? value.x : Number(value.x);
      const y = typeof value.y === "number" ? value.y : Number(value.y);
      return [
        Number.isFinite(x) ? x : fallback[0],
        Number.isFinite(y) ? y : fallback[1]
      ];
    }
    if ("left" in value && "top" in value) {
      const x = typeof value.left === "number" ? value.left : Number(value.left);
      const y = typeof value.top === "number" ? value.top : Number(value.top);
      return [
        Number.isFinite(x) ? x : fallback[0],
        Number.isFinite(y) ? y : fallback[1]
      ];
    }
  }
  return fallback.slice();
}

function normalizeDirection(value, fallback = "front") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "left" || text === "right" || text === "back" || text === "front") {
    return text;
  }
  return fallback;
}

function mergeDeep(target, source) {
  if (!source || typeof source !== "object") {
    return target;
  }
  const output = Array.isArray(target) ? target.slice() : { ...target };
  for (const [key, value] of Object.entries(source)) {
    if (value && typeof value === "object" && !Array.isArray(value)) {
      output[key] = mergeDeep(
        Object.prototype.hasOwnProperty.call(output, key) && typeof output[key] === "object" ? output[key] : {},
        value
      );
    } else {
      output[key] = value;
    }
  }
  return output;
}

function addOffset(point, offset) {
  return [point[0] + offset[0], point[1] + offset[1]];
}

function resolveNumber(value, fallback) {
  return typeof value === "number" ? value : fallback;
}

function resolveOptionalNumber(value) {
  if (value == null) {
    return null;
  }
  const numeric = typeof value === "number" ? value : Number(value);
  return Number.isFinite(numeric) ? numeric : null;
}

function resolveBoolean(value, fallback) {
  return typeof value === "boolean" ? value : fallback;
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

  const torso = computeTorsoFrame(position, measurements.torso);
  const head = buildHeadSkeleton(torso, measurements.head);
  const hands = buildHandSkeleton(position, torso.handAttachmentPoints, measurements.hands, torso.direction);
  const legs = buildLegSkeleton(position, torso.legAttachmentPoints, measurements.legs, torso.direction);

  return {
    skeleton: {
      position,
      torso,
      head,
      hands,
      legs
    },
    position,
    measurements,
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

  if (direction === "back") {
    [leftHandPoint, rightHandPoint] = [rightHandPoint, leftHandPoint];
    [leftLegPoint, rightLegPoint] = [rightLegPoint, leftLegPoint];
  } else if (direction === "left" || direction === "right") {
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

function orientEffectorForDirection(effector, direction) {
  if (direction === "back") {
    return [-effector[0], effector[1]];
  }
  return effector;
}

function buildHandSkeleton(position, attachmentPoints, handMeasurements = {}, torsoDirection = "front") {
  const defaults = defaultMeasurements.hands;
  return {
    left: buildHandSide("left"),
    right: buildHandSide("right")
  };

  function buildHandSide(side) {
    const measurement = handMeasurements[side] || {};
    const sideDefaults = defaults[side];
    const upper = resolveNumber(measurement.upperLength, sideDefaults.upperLength);
    const lower = resolveNumber(measurement.lowerLength, sideDefaults.lowerLength);
    const attachmentOffset = [
      attachmentPoints[side][0] - position[0],
      attachmentPoints[side][1] - position[1] - (upper + lower)
    ];
    const baseDefaultEffector = normalizePoint(sideDefaults.effectorCoordinate, attachmentOffset);
    const directionalDefaultEffector = orientEffectorForDirection(baseDefaultEffector, torsoDirection);
    const effector =
      measurement.effectorCoordinate != null
        ? normalizePoint(measurement.effectorCoordinate, directionalDefaultEffector)
        : directionalDefaultEffector;
    return {
      attachmentPoint: attachmentPoints[side],
      targetPoint: addOffset(position, effector),
      lengths: {
        upper,
        lower
      },
      positiveBend: resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend)
    };
  }
}

function buildLegSkeleton(position, attachmentPoints, legMeasurements = {}, torsoDirection = "front") {
  const defaults = defaultMeasurements.legs;
  return {
    left: buildLegSide("left"),
    right: buildLegSide("right")
  };

  function buildLegSide(side) {
    const measurement = legMeasurements[side] || {};
    const sideDefaults = defaults[side];
    const upperLength = resolveNumber(measurement.upperLength, sideDefaults.upperLength);
    const lowerLength = resolveNumber(measurement.lowerLength, sideDefaults.lowerLength);
    const attachmentOffset = [
      attachmentPoints[side][0] - position[0],
      -(upperLength + lowerLength)
    ];
    const baseDefaultEffector = normalizePoint(sideDefaults.effectorCoordinate, attachmentOffset);
    const directionalDefaultEffector = orientEffectorForDirection(baseDefaultEffector, torsoDirection);
    const effector =
      measurement.effectorCoordinate != null
        ? normalizePoint(measurement.effectorCoordinate, directionalDefaultEffector)
        : directionalDefaultEffector;

    const positiveBend = resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend);
    const foot = resolveFootMeasurement(measurement, positiveBend);

    return {
      attachmentPoint: attachmentPoints[side],
      targetPoint: addOffset(position, effector),
      lengths: {
        upper: upperLength,
        lower: lowerLength
      },
      positiveBend,
      foot
    };
  }
}

function resolveFootMeasurement(measurement, positiveBend) {
  const defaultDirection = positiveBend ? "right" : "left";
  const normalizedFoot = normalizeInput(measurement.foot, null);
  if (normalizedFoot) {
    return {
      length: resolveOptionalNumber(normalizedFoot.length),
      direction: normalizeFootDirection(normalizedFoot.direction, defaultDirection)
    };
  }
  return {
    length: resolveOptionalNumber(measurement.feetLength),
    direction: normalizeFootDirection(measurement.feetDirection, defaultDirection)
  };
}

return {
  build: buildStickManSkeleton,
  defaultMeasurements,
  normalizeInput,
  mergeDeep
};

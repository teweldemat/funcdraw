const DEFAULT_TORSO_WIDTH = 6;
const DEFAULT_TORSO_HEIGHT = 11;

const defaultMeasurements = {
  torso: {
    width: DEFAULT_TORSO_WIDTH,
    height: DEFAULT_TORSO_HEIGHT
  },
  head: {
    verticalExtent: 4.5,
    angle: 90
  },
  hands: {
    left: {
      upperLength: 4,
      lowerLength: 3,
      effectorCoordinate: [-11, 11],
      positiveBend: true
    },
    right: {
      upperLength: 4,
      lowerLength: 3,
      effectorCoordinate: [11, 11],
      positiveBend: true
    }
  },
  legs: {
    left: {
      upperLength: 4.5,
      lowerLength: 4,
      effectorCoordinate: [-4, -4.5],
      positiveBend: false
    },
    right: {
      upperLength: 4.5,
      lowerLength: 4,
      effectorCoordinate: [4, -4.5],
      positiveBend: false
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

function resolveBoolean(value, fallback) {
  return typeof value === "boolean" ? value : fallback;
}

function buildStickManSkeleton(optionsInput = {}) {
  const normalizedOptions = normalizeInput(optionsInput, {});
  const position = normalizePoint(normalizedOptions.position, [20, 6]);
  const measurementOverrides = normalizeInput(normalizedOptions.measurements, {});
  const measurements = mergeDeep(defaultMeasurements, measurementOverrides);

  const torso = computeTorsoFrame(position, measurements.torso);
  const head = buildHeadSkeleton(torso, measurements.head);
  const hands = buildHandSkeleton(position, torso.handAttachmentPoints, measurements.hands);
  const legs = buildLegSkeleton(position, torso.legAttachmentPoints, measurements.legs);

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
  const [centerX, bottomY] = position;
  const halfWidth = width / 2;
  const topY = bottomY + height;
  const handsY = topY - height * 0.15;
  const legOffset = width * 0.25;
  return {
    centerBottomPoint: position,
    width,
    height,
    headAttachmentPoint: [centerX, topY],
    handAttachmentPoints: {
      left: [centerX - halfWidth, handsY],
      right: [centerX + halfWidth, handsY]
    },
    legAttachmentPoints: {
      left: [centerX - legOffset, bottomY],
      right: [centerX + legOffset, bottomY]
    }
  };
}

function buildHeadSkeleton(torso, headMeasurements = {}) {
  return {
    attachmentPoint: torso.headAttachmentPoint,
    verticalExtent: resolveNumber(headMeasurements.verticalExtent, defaultMeasurements.head.verticalExtent),
    angle: resolveNumber(headMeasurements.angle, defaultMeasurements.head.angle)
  };
}

function buildHandSkeleton(position, attachmentPoints, handMeasurements = {}) {
  const defaults = defaultMeasurements.hands;
  return {
    left: buildHandSide("left"),
    right: buildHandSide("right")
  };

  function buildHandSide(side) {
    const measurement = handMeasurements[side] || {};
    const sideDefaults = defaults[side];
    return {
      attachmentPoint: attachmentPoints[side],
      targetPoint: addOffset(position, normalizePoint(measurement.effectorCoordinate, sideDefaults.effectorCoordinate)),
      lengths: {
        upper: resolveNumber(measurement.upperLength, sideDefaults.upperLength),
        lower: resolveNumber(measurement.lowerLength, sideDefaults.lowerLength)
      },
      positiveBend: resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend)
    };
  }
}

function buildLegSkeleton(position, attachmentPoints, legMeasurements = {}) {
  const defaults = defaultMeasurements.legs;
  return {
    left: buildLegSide("left"),
    right: buildLegSide("right")
  };

  function buildLegSide(side) {
    const measurement = legMeasurements[side] || {};
    const sideDefaults = defaults[side];
    return {
      attachmentPoint: attachmentPoints[side],
      targetPoint: addOffset(position, normalizePoint(measurement.effectorCoordinate, sideDefaults.effectorCoordinate)),
      lengths: {
        upper: resolveNumber(measurement.upperLength, sideDefaults.upperLength),
        lower: resolveNumber(measurement.lowerLength, sideDefaults.lowerLength)
      },
      positiveBend: resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend)
    };
  }
}

return {
  build: buildStickManSkeleton,
  defaultMeasurements,
  normalizeInput,
  mergeDeep
};

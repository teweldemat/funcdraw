const defaultLengths = { upper: 4.5, lower: 4 };
const defaultStyle = { stroke: "#0ea5e9", width: 1.1 };
const FS_TYPE = {
  NULL: 0,
  BOOLEAN: 1,
  INTEGER: 2,
  FLOAT: 6,
  STRING: 7,
  LIST: 9,
  KVC: 10
};

function fsValueToJs(value) {
  if (!Array.isArray(value) || value.length !== 2) {
    return value;
  }
  const [type, raw] = value;
  switch (type) {
    case FS_TYPE.NULL:
      return null;
    case FS_TYPE.BOOLEAN:
    case FS_TYPE.INTEGER:
    case FS_TYPE.FLOAT:
    case FS_TYPE.STRING:
      return raw;
    case FS_TYPE.LIST: {
      const list = [];
      if (raw && typeof raw[Symbol.iterator] === "function") {
        for (const item of raw) {
          list.push(fsValueToJs(item));
        }
      }
      return list;
    }
    case FS_TYPE.KVC: {
      const obj = {};
      if (raw && typeof raw.getAll === "function") {
        for (const [key, val] of raw.getAll()) {
          obj[key] = fsValueToJs(val);
        }
      }
      return obj;
    }
    default:
      return raw;
  }
}

function normalizeOptions(options) {
  if (!options) {
    return {};
  }
  if (
    Array.isArray(options) &&
    options.length === 2 &&
    typeof options[0] === "number" &&
    typeof options[1] !== "number"
  ) {
    const converted = fsValueToJs(options);
    return typeof converted === "object" && converted !== null ? converted : {};
  }
  if (options.__fsKind === "KeyValueCollection") {
    return fsValueToJs([FS_TYPE.KVC, options]);
  }
  return options;
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function createSegmentedLimb(attachmentPoint, endPoint, upperLength, lowerLength, style, bendDirection) {
  const epsilon = 1e-6;
  const safeUpper = Math.max(upperLength, epsilon);
  const safeLower = Math.max(lowerLength, epsilon);

  const dx = endPoint[0] - attachmentPoint[0];
  const dy = endPoint[1] - attachmentPoint[1];
  const distance = Math.sqrt(dx * dx + dy * dy);
  const maxReach = safeUpper + safeLower;
  const minReach = Math.abs(safeUpper - safeLower) + epsilon;

  const direction =
    distance > epsilon
      ? [dx / distance, dy / distance]
      : [0, 1];

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
  const knee = [
    attachmentPoint[0] + direction[0] * (safeUpper * cosShoulder) + perp[0] * (bendDirection * safeUpper * sinShoulder),
    attachmentPoint[1] + direction[1] * (safeUpper * cosShoulder) + perp[1] * (bendDirection * safeUpper * sinShoulder)
  ];

  return [
    {
      type: "line",
      from: attachmentPoint,
      to: knee,
      stroke: style.stroke,
      width: style.width
    },
    {
      type: "line",
      from: knee,
      to: reachTarget,
      stroke: style.stroke,
      width: style.width
    }
  ];
}

function createLeg(options) {
  const config = normalizeOptions(options);
  const attachmentPoint = config.attachmentPoint || [0, 0];
  const targetPoint = config.targetPoint || attachmentPoint;
  const lengths = config.lengths || defaultLengths;
  const style = {
    stroke: config.style?.stroke ?? defaultStyle.stroke,
    width: config.style?.width ?? defaultStyle.width
  };
  let bendDirection = null;
  if (typeof config.bendDirection === "number") {
    bendDirection = config.bendDirection;
  } else if (config.positiveBend === true) {
    bendDirection = 1;
  } else if (config.positiveBend === false) {
    bendDirection = -1;
  } else {
    bendDirection = targetPoint[0] >= attachmentPoint[0] ? 1 : -1;
  }

  const segments = createSegmentedLimb(
    attachmentPoint,
    targetPoint,
    lengths.upper,
    lengths.lower,
    style,
    bendDirection
  );

  return {
    graphics: segments,
    attachmentPoint,
    targetPoint
  };
}

return createLeg;

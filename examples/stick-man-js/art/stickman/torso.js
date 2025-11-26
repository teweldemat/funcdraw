const defaultStyle = {
  fill: "#1f2937",
  stroke: "#cbd5f5",
  strokeWidth: 0.6
};

const FS_TYPE = {
  LIST: 9,
  KVC: 10
};

function fsValueToJs(value) {
  if (!Array.isArray(value) || value.length !== 2) {
    return value;
  }
  const [type, raw] = value;
  if (type === FS_TYPE.LIST) {
    const arr = [];
    if (raw && typeof raw[Symbol.iterator] === "function") {
      for (const item of raw) {
        arr.push(fsValueToJs(item));
      }
    }
    return arr;
  }
  if (type === FS_TYPE.KVC) {
    const obj = {};
    if (raw && typeof raw.getAll === "function") {
      for (const [key, val] of raw.getAll()) {
        obj[key] = fsValueToJs(val);
      }
    }
    return obj;
  }
  return raw;
}

function normalizeConfig(config) {
  if (!config) {
    return {};
  }
  if (Array.isArray(config) && config.length === 2) {
    const converted = fsValueToJs(config);
    return typeof converted === "object" && converted !== null ? converted : {};
  }
  if (config.__fsKind === "KeyValueCollection") {
    return fsValueToJs([FS_TYPE.KVC, config]);
  }
  return config;
}

function normalizePoint(value, fallback) {
  if (!value) {
    return fallback;
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[0] === "number" && typeof value[1] === "number") {
    return value;
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[0] === "number" && typeof value[1] !== "number") {
    const converted = fsValueToJs(value);
    return Array.isArray(converted) ? converted : fallback;
  }
  if (value.__fsKind === "FsList") {
    const converted = fsValueToJs([FS_TYPE.LIST, value]);
    return Array.isArray(converted) ? converted : fallback;
  }
  return Array.isArray(value) ? value : fallback;
}

function createTorso(configInput = {}) {
  const config = normalizeConfig(configInput);
  const centerBottomPoint = normalizePoint(config.centerBottomPoint, [20, 6]);
  const width = typeof config.width === "number" ? config.width : 6;
  const height = typeof config.height === "number" ? config.height : 11;
  const fill = config.fill ?? defaultStyle.fill;
  const stroke = config.stroke ?? defaultStyle.stroke;
  const strokeWidth = config.strokeWidth ?? defaultStyle.strokeWidth;

  const [centerX, bottomY] = centerBottomPoint;
  const halfWidth = width / 2;
  const topY = bottomY + height;

  const body = {
    type: "rect",
    position: [centerX - halfWidth, bottomY],
    size: [width, height],
    fill,
    stroke,
    width: strokeWidth
  };

  const handsY = topY - height * 0.15;
  const legOffset = width * 0.25;

  return {
    graphics: [body],
    handAttachmentPoints: {
      left: [centerX - halfWidth, handsY],
      right: [centerX + halfWidth, handsY]
    },
    legAttachmentPoints: {
      left: [centerX - legOffset, bottomY],
      right: [centerX + legOffset, bottomY]
    },
    headAttachmentPoint: [centerX, topY]
  };
}

return createTorso;

const defaultHeadConfig = {
  verticalExtent: 4.5,
  angle: 90,
  fill: "#fff7ed",
  stroke: "#fdba74",
  strokeWidth: 0.4,
  gazeColor: "#ea580c",
  segments: 20
};

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
      const converted = [];
      if (raw && typeof raw[Symbol.iterator] === "function") {
        for (const item of raw) {
          converted.push(fsValueToJs(item));
        }
      }
      return converted;
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

function normalizeValue(value) {
  if (!value) {
    return value;
  }
  if (
    Array.isArray(value) &&
    value.length === 2 &&
    typeof value[0] === "number" &&
    typeof value[1] !== "number"
  ) {
    return fsValueToJs(value);
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[0] === "number" && typeof value[1] === "number") {
    return value;
  }
  if (value.__fsKind === "FsList") {
    return fsValueToJs([FS_TYPE.LIST, value]);
  }
  if (value.__fsKind === "KeyValueCollection") {
    return fsValueToJs([FS_TYPE.KVC, value]);
  }
  return value;
}

function degToRad(degrees) {
  return (degrees * Math.PI) / 180;
}

function createHead(attachmentPointInput, configInput = {}) {
  const attachmentPoint = normalizeValue(attachmentPointInput) || [20, 17];
  const config = normalizeValue(configInput) || {};

  const safeExtent = Math.max(config.verticalExtent ?? defaultHeadConfig.verticalExtent, 1);
  const radius = safeExtent / 2;
  const radians = degToRad(config.angle ?? defaultHeadConfig.angle);
  const fill = config.fill ?? defaultHeadConfig.fill;
  const stroke = config.stroke ?? defaultHeadConfig.stroke;
  const strokeWidth = config.strokeWidth ?? defaultHeadConfig.strokeWidth;
  const gazeColor = config.gazeColor ?? defaultHeadConfig.gazeColor;
  const segments = Math.max(6, Math.floor(config.segments ?? defaultHeadConfig.segments));

  const center = [
    attachmentPoint[0] + Math.cos(radians) * radius,
    attachmentPoint[1] + Math.sin(radians) * radius
  ];

  const outlinePoints = [];
  for (let i = 0; i < segments; i += 1) {
    const theta = (i / segments) * Math.PI * 2;
    outlinePoints.push([
      center[0] + Math.cos(theta) * radius,
      center[1] + Math.sin(theta) * radius
    ]);
  }

  const outline = {
    type: "polygon",
    points: outlinePoints,
    fill,
    stroke,
    width: strokeWidth
  };

  const gaze = {
    type: "line",
    from: center,
    to: [
      center[0] + Math.cos(radians) * radius,
      center[1] + Math.sin(radians) * radius
    ],
    stroke: gazeColor,
    width: strokeWidth
  };

  return {
    graphics: [outline, gaze],
    center
  };
}

return createHead;

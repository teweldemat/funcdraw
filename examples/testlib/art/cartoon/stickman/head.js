const defaultHeadConfig = {
  verticalExtent: 4.5,
  angle: 90,
  fill: "#fff7ed",
  stroke: "#fdba74",
  strokeWidth: 0.4,
  gazeColor: "#ea580c",
  segments: 20
};
const PI = typeof Math === "object" && typeof Math.PI === "number" ? Math.PI : 3.141592653589793;

function toNumber(value, fallback) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (value == null) {
    return fallback;
  }
  const coerced = Number(value);
  return Number.isFinite(coerced) ? coerced : fallback;
}

function normalizePoint(value, fallback) {
  if (Array.isArray(value) && value.length >= 2) {
    return [toNumber(value[0], fallback[0]), toNumber(value[1], fallback[1])];
  }
  if (value && typeof value === "object") {
    if ("x" in value && "y" in value) {
      return [toNumber(value.x, fallback[0]), toNumber(value.y, fallback[1])];
    }
    if ("left" in value && "top" in value) {
      return [toNumber(value.left, fallback[0]), toNumber(value.top, fallback[1])];
    }
  }
  return fallback.slice();
}

function normalizeConfig(config) {
  return config && typeof config === "object" ? config : {};
}

function degToRad(degrees) {
  return (degrees * PI) / 180;
}

function createHead(attachmentPointInput, configInput = {}) {
  const attachmentPoint = normalizePoint(attachmentPointInput, [20, 17]);
  const config = normalizeConfig(configInput);

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
    const theta = (i / segments) * PI * 2;
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

const defaultStyle = {
  fill: "#1f2937",
  stroke: "#cbd5f5",
  strokeWidth: 0.6
};

function normalizeConfig(config) {
  return config && typeof config === "object" ? config : {};
}

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
    const x = toNumber(value[0], fallback[0]);
    const y = toNumber(value[1], fallback[1]);
    return [x, y];
  }
  if (value && typeof value === "object") {
    if ("x" in value && "y" in value) {
      return [toNumber(value.x, fallback[0]), toNumber(value.y, fallback[1])];
    }
    if ("left" in value && "bottom" in value) {
      return [toNumber(value.left, fallback[0]), toNumber(value.bottom, fallback[1])];
    }
  }
  return fallback.slice();
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

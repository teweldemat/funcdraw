function ensureObject(value, fallback) {
  return value != null && typeof value === "object" ? value : fallback;
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

function clampNumber(value, min, max, fallback) {
  const num = toNumber(value, fallback != null ? fallback : min);
  if (!Number.isFinite(num)) {
    return min;
  }
  return Math.min(max, Math.max(min, num));
}

function clampInteger(value, min, max, fallback) {
  const num = Math.round(toNumber(value, fallback != null ? fallback : min));
  if (!Number.isFinite(num)) {
    return min;
  }
  return Math.min(max, Math.max(min, num));
}

function selectPalette(rawPalette) {
  const palette = ensureObject(rawPalette, {});
  return {
    fill: typeof palette.fill === "string" ? palette.fill : "#f1f5f9",
    stroke: typeof palette.stroke === "string" ? palette.stroke : "#cbd5f5",
    highlight: typeof palette.highlight === "string" ? palette.highlight : "#ffffff"
  };
}

function createLobe(center, radius, fill, stroke) {
  return {
    type: "circle",
    center,
    radius,
    fill,
    stroke,
    width: Math.max(radius * 0.25, 0.2)
  };
}

function cloud(rawOptions = {}) {
  const options = ensureObject(rawOptions, {});
  const position = Array.isArray(options.position) ? options.position : [0, 0];
  const width = clampNumber(options.width, 6, 80, 22);
  const puffiness = clampNumber(options.puffiness ?? options.heightFactor, 0.6, 1.6, 1);
  const lobeCount = clampInteger(options.lobes, 3, 6, 4);
  const palette = selectPalette(options.palette);
  const height = width * 0.4 * puffiness;

  const graphics = [];

  graphics.push({
    type: "ellipse",
    center: [position[0], position[1] + height * 0.15],
    radiusX: width * 0.55,
    radiusY: height * 0.5,
    fill: palette.fill,
    stroke: palette.stroke,
    width: Math.max(width * 0.04, 0.3)
  });

  const lobeSpacing = width * 0.75;
  for (let index = 0; index < lobeCount; index++) {
    const progress = lobeCount === 1 ? 0.5 : index / (lobeCount - 1);
    const offsetX = (progress - 0.5) * lobeSpacing;
    const bulge = 1 - Math.abs(progress - 0.5) * 1.4;
    const centerY = position[1] + height * (0.25 + bulge * 0.4);
    const radius = width * (0.18 + bulge * 0.12);
    graphics.push(createLobe([position[0] + offsetX, centerY], radius, palette.fill, palette.stroke));
  }

  graphics.push({
    type: "ellipse",
    center: [position[0] - width * 0.2, position[1] + height * 0.25],
    radiusX: width * 0.3,
    radiusY: height * 0.25,
    fill: palette.highlight,
    stroke: palette.highlight,
    width: Math.max(width * 0.015, 0.1),
    opacity: 0.6
  });

  return {
    graphics,
    anchor: position,
    width,
    height
  };
}

return cloud;

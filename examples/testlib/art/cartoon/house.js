function ensureObject(value, fallback) {
  return value != null && typeof value === "object" ? value : fallback;
}

function selectCoordinate(source, keys) {
  for (const key of keys) {
    if (key in source) {
      return { found: true, value: source[key] };
    }
  }
  return { found: false, value: undefined };
}

function normalizePoint(value, fallback) {
  if (Array.isArray(value) && value.length >= 2 && typeof value[0] === "number" && typeof value[1] === "number") {
    return [value[0], value[1]];
  }
  if (value && typeof value === "object") {
    const xResult = selectCoordinate(value, ["x", "left", "right"]);
    const yResult = selectCoordinate(value, ["y", "top", "bottom"]);
    if (xResult.found && yResult.found) {
      return [toNumber(xResult.value, fallback[0]), toNumber(yResult.value, fallback[1])];
    }
  }
  return fallback.slice();
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

function selectPalette(type) {
  switch (type) {
    case "modern":
      return {
        body: "#e2e8f0",
        outline: "#0f172a",
        roof: "#1f2937",
        accent: "#0ea5e9"
      };
    case "cottage":
      return {
        body: "#fef3c7",
        outline: "#b45309",
        roof: "#92400e",
        accent: "#c2410c"
      };
    default:
      return {
        body: "#f1f5f9",
        outline: "#0f172a",
        roof: "#dc2626",
        accent: "#f97316"
      };
  }
}

function createWindow(position, size) {
  return {
    type: "rect",
    position: [position[0] - size / 2, position[1] - size / 2],
    size: [size, size],
    fill: "#fefce8",
    stroke: "#94a3b8",
    width: Math.max(size * 0.08, 0.2)
  };
}

function createDoor(position, width, height, color, outline, openLevel, interiorColor) {
  const clampedLevel = Math.max(0, Math.min(1, openLevel || 0));
  const base = {
    type: "rect",
    position: [position[0] - width / 2, position[1]],
    size: [width, height],
    fill: clampedLevel > 0 ? interiorColor : color,
    stroke: outline,
    width: Math.max(width * 0.08, 0.25)
  };

  if (clampedLevel <= 0) {
    return [base];
  }

  const hingeX = position[0] - width / 2;
  const bottomY = position[1];
  const topY = position[1] + height;
  const angle = clampedLevel * Math.PI * 0.5;
  const swingOut = Math.sin(angle);
  const swingForward = Math.cos(angle);
  const outwardOffsetX = width * 0.6 * swingOut;
  const outwardOffsetY = width * 0.2 * swingOut;
  const farX = hingeX + width * swingForward + outwardOffsetX;
  const strokeWidth = Math.max(width * 0.05, 0.35);

  const panel = {
    type: "polygon",
    points: [
      [hingeX, bottomY],
      [hingeX, topY],
      [farX, topY + outwardOffsetY],
      [farX, bottomY + outwardOffsetY]
    ],
    fill: color,
    stroke: outline,
    width: strokeWidth
  };

  return [base, panel];
}

function createClassicRoof(center, width, roofHeight, color, outline) {
  const half = width / 2;
  return {
    type: "polygon",
    points: [
      [center[0] - half - 0.3 * width, center[1]],
      [center[0], center[1] + roofHeight],
      [center[0] + half + 0.3 * width, center[1]]
    ],
    fill: color,
    stroke: outline,
    width: Math.max(width * 0.05, 0.4)
  };
}

function createModernRoof(center, width, thickness, color, accent) {
  const half = width / 2;
  return [
    {
      type: "rect",
      position: [center[0] - half, center[1]],
      size: [width, thickness],
      fill: color,
      stroke: color,
      width: thickness * 0.5
    },
    {
      type: "rect",
      position: [center[0] + width * 0.1, center[1] + thickness * 0.4],
      size: [width * 0.5, thickness * 0.6],
      fill: accent,
      stroke: accent,
      width: thickness * 0.3
    }
  ];
}

function createCottageRoof(center, width, roofHeight, color, outline) {
  const half = width / 2;
  return {
    type: "polygon",
    points: [
      [center[0] - half - 1, center[1]],
      [center[0] - half * 0.2, center[1] + roofHeight],
      [center[0] + half * 0.2, center[1] + roofHeight],
      [center[0] + half + 1, center[1]]
    ],
    fill: color,
    stroke: outline,
    width: Math.max(width * 0.04, 0.35)
  };
}

function house(rawOptions = {}) {
  const options = ensureObject(rawOptions, {});
  const position = normalizePoint(options.position, [0, 0]);
  const width = clampNumber(options.width, 8, 400, 16);
  const type = String(options.type ?? "classic").toLowerCase();
  const palette = selectPalette(type);
  const doorOpenLevel = clampNumber(options.doorOpenLevel, 0, 1, 0);
  const baseHeight = width * 0.65;
  const roofHeight = width * (type === "modern" ? 0.08 : type === "cottage" ? 0.3 : 0.4);
  const baseLeft = position[0] - width / 2;
  const doorWidth = Math.max(width * 0.18, 2.2);
  const doorHeight = baseHeight * 0.45;
  const windowSize = width * 0.2;
  const windowY = position[1] + baseHeight * 0.55;

  const graphics = [];
  graphics.push({
    type: "rect",
    position: [baseLeft, position[1]],
    size: [width, baseHeight],
    fill: palette.body,
    stroke: palette.outline,
    width: Math.max(width * 0.04, 0.4)
  });

  graphics.push(
    createWindow([position[0] - width * 0.25, windowY], windowSize),
    createWindow([position[0] + width * 0.25, windowY], windowSize)
  );

  graphics.push(
    ...createDoor(
      [position[0], position[1]],
      doorWidth,
      doorHeight,
      palette.accent,
      palette.outline,
      doorOpenLevel,
      palette.outline
    )
  );

  if (type === "modern") {
    graphics.push(...createModernRoof([position[0], position[1] + baseHeight], width, roofHeight, palette.roof, palette.accent));
  } else if (type === "cottage") {
    graphics.push(createCottageRoof([position[0], position[1] + baseHeight], width, roofHeight, palette.roof, palette.outline));
  } else {
    graphics.push(createClassicRoof([position[0], position[1] + baseHeight], width, roofHeight, palette.roof, palette.outline));
  }

  return {
    graphics,
    anchor: position,
    width,
    type
  };
}

return house;

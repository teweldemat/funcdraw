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

function normalizeGraphics(value) {
  if (!value) {
    return [];
  }
  if (Array.isArray(value)) {
    return value.slice();
  }
  if (typeof value === "object") {
    if (Array.isArray(value.graphics)) {
      return value.graphics.slice();
    }
    return [value];
  }
  return [];
}

function applyOpacity(nodes, opacity) {
  const clamped = clampNumber(opacity, 0, 1, 1);
  if (clamped >= 1) {
    return nodes;
  }
  return nodes.map((node) => {
    if (!node || typeof node !== "object") {
      return node;
    }
    const existing = typeof node.opacity === "number" ? node.opacity : 1;
    return { ...node, opacity: existing * clamped };
  });
}

function resolveInterior(spec, context) {
  if (!spec) {
    return [];
  }
  const baseValue = typeof spec === "function" ? spec(context) : spec;
  const graphics = normalizeGraphics(
    baseValue && typeof baseValue === "object" && !Array.isArray(baseValue) && baseValue.graphics
      ? baseValue.graphics
      : baseValue
  );
  if (graphics.length === 0) {
    return [];
  }

  const resolvedOpacity =
    typeof baseValue === "object" && !Array.isArray(baseValue)
      ? baseValue.opacity ?? context.reveal
      : context.reveal;
  return applyOpacity(graphics, resolvedOpacity);
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
  const baseOpacity = clampedLevel <= 0 ? 1 : 0; // hide the static slab as soon as the door starts opening
  const base = {
    type: "rect",
    position: [position[0] - width / 2, position[1]],
    size: [width, height],
    fill: clampedLevel > 0 ? interiorColor : color,
    stroke: outline,
    width: Math.max(width * 0.08, 0.25),
    opacity: baseOpacity
  };

  if (clampedLevel <= 0) {
    return [base];
  }

  const hingeX = position[0] - width / 2;
  const bottomY = position[1];
  const topY = position[1] + height;
  const angle = clampedLevel * Math.PI; // swing fully flat against the wall
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

  return [panel];
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
  const position = Array.isArray(options.position) ? options.position : [0, 0];
  const width = clampNumber(options.width, 8, 400, 16);
  const type = String(options.type ?? "classic").toLowerCase();
  const palette = selectPalette(type);
  const doorOpenLevel = clampNumber(options.doorOpenLevel, 0, 1, 0);
  const baseHeight = width * 0.65;
  const roofHeight = width * (type === "modern" ? 0.08 : type === "cottage" ? 0.3 : 0.4);
  const baseLeft = position[0] - width / 2;
  const doorWidth = Math.max(width * 0.18, 2.2);
  const doorHeight = baseHeight * 0.9; // taller opening so interior occupants are visible
  const windowSize = width * 0.2;
  const windowY = position[1] + baseHeight * 0.55;
  const interiorReveal = doorOpenLevel;
  const doorLeft = position[0] - doorWidth / 2;
  const doorRight = position[0] + doorWidth / 2;
  const baseTop = position[1] + baseHeight;

  const interiorNodes = resolveInterior(options.interior, {
    doorWidth,
    doorHeight,
    doorPosition: [position[0], position[1]],
    doorCenter: [position[0], position[1] + doorHeight / 2],
    baseHeight,
    baseWidth: width,
    reveal: interiorReveal,
    palette,
    doorAnchor: [position[0], position[1]]
  });

  const graphics = [];

  // Dark room fill behind the door
  graphics.push({
    type: "rect",
    position: [doorLeft, position[1]],
    size: [doorWidth, doorHeight],
    fill: "#0b1224",
    stroke: "#0b1224",
    width: Math.max(doorWidth * 0.04, 0.25),
    opacity: Math.max(0, 1 - interiorReveal)
  });

  // Interior occupant/content
  graphics.push(...interiorNodes);

  // Door on top of interior
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

  // Walls over door (hide the portion swinging inside), then windows
  const wallStroke = Math.max(width * 0.04, 0.4);
  const leftWidth = Math.max(0, doorLeft - baseLeft);
  if (leftWidth > 0) {
    graphics.push({
      type: "rect",
      position: [baseLeft, position[1]],
      size: [leftWidth, baseHeight],
      fill: palette.body,
      stroke: palette.outline,
      width: wallStroke
    });
  }
  const rightWidth = Math.max(0, baseLeft + width - doorRight);
  if (rightWidth > 0) {
    graphics.push({
      type: "rect",
      position: [doorRight, position[1]],
      size: [rightWidth, baseHeight],
      fill: palette.body,
      stroke: palette.outline,
      width: wallStroke
    });
  }
  const topHeight = Math.max(0, baseHeight - doorHeight);
  if (topHeight > 0) {
    graphics.push({
      type: "rect",
      position: [doorLeft, position[1] + doorHeight],
      size: [doorWidth, topHeight],
      fill: palette.body,
      stroke: palette.outline,
      width: wallStroke
    });
  }

  graphics.push(
    createWindow([position[0] - width * 0.25, windowY], windowSize),
    createWindow([position[0] + width * 0.25, windowY], windowSize)
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

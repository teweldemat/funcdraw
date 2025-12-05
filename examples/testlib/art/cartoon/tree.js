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

function selectTreePalette(type) {
  switch (type) {
    case "pine":
      return {
        trunk: "#78350f",
        canopy: "#15803d",
        accent: "#0f5132"
      };
    case "column":
      return {
        trunk: "#7f5539",
        canopy: "#4d908e",
        accent: "#577590"
      };
    default:
      return {
        trunk: "#92400e",
        canopy: "#15803d",
        accent: "#22c55e"
      };
  }
}

function createRoundCanopy(center, radius, color, outline) {
  return {
    type: "circle",
    center,
    radius,
    fill: color,
    stroke: outline,
    width: radius * 0.15
  };
}

function createPineCanopy(center, width, height, color, outline) {
  const halfWidth = width / 2;
  return {
    type: "polygon",
    points: [
      [center[0] - halfWidth, center[1]],
      [center[0], center[1] + height],
      [center[0] + halfWidth, center[1]]
    ],
    fill: color,
    stroke: outline,
    width: Math.max(width * 0.04, 0.3)
  };
}

function createColumnCanopy(center, width, height, color, outline) {
  return {
    type: "ellipse",
    center,
    radiusX: width / 2,
    radiusY: height / 2,
    fill: color,
    stroke: outline,
    width: Math.max(width * 0.04, 0.3)
  };
}

function tree(rawOptions = {}) {
  const options = ensureObject(rawOptions, {});
  const position = Array.isArray(options.position) ? options.position : [0, 0];
  const height = clampNumber(options.height, 6, 500, 14);
  const type = String(options.type ?? "round").toLowerCase();
  const palette = selectTreePalette(type);
  const trunkHeight = height * 0.35;
  const canopyHeight = height - trunkHeight;
  const trunkWidth = Math.max(height * 0.12, 0.9);

  const trunk = {
    type: "rect",
    position: [position[0] - trunkWidth / 2, position[1]],
    size: [trunkWidth, trunkHeight],
    fill: palette.trunk,
    stroke: palette.trunk,
    width: trunkWidth * 0.2
  };

  const canopyCenter = [position[0], position[1] + trunkHeight + canopyHeight * 0.5];
  let canopy = null;
  if (type === "pine") {
    canopy = createPineCanopy([position[0], position[1] + trunkHeight], height * 0.9, canopyHeight, palette.canopy, palette.accent);
  } else if (type === "column") {
    canopy = createColumnCanopy(canopyCenter, height * 0.5, canopyHeight, palette.canopy, palette.accent);
  } else {
    canopy = createRoundCanopy(canopyCenter, canopyHeight * 0.55, palette.canopy, palette.accent);
  }

  const accents = [];
  if (type === "round") {
    accents.push(
      {
        type: "circle",
        center: [canopyCenter[0] - canopyHeight * 0.25, canopyCenter[1] + canopyHeight * 0.2],
        radius: canopyHeight * 0.2,
        fill: palette.accent,
        stroke: palette.accent,
        width: canopyHeight * 0.05
      },
      {
        type: "circle",
        center: [canopyCenter[0] + canopyHeight * 0.2, canopyCenter[1]],
        radius: canopyHeight * 0.18,
        fill: palette.accent,
        stroke: palette.accent,
        width: canopyHeight * 0.05
      }
    );
  }

  return {
    graphics: [trunk, canopy, ...accents],
    anchor: position,
    height,
    type
  };
}

return tree;

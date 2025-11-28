const PI_APPROX = typeof Math === "object" && typeof Math.PI === "number" ? Math.PI : 3.141592653589793;
const TAU_APPROX = PI_APPROX * 2;
const DEG_TO_RAD = PI_APPROX / 180;

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

function clampInteger(value, min, max, fallback) {
  const num = Math.round(toNumber(value, fallback != null ? fallback : min));
  if (!Number.isFinite(num)) {
    return min;
  }
  return Math.min(max, Math.max(min, num));
}

function degreesToRadians(degrees) {
  return degrees * DEG_TO_RAD;
}

function selectPalette(rawPalette) {
  const palette = ensureObject(rawPalette, {});
  return {
    core: typeof palette.core === "string" ? palette.core : "#facc15",
    outline: typeof palette.outline === "string" ? palette.outline : "#f97316",
    rays: typeof palette.rays === "string" ? palette.rays : "#fb923c",
    glow: typeof palette.glow === "string" ? palette.glow : "#fde68a"
  };
}

function createRay(center, innerRadius, outerRadius, angle, color, width) {
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  const innerPoint = [center[0] + cos * innerRadius, center[1] + sin * innerRadius];
  const outerPoint = [center[0] + cos * outerRadius, center[1] + sin * outerRadius];
  return {
    type: "line",
    from: innerPoint,
    to: outerPoint,
    stroke: color,
    width
  };
}

function sun(rawOptions = {}) {
  const options = ensureObject(rawOptions, {});
  const position = normalizePoint(options.position, [0, 0]);
  const radius = clampNumber(options.radius, 2, 30, 6);
  const rayLength = clampNumber(options.rayLength, radius * 0.5, radius * 2.5, radius * 1.4);
  const rayInset = clampNumber(options.rayInset, 0, radius * 0.6, radius * 0.2);
  const rayCount = clampInteger(options.rays, 4, 32, 12);
  const rotation = degreesToRadians(toNumber(options.rotation, 0));
  const palette = selectPalette(options.palette);

  const graphics = [];

  graphics.push({
    type: "circle",
    center: position,
    radius: radius * 1.35,
    fill: palette.glow,
    stroke: palette.glow,
    width: radius * 0.1,
    opacity: 0.25
  });

  for (let index = 0; index < rayCount; index++) {
    const angle = rotation + (index / rayCount) * TAU_APPROX;
    graphics.push(createRay(position, radius - rayInset, radius + rayLength, angle, palette.rays, Math.max(radius * 0.15, 0.4)));
  }

  graphics.push({
    type: "circle",
    center: position,
    radius,
    fill: palette.core,
    stroke: palette.outline,
    width: Math.max(radius * 0.3, 0.5)
  });

  graphics.push({
    type: "circle",
    center: [position[0] - radius * 0.35, position[1] + radius * 0.2],
    radius: radius * 0.35,
    fill: palette.glow,
    stroke: palette.glow,
    width: Math.max(radius * 0.08, 0.2),
    opacity: 0.8
  });

  return {
    graphics,
    center: position,
    radius,
    rays: rayCount
  };
}

return sun;

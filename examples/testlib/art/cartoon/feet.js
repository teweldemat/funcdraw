const defaultFeet = {
  radius: 1.4,
  horizontalScale: 2.45,
  forwardLength: 0.8,
  stroke: "#f97316",
  strokeWidth: 0.5
};

function normalizeOptions(value) {
  return value != null && typeof value === "object" ? value : {};
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
    if ("left" in value && "top" in value) {
      return [toNumber(value.left, fallback[0]), toNumber(value.top, fallback[1])];
    }
  }
  return fallback.slice();
}

function clampPositive(value, fallback, min = 0.01) {
  const result = toNumber(value, fallback);
  return result >= min ? result : fallback;
}

function normalizeSide(value, fallback = "left") {
  if (typeof value === "string") {
    const lowered = value.trim().toLowerCase();
    if (lowered === "right" || lowered === "left") {
      return lowered;
    }
  }
  return fallback;
}

function Feet(rawOptions = {}) {
  const options = normalizeOptions(rawOptions);
  const anklePoint = normalizePoint(options.anklePoint ?? options.anchor ?? options.position, [0, 0]);
  const side = normalizeSide(options.side);
  const baseRadius = clampPositive(options.radius, defaultFeet.radius, 0.05);
  const horizontalScale = clampPositive(options.horizontalScale, defaultFeet.horizontalScale, 0.2);
  const radiusX = baseRadius * horizontalScale;
  const forwardLength = clampPositive(options.forwardLength, defaultFeet.forwardLength, 0);
  const defaultLineLength = baseRadius + forwardLength;
  const lineLength = clampPositive(options.length, defaultLineLength, 0.1);
  const directionHint = normalizeSide(options.directionHint, side);
  const sign = directionHint === "right" ? 1 : -1;
  const style = normalizeOptions(options.style);
  const stroke =
    typeof options.stroke === "string"
      ? options.stroke
      : typeof style.stroke === "string"
        ? style.stroke
        : defaultFeet.stroke;
  const strokeWidth = clampPositive(style.width ?? options.strokeWidth, defaultFeet.strokeWidth, 0.01);
  const toePoint = [anklePoint[0] + sign * lineLength, anklePoint[1]];

  return {
    graphics: [
      {
        type: "line",
        from: anklePoint,
        to: toePoint,
        stroke,
        width: strokeWidth
      }
    ],
    anklePoint,
    center: toePoint,
    toePoint,
    side,
    direction: directionHint,
    length: lineLength
  };
}

return Feet;

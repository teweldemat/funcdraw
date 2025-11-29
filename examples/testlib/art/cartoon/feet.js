const defaultFeet = {
  radius: 1.4,
  horizontalScale: 2.45,
  forwardLength: 0.8,
  stroke: "#f97316",
  strokeWidth: 0.5
};

const helperCollection = typeof helpers === "object" ? helpers : null;
const normalizeInput = helperCollection?.normalizeInput;
const normalizePoint = helperCollection?.normalizePoint;
const resolveNumber = helperCollection?.resolveNumber;

function requireHelper(fn, name) {
  if (typeof fn !== "function") {
    throw new Error(`cartoon/helpers/${name}.js must export a function as helpers.${name}`);
  }
}

requireHelper(normalizeInput, "normalizeInput");
requireHelper(normalizePoint, "normalizePoint");
requireHelper(resolveNumber, "resolveNumber");

function clampPositive(value, fallback, min = 0.01) {
  const result = resolveNumber(value, fallback);
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
  const options = normalizeInput(rawOptions, {});
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
  const style = normalizeInput(options.style, {});
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

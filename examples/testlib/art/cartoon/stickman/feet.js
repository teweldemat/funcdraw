const defaultFeet = {
  length: 2.2,
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

function normalizeDirection(value, fallback = "left") {
  if (typeof value === "string") {
    const lowered = value.trim().toLowerCase();
    if (lowered === "right" || lowered === "left") {
      return lowered;
    }
    if (lowered === "center" || lowered === "middle") {
      return "center";
    }
  }
  return fallback;
}

function createFeet(rawOptions = {}) {
  const options = normalizeInput(rawOptions, {});
  const anklePoint = normalizePoint(options.anklePoint ?? options.anchor ?? options.position, [0, 0]);
  const side = normalizeSide(options.side);
  const directionHint = normalizeDirection(options.directionHint, normalizeDirection(side, "left"));
  const lineLength = clampPositive(options.length, defaultFeet.length, 0.05);
  const style = normalizeInput(options.style, {});
  const stroke =
    typeof options.stroke === "string"
      ? options.stroke
      : typeof style.stroke === "string"
        ? style.stroke
        : defaultFeet.stroke;
  const strokeWidth = clampPositive(style.width ?? options.strokeWidth, defaultFeet.strokeWidth, 0.01);
  const half = lineLength / 2;
  const sign = directionHint === "right" ? 1 : -1;
  const fromPoint =
    directionHint === "center" ? [anklePoint[0] - half, anklePoint[1]] : anklePoint;
  const toePoint =
    directionHint === "center"
      ? [anklePoint[0] + half, anklePoint[1]]
      : [anklePoint[0] + sign * lineLength, anklePoint[1]];
  const centerPoint = directionHint === "center" ? anklePoint : toePoint;

  return {
    graphics: [
      {
        type: "line",
        from: fromPoint,
        to: toePoint,
        stroke,
        width: strokeWidth
      }
    ],
    anklePoint,
    center: centerPoint,
    toePoint,
    side,
    direction: directionHint,
    length: lineLength
  };
}

return createFeet;

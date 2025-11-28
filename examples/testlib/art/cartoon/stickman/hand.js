const defaultLengths = { upper: 4, lower: 3 };
const defaultStyle = { stroke: "#f97316", width: 0.8 };
const EPSILON = 1e-6;

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function normalizeOptions(options) {
  return options && typeof options === "object" ? options : {};
}

function isPointLike(value) {
  if (!value) return false;
  if (Array.isArray(value)) return true;
  if (typeof value === "object") {
    return "x" in value || "y" in value || "left" in value || "top" in value;
  }
  return false;
}

function normalizePoint(point, fallback) {
  const base = Array.isArray(fallback) ? fallback : [0, 0];

  if (!point) return base.slice();

  if (Array.isArray(point)) {
    const x = Number(point[0]) || 0;
    const y = Number(point[1]) || 0;
    return [x, y];
  }

  if (typeof point === "object") {
    if ("x" in point || "y" in point) {
      const x = Number(point.x) || 0;
      const y = Number(point.y) || 0;
      return [x, y];
    }

    if ("left" in point || "top" in point) {
      const x = Number(point.left) || 0;
      const y = Number(point.top) || 0;
      return [x, y];
    }
  }

  return base.slice();
}

function createSegmentedLimb(
  attachmentPoint,
  endPoint,
  upperLength,
  lowerLength,
  style,
  bendDirection
) {
  const safeUpper = Math.max(Math.abs(upperLength), EPSILON);
  const safeLower = Math.max(Math.abs(lowerLength), EPSILON);

  const dx = endPoint[0] - attachmentPoint[0];
  const dy = endPoint[1] - attachmentPoint[1];
  const distance = Math.sqrt(dx * dx + dy * dy);
  const maxReach = safeUpper + safeLower;
  const minReach = Math.abs(safeUpper - safeLower) + EPSILON;

  const direction =
    distance > EPSILON
      ? [dx / distance, dy / distance]
      : [0, 1];

  const reach = clamp(distance, minReach, maxReach);
  const reachTarget = [
    attachmentPoint[0] + direction[0] * reach,
    attachmentPoint[1] + direction[1] * reach
  ];

  const cosShoulder = clamp(
    (safeUpper * safeUpper + reach * reach - safeLower * safeLower) /
      (2 * safeUpper * reach),
    -1,
    1
  );
  const sinShoulder = Math.sqrt(Math.max(0, 1 - cosShoulder * cosShoulder));

  const perp = [-direction[1], direction[0]];
  const elbow = [
    attachmentPoint[0] +
      direction[0] * (safeUpper * cosShoulder) +
      perp[0] * (bendDirection * safeUpper * sinShoulder),
    attachmentPoint[1] +
      direction[1] * (safeUpper * cosShoulder) +
      perp[1] * (bendDirection * safeUpper * sinShoulder)
  ];

  const segments = [
    {
      type: "line",
      from: attachmentPoint,
      to: elbow,
      stroke: style.stroke,
      width: style.width
    },
    {
      type: "line",
      from: elbow,
      to: reachTarget,
      stroke: style.stroke,
      width: style.width
    }
  ];

  return { segments, attachmentPoint, elbow, reachTarget };
}

// createHand(attachmentPoint?, options?)
// - createHand({ ...options })
// Backwards‑compatible: old single‑arg object call still works.
function createHand(arg1, arg2) {
  let config;

  if (isPointLike(arg1)) {
    const options = normalizeOptions(arg2);
    config = { ...options, attachmentPoint: arg1 };
  } else {
    config = normalizeOptions(arg1);
  }

  const attachmentPoint = normalizePoint(config.attachmentPoint, [0, 0]);
  const targetPoint = normalizePoint(config.targetPoint, attachmentPoint);

  const rawLengths = config.lengths || {};
  const lengths = {
    upper:
      typeof rawLengths.upper === "number"
        ? rawLengths.upper
        : defaultLengths.upper,
    lower:
      typeof rawLengths.lower === "number"
        ? rawLengths.lower
        : defaultLengths.lower
  };

  const style = {
    ...defaultStyle,
    ...(config.style && typeof config.style === "object" ? config.style : {})
  };

  let bendDirection;
  if (typeof config.bendDirection === "number") {
    bendDirection = config.bendDirection;
  } else if (config.positiveBend === true) {
    bendDirection = 1;
  } else if (config.positiveBend === false) {
    bendDirection = -1;
  } else {
    bendDirection = targetPoint[0] >= attachmentPoint[0] ? 1 : -1;
  }

  const { segments, elbow, reachTarget } = createSegmentedLimb(
    attachmentPoint,
    targetPoint,
    lengths.upper,
    lengths.lower,
    style,
    bendDirection
  );

  return {
    graphics: segments,
    attachmentPoint,
    elbow,
    targetPoint,   // requested goal
    reachTarget,   // actual wrist position after clamping
    bendDirection,
    lengths,
    style
  };
}

return createHand;
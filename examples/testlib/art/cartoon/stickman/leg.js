const defaultLengths = { upper: 4.5, lower: 4 };
const defaultStyle = { stroke: "#0ea5e9", width: 1.1 };

function normalizeOptions(options) {
  return options && typeof options === "object" ? options : {};
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function normalizeVector(vector, fallback = [0, -1]) {
  if (Array.isArray(vector) && vector.length >= 2) {
    const x = typeof vector[0] === "number" ? vector[0] : Number(vector[0]);
    const y = typeof vector[1] === "number" ? vector[1] : Number(vector[1]);
    const magnitude = Math.sqrt(x * x + y * y);
    if (Number.isFinite(magnitude) && magnitude > 1e-6) {
      return [x / magnitude, y / magnitude];
    }
  }
  return fallback.slice();
}

function createSegmentedLimb(attachmentPoint, endPoint, upperLength, lowerLength, style, bendDirection) {
  const epsilon = 1e-6;
  const safeUpper = Math.max(upperLength, epsilon);
  const safeLower = Math.max(lowerLength, epsilon);

  const dx = endPoint[0] - attachmentPoint[0];
  const dy = endPoint[1] - attachmentPoint[1];
  const distance = Math.sqrt(dx * dx + dy * dy);
  const maxReach = safeUpper + safeLower;
  const minReach = Math.abs(safeUpper - safeLower) + epsilon;

  const direction =
    distance > epsilon
      ? [dx / distance, dy / distance]
      : [0, 1];

  const reach = clamp(distance, minReach, maxReach);
  const reachTarget = [
    attachmentPoint[0] + direction[0] * reach,
    attachmentPoint[1] + direction[1] * reach
  ];

  const cosShoulder = clamp(
    (safeUpper * safeUpper + reach * reach - safeLower * safeLower) / (2 * safeUpper * reach),
    -1,
    1
  );
  const sinShoulder = Math.sqrt(Math.max(0, 1 - cosShoulder * cosShoulder));
  const perp = [-direction[1], direction[0]];
  const knee = [
    attachmentPoint[0] + direction[0] * (safeUpper * cosShoulder) + perp[0] * (bendDirection * safeUpper * sinShoulder),
    attachmentPoint[1] + direction[1] * (safeUpper * cosShoulder) + perp[1] * (bendDirection * safeUpper * sinShoulder)
  ];

  return [
    {
      type: "line",
      from: attachmentPoint,
      to: knee,
      stroke: style.stroke,
      width: style.width
    },
    {
      type: "line",
      from: knee,
      to: reachTarget,
      stroke: style.stroke,
      width: style.width
    }
  ];
}

function createLeg(options) {
  const config = normalizeOptions(options);
  const attachmentPoint = config.attachmentPoint || [0, 0];
  const targetPoint = config.targetPoint || attachmentPoint;
  const lengths = config.lengths || defaultLengths;
  const style = {
    stroke: config.style?.stroke ?? defaultStyle.stroke,
    width: config.style?.width ?? defaultStyle.width
  };
  let bendDirection = null;
  if (typeof config.bendDirection === "number") {
    bendDirection = config.bendDirection;
  } else if (config.positiveBend === true) {
    bendDirection = 1;
  } else if (config.positiveBend === false) {
    bendDirection = -1;
  } else {
    bendDirection = targetPoint[0] >= attachmentPoint[0] ? 1 : -1;
  }

  const segments = createSegmentedLimb(
    attachmentPoint,
    targetPoint,
    lengths.upper,
    lengths.lower,
    style,
    bendDirection
  );
  const lastSegment = segments.length > 0 ? segments[segments.length - 1] : null;
  const resolvedTargetPoint =
    lastSegment && Array.isArray(lastSegment.to)
      ? lastSegment.to.slice()
      : Array.isArray(targetPoint)
        ? targetPoint.slice()
        : targetPoint;
  const resolvedDirection =
    lastSegment && Array.isArray(lastSegment.to) && Array.isArray(lastSegment.from)
      ? normalizeVector([lastSegment.to[0] - lastSegment.from[0], lastSegment.to[1] - lastSegment.from[1]])
      : normalizeVector();

  return {
    graphics: segments,
    attachmentPoint,
    targetPoint,
    resolvedTargetPoint,
    resolvedDirection
  };
}

return createLeg;

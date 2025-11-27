const defaultLengths = { upper: 4, lower: 3 };
const defaultStyle = { stroke: "#f97316", width: 0.8 };

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function normalizeOptions(options) {
  return options && typeof options === "object" ? options : {};
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
  const elbow = [
    attachmentPoint[0] + direction[0] * (safeUpper * cosShoulder) + perp[0] * (bendDirection * safeUpper * sinShoulder),
    attachmentPoint[1] + direction[1] * (safeUpper * cosShoulder) + perp[1] * (bendDirection * safeUpper * sinShoulder)
  ];

  return [
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
}

function createHand(options) {
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

  return {
    graphics: segments,
    attachmentPoint,
    targetPoint
  };
}

return createHand;

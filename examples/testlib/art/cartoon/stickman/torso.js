const defaultStyle = {
  fill: "#1f2937",
  stroke: "#cbd5f5",
  strokeWidth: 0.6
};

const helperCollection = typeof helpers === "object" ? helpers : null;
const normalizeInput = helperCollection?.normalizeInput;
const resolveNumber = helperCollection?.resolveNumber;
const normalizePoint = helperCollection?.normalizePoint;

function requireHelper(fn, name) {
  if (typeof fn !== "function") {
    throw new Error(`cartoon/helpers/${name}.js must export a function as helpers.${name}`);
  }
}

requireHelper(normalizeInput, "normalizeInput");
requireHelper(resolveNumber, "resolveNumber");
requireHelper(normalizePoint, "normalizePoint");

function normalizeDirection(value, fallback = "front") {
  if (typeof value !== "string") {
    return fallback;
  }
  const text = value.trim().toLowerCase();
  if (text === "front" || text === "back" || text === "left" || text === "right") {
    return text;
  }
  return fallback;
}

function createTorso(configInput = {}) {
  const config = normalizeInput(configInput, {});
  const centerBottomPoint = normalizePoint(config.centerBottomPoint, [20, 6]);
  const width = resolveNumber(config.width, 6);
  const height = resolveNumber(config.height, 11);
  const neckHeight = Math.max(resolveNumber(config.neckHeight, height * 0.14), 0);
  const neckWidthRatio = Math.min(Math.max(resolveNumber(config.neckWidthRatio, 0.35), 0.1), 1);
  const shoulderExtension = Math.max(resolveNumber(config.shoulderExtension, width * 0.1), 0);
  const waistRatio = Math.min(Math.max(resolveNumber(config.waistRatio, 0.65), 0.2), 1);
  const waistWidth = resolveNumber(config.waistWidth, width * waistRatio);
  const fill = config.fill ?? defaultStyle.fill;
  const stroke = config.stroke ?? defaultStyle.stroke;
  const strokeWidth = config.strokeWidth ?? defaultStyle.strokeWidth;
  const direction = normalizeDirection(config.direction, "front");
  const isProfile = direction === "left" || direction === "right";
  const facingSign = direction === "left" ? -1 : 1;

  const [centerX, bottomY] = centerBottomPoint;
  const halfWidth = width / 2;
  const waistHalfWidth = waistWidth / 2;
  const bodyTopY = bottomY + height - neckHeight;
  const topY = bottomY + height;
  const underarmY = topY - height * 0.3;
  const shoulderLift = Math.max(height * 0.08, shoulderExtension * 0.25);
  const shoulderLowerY = underarmY + shoulderLift * 0.5;
  const shoulderUpperY = topY - shoulderLift * 0.3;
  const shoulderPlateauHalf = Math.max(halfWidth * 0.18, shoulderExtension * 0.35);

  const profileWidth = isProfile ? Math.max(width * 0.32, waistHalfWidth * 0.45) : 0;

  let neckHalfWidth = Math.max((shoulderPlateauHalf + waistHalfWidth) * 0.35, (width * neckWidthRatio) / 2);
  if (isProfile) {
    const profileNeckHalf = Math.max(profileWidth * 0.25, width * 0.08);
    neckHalfWidth = Math.min(neckHalfWidth, profileNeckHalf);
  }
  const neckOffsetX = isProfile ? 0 : facingSign * Math.min(neckHalfWidth * 0.6, profileWidth * 0.4);
  const neckCenterX = centerX + neckOffsetX;
  const neck = neckHeight
    ? [
        {
          type: "rect",
          position: [neckCenterX - neckHalfWidth, bodyTopY],
          size: [neckHalfWidth * 2, neckHeight],
          fill,
          stroke,
          width: strokeWidth
        }
      ]
    : [];

  let bodyPoints;
  if (isProfile) {
    const profileHalf = profileWidth / 2;
    const backEdgeX = centerX - facingSign * profileHalf;
    const frontEdgeX = centerX + facingSign * profileHalf;

    bodyPoints = [
      [backEdgeX, bottomY],
      [backEdgeX, bodyTopY],
      [frontEdgeX, bodyTopY],
      [frontEdgeX, bottomY]
    ];

  } else {
    bodyPoints = [
      [centerX - waistHalfWidth, bottomY],
      [centerX - halfWidth * 0.9, bottomY + height * 0.25],
      [centerX - halfWidth, underarmY],
      [centerX - (halfWidth + shoulderExtension), shoulderLowerY],
      [centerX - (halfWidth + shoulderExtension), shoulderUpperY],
      [centerX - shoulderPlateauHalf, bodyTopY],
      [centerX + shoulderPlateauHalf, bodyTopY],
      [centerX + (halfWidth + shoulderExtension), shoulderUpperY],
      [centerX + (halfWidth + shoulderExtension), shoulderLowerY],
      [centerX + halfWidth, underarmY],
      [centerX + halfWidth * 0.9, bottomY + height * 0.25],
      [centerX + waistHalfWidth, bottomY]
    ];

  }

  const body = {
    type: "polygon",
    points: bodyPoints,
    fill,
    stroke,
    width: strokeWidth
  };

  return {
    graphics: [...neck, body]
  };
}

return createTorso;

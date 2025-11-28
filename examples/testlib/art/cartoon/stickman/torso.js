const defaultStyle = {
  fill: "#1f2937",
  stroke: "#cbd5f5",
  strokeWidth: 0.6
};

function normalizeConfig(config) {
  return config && typeof config === "object" ? config : {};
}

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
    if ("left" in value && "bottom" in value) {
      return [toNumber(value.left, fallback[0]), toNumber(value.bottom, fallback[1])];
    }
  }
  return fallback.slice();
}

function createTorso(configInput = {}) {
  const config = normalizeConfig(configInput);
  const centerBottomPoint = normalizePoint(config.centerBottomPoint, [20, 6]);
  const width = typeof config.width === "number" ? config.width : 6;
  const height = typeof config.height === "number" ? config.height : 11;
  const neckHeight = Math.max(toNumber(config.neckHeight, height * 0.14), 0);
  const neckWidthRatio = Math.min(Math.max(toNumber(config.neckWidthRatio, 0.35), 0.1), 1);
  const shoulderExtension = Math.max(toNumber(config.shoulderExtension, width * 0.1), 0);
  const waistRatio = Math.min(Math.max(toNumber(config.waistRatio, 0.65), 0.2), 1);
  const waistWidth = typeof config.waistWidth === "number" ? config.waistWidth : width * waistRatio;
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

  const handsY = topY - height * 0.15;
  const legOffset = width * 0.25;
  const handHorizontalOffset = halfWidth + shoulderExtension;
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
  let handAttachmentPoints;
  let legAttachmentPoints;

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

    handAttachmentPoints = {
      left: [centerX, handsY],
      right: [centerX, handsY]
    };
    legAttachmentPoints = {
      left: [centerX, bottomY],
      right: [centerX, bottomY]
    };
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

    handAttachmentPoints = {
      left: [centerX - handHorizontalOffset, handsY],
      right: [centerX + handHorizontalOffset, handsY]
    };
    legAttachmentPoints = {
      left: [centerX - legOffset, bottomY],
      right: [centerX + legOffset, bottomY]
    };
  }

  const body = {
    type: "polygon",
    points: bodyPoints,
    fill,
    stroke,
    width: strokeWidth
  };

  return {
    graphics: [...neck, body],
    handAttachmentPoints,
    legAttachmentPoints,
    headAttachmentPoint: [centerX, topY]
  };
}

return createTorso;

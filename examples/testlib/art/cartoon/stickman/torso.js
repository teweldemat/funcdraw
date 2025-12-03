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
  const shoulderExtension = Math.max(resolveNumber(config.shoulderExtension, width * 0.1), 0);
  const stroke = config.stroke ?? defaultStyle.stroke;
  const strokeWidth = config.strokeWidth ?? defaultStyle.strokeWidth;
  const direction = normalizeDirection(config.direction, "front");
  const isProfile = direction === "left" || direction === "right";

  const [centerX, bottomY] = centerBottomPoint;
  const halfWidth = width / 2;
  const topY = bottomY + height;

  const defaultHandsY = topY - height * 0.15;
  const handOffset = halfWidth + shoulderExtension;
  const handAttachments = {
    left: normalizePoint(config.handAttachmentPoints?.left, isProfile ? [centerX, defaultHandsY] : [centerX - handOffset, defaultHandsY]),
    right: normalizePoint(config.handAttachmentPoints?.right, isProfile ? [centerX, defaultHandsY] : [centerX + handOffset, defaultHandsY])
  };

  const legOffset = width * 0.25;
  const legAttachments = {
    left: normalizePoint(config.legAttachmentPoints?.left, isProfile ? [centerX, bottomY] : [centerX - legOffset, bottomY]),
    right: normalizePoint(config.legAttachmentPoints?.right, isProfile ? [centerX, bottomY] : [centerX + legOffset, bottomY])
  };

  const headAttachment = normalizePoint(config.headAttachmentPoint, [centerX, topY]);
  const legMidpoint = [
    (legAttachments.left[0] + legAttachments.right[0]) / 2,
    (legAttachments.left[1] + legAttachments.right[1]) / 2
  ];

  const graphics = [
    {
      type: "line",
      from: legAttachments.left,
      to: legAttachments.right,
      stroke,
      width: strokeWidth
    },
    {
      type: "line",
      from: handAttachments.left,
      to: handAttachments.right,
      stroke,
      width: strokeWidth
    },
    {
      type: "line",
      from: headAttachment,
      to: legMidpoint,
      stroke,
      width: strokeWidth
    }
  ];

  return {
    graphics
  };
}

return createTorso;

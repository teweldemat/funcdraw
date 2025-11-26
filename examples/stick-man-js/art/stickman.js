const defaultPalette = {
  torsoFill: "#1f2937",
  torsoStroke: "#cbd5f5",
  torsoStrokeWidth: 0.6,
  headFill: "#fff7ed",
  headStroke: "#fdba74",
  headStrokeWidth: 0.4,
  headGazeColor: "#ea580c",
  handStroke: "#f97316",
  handWidth: 0.8,
  legStroke: "#0ea5e9",
  legWidth: 1.1,
  overlayHand: "#fb7185",
  overlayLeg: "#38bdf8"
};

const DEFAULT_TORSO_WIDTH = 6;
const DEFAULT_TORSO_HEIGHT = 11;

const defaultMeasurements = {
  torso: {
    width: DEFAULT_TORSO_WIDTH,
    height: DEFAULT_TORSO_HEIGHT
  },
  head: {
    verticalExtent: 4.5,
    angle: 90
  },
  hands: {
    retractDistance: 0.75,
    left: {
      upperLength: 4,
      lowerLength: 3,
      effectorCoordinate: [-11, 11],
      positiveBend: true
    },
    right: {
      upperLength: 4,
      lowerLength: 3,
      effectorCoordinate: [11, 11],
      positiveBend: true
    }
  },
  legs: {
    left: {
      upperLength: 4.5,
      lowerLength: 4,
      effectorCoordinate: [-4, -4.5],
      positiveBend: false
    },
    right: {
      upperLength: 4.5,
      lowerLength: 4,
      effectorCoordinate: [4, -4.5],
      positiveBend: false
    }
  }
};

const FS_TYPE = {
  NULL: 0,
  BOOLEAN: 1,
  INTEGER: 2,
  FLOAT: 6,
  STRING: 7,
  LIST: 9,
  KVC: 10
};

function fsValueToJs(value) {
  if (!Array.isArray(value) || value.length !== 2) {
    return value;
  }
  const [type, raw] = value;
  switch (type) {
    case FS_TYPE.NULL:
      return null;
    case FS_TYPE.BOOLEAN:
    case FS_TYPE.INTEGER:
    case FS_TYPE.FLOAT:
    case FS_TYPE.STRING:
      return raw;
    case FS_TYPE.LIST: {
      const list = [];
      if (raw && typeof raw[Symbol.iterator] === "function") {
        for (const item of raw) {
          list.push(fsValueToJs(item));
        }
      }
      return list;
    }
    case FS_TYPE.KVC: {
      const obj = {};
      if (raw && typeof raw.getAll === "function") {
        for (const [key, val] of raw.getAll()) {
          obj[key] = fsValueToJs(val);
        }
      }
      return obj;
    }
    default:
      return raw;
  }
}

function normalizeInput(value, fallback) {
  if (value == null) {
    return fallback;
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[0] === "number" && typeof value[1] !== "number") {
    const converted = fsValueToJs(value);
    return converted ?? fallback;
  }
  if (value.__fsKind === "KeyValueCollection") {
    return fsValueToJs([FS_TYPE.KVC, value]) ?? fallback;
  }
  return value;
}

function normalizePoint(value, fallback) {
  if (!value) {
    return fallback;
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[0] === "number" && typeof value[1] === "number") {
    return value;
  }
  if (Array.isArray(value) && value.length === 2) {
    const converted = fsValueToJs(value);
    return Array.isArray(converted) ? converted : fallback;
  }
  if (value.__fsKind === "FsList") {
    const converted = fsValueToJs([FS_TYPE.LIST, value]);
    return Array.isArray(converted) ? converted : fallback;
  }
  return fallback;
}

function mergeDeep(target, source) {
  if (!source || typeof source !== "object") {
    return target;
  }
  const output = Array.isArray(target) ? target.slice() : { ...target };
  for (const [key, value] of Object.entries(source)) {
    if (value && typeof value === "object" && !Array.isArray(value)) {
      output[key] = mergeDeep(
        Object.prototype.hasOwnProperty.call(output, key) && typeof output[key] === "object" ? output[key] : {},
        value
      );
    } else {
      output[key] = value;
    }
  }
  return output;
}

function addOffset(point, offset) {
  return [point[0] + offset[0], point[1] + offset[1]];
}

function resolveNumber(value, fallback) {
  return typeof value === "number" ? value : fallback;
}

function resolveBoolean(value, fallback) {
  return typeof value === "boolean" ? value : fallback;
}

function stickMan(optionsInput = {}) {
  const stickImports = {
    torso: provider.import("torso"),
    head: provider.import("head"),
    hand: provider.import("hand"),
    leg: provider.import("leg")
  };

  const normalizedOptions = normalizeInput(optionsInput, {});
  const position = normalizePoint(normalizedOptions.position, [20, 6]);
  const measurements = mergeDeep(defaultMeasurements, normalizeInput(normalizedOptions.measurements, {}));
  const palette = mergeDeep(defaultPalette, normalizeInput(normalizedOptions.palette, {}));

  const handStroke = normalizedOptions.palette?.handStroke ?? palette.handStroke;
  const legStroke = normalizedOptions.palette?.legStroke ?? palette.legStroke;

  const torsoResult = stickImports.torso({
    centerBottomPoint: position,
    width: measurements.torso.width,
    height: measurements.torso.height,
    fill: palette.torsoFill,
    stroke: palette.torsoStroke,
    strokeWidth: palette.torsoStrokeWidth
  });

  const headResult = stickImports.head(torsoResult.headAttachmentPoint, {
    verticalExtent: measurements.head.verticalExtent,
    angle: measurements.head.angle,
    fill: palette.headFill,
    stroke: palette.headStroke,
    strokeWidth: palette.headStrokeWidth,
    gazeColor: palette.headGazeColor
  });

  const handMeasurements = measurements.hands;
  const handDefaults = defaultMeasurements.hands;
  const leftHandAttachmentPoint = torsoResult.handAttachmentPoints.left;
  const rightHandAttachmentPoint = torsoResult.handAttachmentPoints.right;
  const leftHandTargetPoint = addOffset(
    position,
    normalizePoint(handMeasurements.left.effectorCoordinate, handDefaults.left.effectorCoordinate)
  );
  const rightHandTargetPoint = addOffset(
    position,
    normalizePoint(handMeasurements.right.effectorCoordinate, handDefaults.right.effectorCoordinate)
  );
  const handRetract = resolveNumber(handMeasurements.retractDistance, handDefaults.retractDistance);
  const leftHandRetract = resolveNumber(handMeasurements.left.retractDistance, handRetract);
  const rightHandRetract = resolveNumber(handMeasurements.right.retractDistance, handRetract);

  const leftHand = stickImports.hand({
    attachmentPoint: leftHandAttachmentPoint,
    targetPoint: leftHandTargetPoint,
    lengths: {
      upper: resolveNumber(handMeasurements.left.upperLength, handDefaults.left.upperLength),
      lower: resolveNumber(handMeasurements.left.lowerLength, handDefaults.left.lowerLength)
    },
    retractDistance: leftHandRetract,
    positiveBend: resolveBoolean(handMeasurements.left.positiveBend, handDefaults.left.positiveBend),
    style: { stroke: handStroke, width: palette.handWidth }
  });

  const rightHand = stickImports.hand({
    attachmentPoint: rightHandAttachmentPoint,
    targetPoint: rightHandTargetPoint,
    lengths: {
      upper: resolveNumber(handMeasurements.right.upperLength, handDefaults.right.upperLength),
      lower: resolveNumber(handMeasurements.right.lowerLength, handDefaults.right.lowerLength)
    },
    retractDistance: rightHandRetract,
    positiveBend: resolveBoolean(handMeasurements.right.positiveBend, handDefaults.right.positiveBend),
    style: { stroke: handStroke, width: palette.handWidth }
  });

  const legMeasurements = measurements.legs;
  const legDefaults = defaultMeasurements.legs;
  const leftLegAttachmentPoint = torsoResult.legAttachmentPoints.left;
  const rightLegAttachmentPoint = torsoResult.legAttachmentPoints.right;
  const leftLegTargetPoint = addOffset(
    position,
    normalizePoint(legMeasurements.left.effectorCoordinate, legDefaults.left.effectorCoordinate)
  );
  const rightLegTargetPoint = addOffset(
    position,
    normalizePoint(legMeasurements.right.effectorCoordinate, legDefaults.right.effectorCoordinate)
  );

  const leftLeg = stickImports.leg({
    attachmentPoint: leftLegAttachmentPoint,
    targetPoint: leftLegTargetPoint,
    lengths: {
      upper: resolveNumber(legMeasurements.left.upperLength, legDefaults.left.upperLength),
      lower: resolveNumber(legMeasurements.left.lowerLength, legDefaults.left.lowerLength)
    },
    positiveBend: resolveBoolean(legMeasurements.left.positiveBend, legDefaults.left.positiveBend),
    style: { stroke: legStroke, width: palette.legWidth }
  });

  const rightLeg = stickImports.leg({
    attachmentPoint: rightLegAttachmentPoint,
    targetPoint: rightLegTargetPoint,
    lengths: {
      upper: resolveNumber(legMeasurements.right.upperLength, legDefaults.right.upperLength),
      lower: resolveNumber(legMeasurements.right.lowerLength, legDefaults.right.lowerLength)
    },
    positiveBend: resolveBoolean(legMeasurements.right.positiveBend, legDefaults.right.positiveBend),
    style: { stroke: legStroke, width: palette.legWidth }
  });

  const overlays = [
    { point: leftHandAttachmentPoint, color: palette.overlayHand },
    { point: leftHand.targetPoint, color: palette.overlayHand },
    { point: rightHandAttachmentPoint, color: palette.overlayHand },
    { point: rightHand.targetPoint, color: palette.overlayHand },
    { point: leftLegAttachmentPoint, color: palette.overlayLeg },
    { point: leftLeg.targetPoint, color: palette.overlayLeg },
    { point: rightLegAttachmentPoint, color: palette.overlayLeg },
    { point: rightLeg.targetPoint, color: palette.overlayLeg }
  ];

  return {
    graphics: [
      ...torsoResult.graphics,
      ...headResult.graphics,
      ...leftHand.graphics,
      ...rightHand.graphics,
      ...leftLeg.graphics,
      ...rightLeg.graphics
    ],
    overlays
  };
}

return stickMan;

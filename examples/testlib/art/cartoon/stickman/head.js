const defaultHeadConfig = {
  verticalExtent: 4.5,
  angle: 90,
  fill: "#fff7ed",
  stroke: "#fdba74",
  strokeWidth: 0.4,
  gazeColor: "#ea580c",
  segments: 20,
  eyes: {
    separationRatio: 0.38,
    offsetRatio: 0.2,
    radiusRatio: 0.14,
    fill: "#0f172a",
    stroke: "#0f172a",
    highlight: "#fef9c3",
    highlightRatio: 0.4
  }
};

const PI = Math.PI;
const HALF_PI = PI / 2;
const TAU = PI * 2;
const MIN_VERTICAL_EXTENT = 1;
const MIN_SEGMENTS = 6;

const { normalizePoint, normalizeInput, clamp, resolveNumber } = helpers;

function normalizeDirection(value) {
  if (typeof value !== "string") {
    return null;
  }
  const trimmed = value.trim().toLowerCase();
  if (
    trimmed === "left" ||
    trimmed === "right" ||
    trimmed === "front" ||
    trimmed === "back"
  ) {
    return trimmed;
  }
  return null;
}

function degToRad(degrees) {
  return (degrees * PI) / 180;
}

function ensureFiniteNumber(value, fallback) {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function mixEyesConfig(rawConfig, base) {
  const config = normalizeInput(rawConfig, {});
  const merged = { ...base, ...config };

  const separationRatio = clamp(
    resolveNumber(merged.separationRatio, base.separationRatio ?? 0.38),
    0.1,
    0.8
  );
  const offsetRatio = clamp(
    resolveNumber(merged.offsetRatio, base.offsetRatio ?? 0.2),
    -0.2,
    0.6
  );
  const radiusRatio = clamp(
    resolveNumber(merged.radiusRatio, base.radiusRatio ?? 0.14),
    0.05,
    0.35
  );
  const highlightRatio = clamp(
    resolveNumber(merged.highlightRatio, base.highlightRatio ?? 0.4),
    0,
    1
  );

  const fill = merged.fill ?? base.fill ?? "#0f172a";
  const stroke = merged.stroke ?? merged.fill ?? base.stroke ?? fill;
  const highlight = merged.highlight ?? base.highlight ?? "#fef9c3";

  return {
    separationRatio,
    offsetRatio,
    radiusRatio,
    highlightRatio,
    fill,
    stroke,
    highlight
  };
}

function createHead(attachmentPointInput, configInput) {
  const attachmentPoint = normalizePoint(attachmentPointInput, [20, 17]);
  const rawConfig = normalizeInput(configInput, {});

  const verticalExtent = Math.max(
    resolveNumber(rawConfig.verticalExtent, defaultHeadConfig.verticalExtent),
    MIN_VERTICAL_EXTENT
  );
  const radius = verticalExtent / 2;

  const angleDeg = resolveNumber(rawConfig.angle, defaultHeadConfig.angle);
  const angleRad = ensureFiniteNumber(degToRad(angleDeg), HALF_PI);

  const fill = rawConfig.fill ?? defaultHeadConfig.fill;
  const stroke = rawConfig.stroke ?? defaultHeadConfig.stroke;
  const strokeWidth = Math.max(
    0,
    resolveNumber(rawConfig.strokeWidth, defaultHeadConfig.strokeWidth)
  );
  const gazeColor = rawConfig.gazeColor ?? defaultHeadConfig.gazeColor;

  const segmentsRaw = Math.floor(
    resolveNumber(rawConfig.segments, defaultHeadConfig.segments)
  );
  const segments = Math.max(MIN_SEGMENTS, segmentsRaw || MIN_SEGMENTS);

  const direction = normalizeDirection(rawConfig.direction);
  const eyesConfig = mixEyesConfig(rawConfig.eyes, defaultHeadConfig.eyes || {});

  const isProfile = direction === "left" || direction === "right";
  const isFront = direction === "front";
  const isBack = direction === "back";

  const centerAngle = ensureFiniteNumber(
    isProfile ? HALF_PI : angleRad,
    HALF_PI
  );

  const center = [
    attachmentPoint[0] + Math.cos(centerAngle) * radius,
    attachmentPoint[1] + Math.sin(centerAngle) * radius
  ];

  const neckUp = [Math.cos(centerAngle), Math.sin(centerAngle)];
  const headDown = [-neckUp[0], -neckUp[1]];

  const tiltFromVertical = angleRad - HALF_PI;

  let lookAngle;
  if (direction === "left") {
    lookAngle = PI + tiltFromVertical;
  } else if (direction === "right") {
    lookAngle = tiltFromVertical;
  } else if (direction === "back") {
    lookAngle = -HALF_PI + tiltFromVertical;
  } else {
    lookAngle = angleRad;
  }
  const lookRadians = ensureFiniteNumber(lookAngle, centerAngle);

  const forward = [Math.cos(lookRadians), Math.sin(lookRadians)];
  const right = [
    Math.cos(lookRadians + HALF_PI),
    Math.sin(lookRadians + HALF_PI)
  ];

  const outlinePoints = [];
  for (let i = 0; i < segments; i += 1) {
    const theta = (i / segments) * TAU;
    outlinePoints.push([
      center[0] + Math.cos(theta) * radius,
      center[1] + Math.sin(theta) * radius
    ]);
  }

  const outline = {
    type: "polygon",
    points: outlinePoints,
    fill,
    stroke,
    width: strokeWidth
  };

  const eyes = [];
  const facialDetails = [];

  const eyeBase = [
    center[0] + forward[0] * (radius * eyesConfig.offsetRatio),
    center[1] + forward[1] * (radius * eyesConfig.offsetRatio)
  ];
  const lateralDistance = radius * eyesConfig.separationRatio;
  const eyeRadius = radius * eyesConfig.radiusRatio;
  const highlightRadius = eyeRadius * eyesConfig.highlightRatio;

  const leftEyeCenter = [
    eyeBase[0] - right[0] * lateralDistance,
    eyeBase[1] - right[1] * lateralDistance
  ];
  const rightEyeCenter = [
    eyeBase[0] + right[0] * lateralDistance,
    eyeBase[1] + right[1] * lateralDistance
  ];

  const createRoundEye = (eyeCenter) => {
    const shapes = [
      {
        type: "circle",
        center: eyeCenter,
        radius: eyeRadius,
        fill: eyesConfig.fill,
        stroke: eyesConfig.stroke,
        width: strokeWidth * 0.75
      }
    ];

    if (highlightRadius > 0) {
      shapes.push({
        type: "circle",
        center: [
          eyeCenter[0] +
            forward[0] * eyeRadius * 0.25 -
            right[0] * eyeRadius * 0.2,
          eyeCenter[1] +
            forward[1] * eyeRadius * 0.25 -
            right[1] * eyeRadius * 0.2
        ],
        radius: highlightRadius,
        fill: eyesConfig.highlight,
        stroke: eyesConfig.highlight,
        width: highlightRadius * 0.5,
        opacity: 0.85
      });
    }

    return shapes;
  };

  if (!isBack && !isProfile) {
    eyes.push(
      ...createRoundEye(leftEyeCenter),
      ...createRoundEye(rightEyeCenter)
    );

    if (isFront) {
      const noseRoot = [
        eyeBase[0] + headDown[0] * radius * 0.15,
        eyeBase[1] + headDown[1] * radius * 0.15
      ];
      const noseTip = [
        noseRoot[0] + headDown[0] * radius * 0.4,
        noseRoot[1] + headDown[1] * radius * 0.4
      ];

      facialDetails.push({
        type: "line",
        from: noseRoot,
        to: noseTip,
        stroke: gazeColor,
        width: strokeWidth * 0.9
      });
    }
  } else if (isProfile) {
    const profileSign = direction === "left" ? -1 : 1;

    const singleEyeCenter = [
      center[0] + profileSign * radius * 0.65,
      center[1] + radius * 0.05
    ];
    const singleEyeOuterRadius = Math.max(eyeRadius * 1.25, radius * 0.08);

    eyes.push(
      {
        type: "circle",
        center: singleEyeCenter,
        radius: singleEyeOuterRadius,
        fill: "#f8fafc",
        stroke: eyesConfig.stroke,
        width: strokeWidth * 0.9
      },
      {
        type: "circle",
        center: [
          singleEyeCenter[0] + profileSign * singleEyeOuterRadius * 0.15,
          singleEyeCenter[1]
        ],
        radius: singleEyeOuterRadius * 0.45,
        fill: eyesConfig.fill,
        stroke: eyesConfig.fill,
        width: strokeWidth * 0.7
      }
    );

    if (highlightRadius > 0) {
      eyes.push({
        type: "circle",
        center: [
          singleEyeCenter[0] + profileSign * singleEyeOuterRadius * 0.3,
          singleEyeCenter[1] - singleEyeOuterRadius * 0.2
        ],
        radius: singleEyeOuterRadius * 0.25,
        fill: eyesConfig.highlight,
        stroke: eyesConfig.highlight,
        width: singleEyeOuterRadius * 0.12,
        opacity: 0.85
      });
    }

    const noseRoot = [
      singleEyeCenter[0] + headDown[0] * radius * 0.1,
      singleEyeCenter[1] + headDown[1] * radius * 0.1
    ];

    const noseTip = [
      noseRoot[0] + profileSign * radius * 0.45,
      noseRoot[1] - radius * 0.05
    ];

    const noseBottom = [
      noseTip[0],
      noseTip[1] - radius * 0.18
    ];

    facialDetails.push(
      {
        type: "line",
        from: noseRoot,
        to: noseTip,
        stroke: gazeColor,
        width: strokeWidth * 0.9
      },
      {
        type: "line",
        from: noseTip,
        to: noseBottom,
        stroke: gazeColor,
        width: strokeWidth * 0.9
      }
    );
  }

  const graphics = [outline, ...eyes, ...facialDetails];

  return {
    graphics
  };
}

return createHead;

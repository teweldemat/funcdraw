const view = { left: 0, bottom: 0, right: 40, top: 30 };
const stickManBuilder = stickman;

const groundY = 0;
const hipHeight = 10.5;
const time = getTimeHook();
const { left: leftHandTarget, right: rightHandTarget } = createHandSwingTargets(time);

const figure = stickManBuilder({
  position: [20, groundY + hipHeight],
  measurements: {
    hands: {
      left: {
        upperLength: 6,
        lowerLength: 6,
        effectorCoordinate: leftHandTarget,
        positiveBend: true
      },
      right: {
        upperLength: 6,
        lowerLength: 6,
        effectorCoordinate: rightHandTarget,
        positiveBend: false
      }
    },
    legs: {
      left: {
        upperLength: 6,
        lowerLength: 5,
        effectorCoordinate: [-4, -hipHeight],
        positiveBend: false
      },
      right: {
        upperLength: 6,
        lowerLength: 5,
        effectorCoordinate: [4, -hipHeight],
        positiveBend: true
      }
    }
  },
  palette: {
    overlayHand: "#fb7185",
    overlayLeg: "#38bdf8"
  }
});

return {
  view,
  graphics: [
    createGround(groundY),
    ...figure.graphics,
    ...renderOverlays(figure.overlays || [])
  ]
};

function createGround(y) {
  return {
    type: "line",
    from: [view.left, y],
    to: [view.right, y],
    stroke: "#64748b",
    width: 0.5
  };
}

function crossMarker(point, color, size = 0.6) {
  return [
    {
      type: "line",
      from: [point[0] - size, point[1] - size],
      to: [point[0] + size, point[1] + size],
      stroke: color,
      width: 0.35
    },
    {
      type: "line",
      from: [point[0] - size, point[1] + size],
      to: [point[0] + size, point[1] - size],
      stroke: color,
      width: 0.35
    }
  ];
}

function renderOverlays(points) {
  const graphics = [];
  for (const item of points) {
    if (!item || !Array.isArray(item.point) || typeof item.point[0] !== "number") {
      continue;
    }
    graphics.push(...crossMarker(item.point, item.color || "#ffffff"));
  }
  return graphics;
}

function createHandSwingTargets(time) {
  const SPEED = 0.6;
  const lift = easeInOutCubic(triangleWave(time * SPEED));

  const CLAP_START = 0.85;
  const clapAmount = clamp(
    (lift - CLAP_START) / (1 - CLAP_START),
    0,
    1
  );

  return {
    left: computeHandTarget(-1, lift, clapAmount),
    right: computeHandTarget(1, lift, clapAmount)
  };
}

function computeHandTarget(direction, lift, clap) {
  const yDown = 0.5;
  const yTop = hipHeight + 13;
  const xWide = 12.5;

  const y = interpolateScalar(yDown, yTop, lift);

  const xWideDir = direction * xWide;
  const x = interpolateScalar(xWideDir, 0, clap);

  return [x, y];
}

function interpolatePoints(start, end, t) {
  return [
    interpolateScalar(start[0], end[0], t),
    interpolateScalar(start[1], end[1], t)
  ];
}

function interpolateScalar(start, end, t) {
  return start + (end - start) * t;
}

function triangleWave(value) {
  const cycle = positiveMod(value, 2);
  return cycle <= 1 ? cycle : 2 - cycle;
}

function positiveMod(value, modulus) {
  const result = value % modulus;
  return result < 0 ? result + modulus : result;
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function easeInOutCubic(t) {
  return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
}

function easeOutCubic(t) {
  return 1 - Math.pow(1 - t, 3);
}

function easeInCubic(t) {
  return t * t * t;
}

function getTimeHook() {
  if (!provider || typeof provider.get !== "function") {
    return 0;
  }
  try {
    if (typeof provider.isDefined === "function" && !provider.isDefined("t")) {
      return 0;
    }
    return ensureNumber(provider.get("t"));
  } catch {
    return 0;
  }
}

function ensureNumber(value) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[1] === "number") {
    return Number.isFinite(value[1]) ? value[1] : 0;
  }
  const numeric = Number(value);
  return Number.isFinite(numeric) ? numeric : 0;
}

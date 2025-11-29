const DEFAULT_POSITION = [0, 10];
const DEFAULT_LEFT_OFFSET = [-2, -11];
const DEFAULT_RIGHT_OFFSET = [2, -11];

const baseStaticBuilder = typeof staticMan === "function" ? staticMan : () => ({ graphics: [] });
const distanceBetweenPoints = typeof distance === "function" ? distance : fallbackDistance;
const baseSkeleton = typeof baseStaticBuilder.skeleton === "function" ? baseStaticBuilder.skeleton() : null;
const skeletonPosition = isPoint(baseSkeleton?.position) ? baseSkeleton.position : DEFAULT_POSITION;
const defaultLeftFoot = isPoint(baseSkeleton?.legs?.left?.targetPoint)
  ? baseSkeleton.legs.left.targetPoint
  : addPoints(skeletonPosition, DEFAULT_LEFT_OFFSET);
const defaultRightFoot = isPoint(baseSkeleton?.legs?.right?.targetPoint)
  ? baseSkeleton.legs.right.targetPoint
  : addPoints(skeletonPosition, DEFAULT_RIGHT_OFFSET);
const defaultLeftOffset = subtractPoints(defaultLeftFoot, skeletonPosition);
const defaultRightOffset = subtractPoints(defaultRightFoot, skeletonPosition);
const defaultOffsetsBySide = {
  left: defaultLeftOffset,
  right: defaultRightOffset
};
const MAX_VERTICAL_ANCHOR_DELTA = 1.2;

function steperMan(optionsInput = {}) {
  const options = ensureObject(optionsInput);
  const fallbackPosition = toPoint(options.position, DEFAULT_POSITION);
  const fixedSide = normalizeSide(options.fixedFeet, "left");
  const movingSide = fixedSide === "left" ? "right" : "left";
  const defaultFixedWorld = addPoints(fallbackPosition, defaultOffsetsBySide[fixedSide]);
  const defaultMovingWorld = addPoints(fallbackPosition, defaultOffsetsBySide[movingSide]);

  const fixedWorldPoint = toPoint(options.fixedFeetPoint, defaultFixedWorld);
  const movingStartWorld = toPoint(options.movingFeetStartPoint, defaultMovingWorld);
  const movingTargetWorld = toPoint(options.movingFeetTargetPoint, movingStartWorld);
  const progress = clamp01(toNumber(options.progress, 0));
  const arcHeight = resolveArcHeight(movingStartWorld, movingTargetWorld);
  const movingWorldPoint = computeArcPoint(movingStartWorld, movingTargetWorld, progress, arcHeight);
  const anchorCandidates = [];
  anchorCandidates.push(subtractPoints(fixedWorldPoint, defaultOffsetsBySide[fixedSide]));
  anchorCandidates.push(subtractPoints(movingWorldPoint, defaultOffsetsBySide[movingSide]));
  const averagedAnchor = averagePoints(anchorCandidates) ?? fallbackPosition;
  const anchorPosition = clampAnchorVerticalDrift(averagedAnchor, fallbackPosition);

  const measurements = ensureObject(options.measurements);
  const legs = ensureObject(measurements.legs);
  const updatedMeasurements = {
    ...measurements,
    legs: {
      ...legs,
      [fixedSide]: {
        ...ensureObject(legs[fixedSide]),
        effectorCoordinate: subtractPoints(fixedWorldPoint, anchorPosition)
      },
      [movingSide]: {
        ...ensureObject(legs[movingSide]),
        effectorCoordinate: subtractPoints(movingWorldPoint, anchorPosition)
      }
    }
  };

  const forwardedOptions = {
    ...options,
    position: anchorPosition,
    measurements: updatedMeasurements
  };
  delete forwardedOptions.fixedFeet;
  delete forwardedOptions.fixedFeetPoint;
  delete forwardedOptions.movingFeetStartPoint;
  delete forwardedOptions.movingFeetTargetPoint;
  delete forwardedOptions.progress;

  const figure = baseStaticBuilder(forwardedOptions);
  figure.sequenceState = {
    position: [...anchorPosition],
    measurements: cloneValue(updatedMeasurements)
  };
  figure.step = {
    fixedSide,
    movingSide,
    fixedPoint: fixedWorldPoint,
    movingPoint: movingWorldPoint,
    anchorPoint: anchorPosition,
    progress
  };
  return figure;
}

function computeArcPoint(start, end, progress, height) {
  const t = clamp01(progress);
  const basePoint = lerpPoint(start, end, t);
  const lift = Math.sin(Math.PI * t) * height;
  return [basePoint[0], basePoint[1] + lift];
}

function resolveArcHeight(start, end) {
  const span = distanceBetweenPoints(start, end);
  return Math.max(1.5, span * 0.25);
}

function normalizeSide(value, fallback = "left") {
  const text = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (text === "left" || text === "right") {
    return text;
  }
  return fallback;
}

function lerpPoint(start, end, t) {
  return [
    start[0] + (end[0] - start[0]) * t,
    start[1] + (end[1] - start[1]) * t
  ];
}

function ensureObject(value) {
  return value && typeof value === "object" ? value : {};
}

function cloneValue(value) {
  if (Array.isArray(value)) {
    return value.map(cloneValue);
  }
  if (value && typeof value === "object") {
    const clone = {};
    for (const key of Object.keys(value)) {
      clone[key] = cloneValue(value[key]);
    }
    return clone;
  }
  return value;
}

function clamp01(value) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    value = Number(value);
  }
  if (!Number.isFinite(value)) {
    return 0;
  }
  if (value < 0) return 0;
  if (value > 1) return 1;
  return value;
}

function toNumber(value, fallback = 0) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toPoint(value, fallback = DEFAULT_POSITION) {
  if (Array.isArray(value) && value.length >= 2) {
    return [toNumber(value[0], fallback[0]), toNumber(value[1], fallback[1])];
  }
  if (value && typeof value === "object") {
    if ("x" in value || "y" in value) {
      return [toNumber(value.x, fallback[0]), toNumber(value.y, fallback[1])];
    }
    if ("left" in value || "top" in value) {
      return [toNumber(value.left, fallback[0]), toNumber(value.top, fallback[1])];
    }
  }
  return Array.isArray(fallback) ? [...fallback] : [0, 0];
}

function isPoint(value) {
  return (
    Array.isArray(value) &&
    value.length >= 2 &&
    typeof value[0] === "number" &&
    typeof value[1] === "number" &&
    Number.isFinite(value[0]) &&
    Number.isFinite(value[1])
  );
}

function addPoints(a, b) {
  return [a[0] + b[0], a[1] + b[1]];
}

function subtractPoints(a, b) {
  return [a[0] - b[0], a[1] - b[1]];
}

function averagePoints(points) {
  if (!Array.isArray(points) || points.length === 0) {
    return null;
  }
  let sumX = 0;
  let sumY = 0;
  let count = 0;
  for (const point of points) {
    if (!isPoint(point)) {
      continue;
    }
    sumX += point[0];
    sumY += point[1];
    count += 1;
  }
  if (count === 0) {
    return null;
  }
  return [sumX / count, sumY / count];
}

function clampAnchorVerticalDrift(candidate, baseline) {
  if (!isPoint(candidate)) {
    return Array.isArray(baseline) ? [...baseline] : DEFAULT_POSITION.slice();
  }
  const reference = isPoint(baseline) ? baseline : DEFAULT_POSITION;
  const minY = reference[1] - MAX_VERTICAL_ANCHOR_DELTA;
  const maxY = reference[1] + MAX_VERTICAL_ANCHOR_DELTA;
  const clampedY = clampRange(candidate[1], minY, maxY);
  return [candidate[0], clampedY];
}

function clampRange(value, min, max) {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return clampRange(0, min, max);
  }
  if (value < min) return min;
  if (value > max) return max;
  return value;
}

function fallbackDistance(a, b) {
  if (!isPoint(a) || !isPoint(b)) {
    return 0;
  }
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  return Math.sqrt(dx * dx + dy * dy);
}

return steperMan;

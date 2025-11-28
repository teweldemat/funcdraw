const DEFAULT_POSITION = [20, 6];

function ensureObject(value) {
  return value != null && typeof value === "object" && !Array.isArray(value) ? value : {};
}

function toNumber(value, fallback) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (value == null) {
    return fallback;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function normalizePosition(value, fallback = DEFAULT_POSITION) {
  if (Array.isArray(value) && value.length >= 2) {
    const x = toNumber(value[0], fallback[0]);
    const y = toNumber(value[1], fallback[1]);
    return [x, y];
  }
  if (value && typeof value === "object") {
    if ("x" in value && "y" in value) {
      const x = toNumber(value.x, fallback[0]);
      const y = toNumber(value.y, fallback[1]);
      return [x, y];
    }
    if ("left" in value && "bottom" in value) {
      const x = toNumber(value.left, fallback[0]);
      const y = toNumber(value.bottom, fallback[1]);
      return [x, y];
    }
  }
  return fallback.slice();
}

function clampNumber(value, min, max) {
  const numeric = toNumber(value, min);
  if (!Number.isFinite(numeric)) {
    return min;
  }
  if (numeric <= min) {
    return min;
  }
  if (numeric >= max) {
    return max;
  }
  return numeric;
}

function hWalker(manModel, startXInput, targetXInput, progressInput) {
  const sourceModel = ensureObject(manModel);
  const basePosition = normalizePosition(sourceModel.position);
  const startX = toNumber(startXInput, basePosition[0]);
  const targetX = toNumber(targetXInput, startX);
  const travelDistance = Math.abs(targetX - startX);
  const direction = travelDistance === 0 ? 0 : targetX >= startX ? 1 : -1;
  const progressDistance = travelDistance > 0 ? clampNumber(progressInput, 0, travelDistance) : 0;
  const resolvedX = direction === 0 ? startX : startX + direction * progressDistance;

  return {
    ...sourceModel,
    position: [resolvedX, basePosition[1]]
  };
}

return hWalker;

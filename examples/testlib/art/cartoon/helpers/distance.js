const ZERO = [0, 0];
const normalizePointHelper = typeof normalizePoint === 'function' ? normalizePoint : null;

function distance(pointA, pointB) {
  const first = normalizePointHelper ? normalizePointHelper(pointA, ZERO) : clonePoint(pointA);
  const second = normalizePointHelper ? normalizePointHelper(pointB, ZERO) : clonePoint(pointB);
  if (!Array.isArray(first) || !Array.isArray(second)) {
    return 0;
  }
  const dx = second[0] - first[0];
  const dy = second[1] - first[1];
  return Math.sqrt(dx * dx + dy * dy);
}

function clonePoint(value) {
  if (Array.isArray(value) && value.length >= 2) {
    const x = Number(value[0]);
    const y = Number(value[1]);
    return [Number.isFinite(x) ? x : ZERO[0], Number.isFinite(y) ? y : ZERO[1]];
  }
  return ZERO.slice();
}

return distance;

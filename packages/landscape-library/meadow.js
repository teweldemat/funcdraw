const sun = require('./sun');
const hill = require('./hill');

const deepClone = (value) => {
  if (Array.isArray(value)) {
    return value.map((entry) => deepClone(entry));
  }
  if (value && typeof value === 'object') {
    const clone = {};
    for (const [key, entry] of Object.entries(value)) {
      clone[key] = deepClone(entry);
    }
    return clone;
  }
  return value;
};

const translatePrimitive = (primitive, dx, dy) => {
  const clone = deepClone(primitive);
  const movePoint = (point) => [point[0] + dx, point[1] + dy];
  if (Array.isArray(clone.center) && clone.center.length === 2) {
    clone.center = movePoint(clone.center);
  }
  if (Array.isArray(clone.from) && Array.isArray(clone.to)) {
    clone.from = movePoint(clone.from);
    clone.to = movePoint(clone.to);
  }
  if (Array.isArray(clone.position) && clone.position.length === 2) {
    clone.position = movePoint(clone.position);
  }
  if (Array.isArray(clone.points)) {
    clone.points = clone.points.map(movePoint);
  }
  return clone;
};

const cloneSun = () => sun.eval.map((primitive) => deepClone(primitive));
const cloneHill = (offsetX, offsetY) => hill.eval.map((primitive) => translatePrimitive(primitive, offsetX, offsetY));

const sky = {
  type: 'rect',
  position: [-12, -12],
  size: [24, 24],
  fill: '#0ea5e9',
  stroke: '#38bdf8',
  width: 0.1
};

const ground = {
  type: 'rect',
  position: [-12, -4],
  size: [24, 8],
  fill: '#166534'
};

const meadow = {
  eval: [sky, ...cloneSun(), ground, ...cloneHill(-2.4, 0), ...cloneHill(2.8, -0.4)]
};

module.exports = meadow;

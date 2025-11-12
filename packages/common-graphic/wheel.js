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

const buildWheelPrimitives = (center = [0, 0]) => [
  {
    type: 'circle',
    center: [...center],
    radius: 2.2,
    fill: '#94a3b8',
    stroke: '#0f172a',
    width: 0.25
  },
  {
    type: 'circle',
    center: [...center],
    radius: 1.4,
    fill: '#0f172a'
  },
  {
    type: 'circle',
    center: [...center],
    radius: 0.5,
    fill: '#e2e8f0'
  }
];

const wheel = {
  eval: buildWheelPrimitives()
};

wheel.clone = (center = [0, 0]) => buildWheelPrimitives(center).map((primitive) => deepClone(primitive));

module.exports = wheel;

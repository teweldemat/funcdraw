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

const chassis = {
  type: 'rect',
  position: [-5, -1],
  size: [10, 2],
  fill: '#0f172a'
};

const baseBody = [chassis, ...body];

const translateWheel = (center) => {
  const [offsetX, offsetY] = Array.isArray(center) && center.length === 2 ? center : [0, 0];
  return wheel.eval.map((primitive) => {
    const clone = deepClone(primitive);
    if (Array.isArray(clone.center) && clone.center.length === 2) {
      clone.center = [clone.center[0] + offsetX, clone.center[1] + offsetY];
    }
    return clone;
  });
};

const DEFAULT_WHEELS = [
  [-4, 1.6],
  [4, 1.6]
];

const wheels = DEFAULT_WHEELS.flatMap((center) => translateWheel(center));

const car = {
  body: baseBody.map((primitive) => deepClone(primitive)),
  eval: [...baseBody.map((primitive) => deepClone(primitive)), ...wheels]
};

return car;

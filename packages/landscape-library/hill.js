const foregroundHill = {
  type: 'polygon',
  points: [
    [-12, -4],
    [-7, 0],
    [-3, -1.2],
    [0, -0.4],
    [3, -1.2],
    [7, 0.5],
    [12, -4]
  ],
  fill: '#16a34a',
  stroke: '#14532d',
  width: 0.35
};

const backgroundHill = {
  type: 'polygon',
  points: [
    [-12, -2.2],
    [-9, 0.6],
    [-5, -0.6],
    [-1, 0.3],
    [4, 1.8],
    [8, -0.5],
    [12, -2]
  ],
  fill: '#22c55e',
  stroke: 'transparent'
};

const hill = {
  eval: [backgroundHill, foregroundHill]
};

module.exports = hill;

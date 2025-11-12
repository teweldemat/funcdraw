const makeRay = (angle) => {
  const radiusOuter = 3.5;
  const radiusInner = 2.2;
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  return {
    type: 'line',
    from: [cos * radiusInner, 6.2 + sin * radiusInner],
    to: [cos * radiusOuter, 6.2 + sin * radiusOuter],
    stroke: '#fde047',
    width: 0.3
  };
};

const rays = Array.from({ length: 8 }, (_value, index) => makeRay((Math.PI / 4) * index));

const core = {
  type: 'circle',
  center: [0, 6.2],
  radius: 2.2,
  fill: '#fbbf24',
  stroke: '#f97316',
  width: 0.3
};

const glow = {
  type: 'circle',
  center: [0, 6.2],
  radius: 2.6,
  stroke: '#fde047',
  width: 0.15,
  fill: 'rgba(250, 204, 21, 0.35)'
};

const sun = {
  center: [0, 6.2],
  eval: [core, glow, ...rays]
};

module.exports = sun;

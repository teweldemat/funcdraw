const cartoonLib = package('@funcdraw/testlib')?.cartoon ?? {};
const createStickman = typeof cartoonLib.stickman === 'function' ? cartoonLib.stickman : () => ({ graphics: [] });

const defaultPose = typeof createStickman.skeleton === 'function' ? createStickman.skeleton() : null;
const defaultY = Array.isArray(defaultPose?.position) ? defaultPose.position[1] : 10.5;
const groundY = 0;
const lineup = [
  { x: -24, direction: 'left' },
  { x: -8, direction: 'front' },
  { x: 8, direction: 'back' },
  { x: 24, direction: 'right' }
];

const heroes = lineup.map(({ x, direction }) =>
  createStickman({
    position: [x, groundY + 10],
    measurements: {
      torso: { direction },
      head: { direction }
    }
  })
);

const labels = lineup.map(({ x, direction }) => ({
  type: 'text',
  text: direction,
  position: [x, -3],
  fill: '#0f172a',
  fontSize: 3,
  align: 'center'
}));

const baseline = {
  type: 'line',
  from: [-28, groundY],
  to: [28, groundY],
  stroke: '#94a3b8',
  width: 0.5
};

return {
  view: { left: -32, bottom: -5, right: 32, top: 32 },
  graphics: [
    baseline,
    ...heroes.flatMap((hero) => (hero && Array.isArray(hero.graphics) ? hero.graphics : [])),
    ...labels
  ]
};

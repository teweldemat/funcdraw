const cartoonLib = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLib?.stickman;
const createStickman =
  typeof stickmanModule?.static === 'function'
    ? stickmanModule.static
    : typeof stickmanModule === 'function'
      ? stickmanModule
      : () => ({ graphics: [] });

const defaultPose = typeof createStickman.skeleton === 'function' ? createStickman.skeleton() : null;
const defaultY = Array.isArray(defaultPose?.position) ? defaultPose.position[1] : 10.5;
const groundY = 0;
const lineup = [
  { x: -48, direction: 'left' },
  { x: -16, direction: 'front' },
  { x: 16, direction: 'back' },
  { x: 48, direction: 'right' }
];

const heroes = lineup.map(({ x, direction }) =>
  createStickman({
    position: [x, groundY + 22],
    measurements: {
      torso: { direction, height: 22, width: 12 },
      head: { direction, verticalExtent: 9 },
      legs: {
        left: { effectorCoordinate: [-4, -22] },
        right: { effectorCoordinate: [4, -22] }
      },
      hands: {
        left: { effectorCoordinate: [-7.8, 4.7] },
        right: { effectorCoordinate: [7.8, 4.7] }
      }
    }
  })
);

const labels = lineup.map(({ x, direction }) => ({
  type: 'text',
  text: direction,
  position: [x, -6],
  fill: '#0f172a',
  fontSize: 12,
  align: 'center'
}));

const baseline = {
  type: 'line',
  from: [-56, groundY],
  to: [56, groundY],
  stroke: '#94a3b8',
  width: 0.5
};

return {
  view: { left: -40, bottom: -30, right: 70, top: 90 },
  graphics: [
    baseline,
    ...heroes.flatMap((hero) => (hero && Array.isArray(hero.graphics) ? hero.graphics : [])),
    ...labels
  ]
};

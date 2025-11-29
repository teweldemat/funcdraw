const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const hasSteperMan = typeof stickmanModule?.steperMan === 'function';
const steperBuilder = hasSteperMan ? stickmanModule.steperMan : null;

const view = { left: -24, bottom: -6, right: 24, top: 32 };
const groundY = 0;
const anchorBaseY = 9.4;
const timeValue = typeof t === 'number' ? t : 0;
const progress = clamp01(timeValue);
const fixedPoint = [-16, groundY];
const movingStartPoint = [-24, groundY];
const movingTargetPoint = [6, groundY];

if (!steperBuilder) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'package("@funcdraw/testlib").cartoon.stickman.steperMan unavailable',
        position: [0, 12],
        fill: '#ef4444',
        fontSize: 3,
        align: 'center'
      }
    ]
  };
}

const anchorX = lerp(fixedPoint[0], movingTargetPoint[0], progress * 0.5);
const anchorLift = Math.sin(Math.PI * progress) * 0.6;

const hero = steperBuilder({
  fixedFeet: 'left',
  fixedFeetPoint: fixedPoint,
  movingFeetStartPoint: movingStartPoint,
  movingFeetTargetPoint: movingTargetPoint,
  progress,
  position: [anchorX, anchorBaseY + anchorLift],
  measurements: {
    torso: { direction: 'right' },
    head: { direction: 'right' }
  },
  handSwing: {
    amplitude: 1.6,
    lift: 0.45,
    forwardOffset: 0.2,
    mode: 'mirror'
  }
});

const figureGraphics = Array.isArray(hero?.graphics) ? hero.graphics : [];

return {
  view,
  graphics: figureGraphics
};

function clamp01(value) {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    value = Number(value);
  }
  if (!Number.isFinite(value)) {
    return 0;
  }
  if (value < 0) return 0;
  if (value > 1) return 1;
  return value;
}

function lerp(start, end, t) {
  return start + (end - start) * t;
}

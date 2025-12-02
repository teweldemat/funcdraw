const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const hasSteperManProfile = typeof stickmanModule?.steperManProfile === 'function';
const steperBuilder = hasSteperManProfile ? stickmanModule.steperManProfile : null;

const view = { left: -24, bottom: -6, right: 24, top: 38 };
const groundY = 0;
const anchorBaseY = 9.4;
const timeValue = typeof t === 'number' ? t : 0;
const progress = clamp01(timeValue);
const fixedPoint = [-16, groundY];
const movingStartPoint = [-24, groundY];
const movingTargetPoint = [-8, groundY];

if (!steperBuilder) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'package("@funcdraw/testlib").cartoon.stickman.steperManProfile unavailable',
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
    amplitude: 6,
    lift: 0.23,
    forwardOffset: 0,
    mode: 'sine'
  }
});

const figureGraphics = Array.isArray(hero?.graphics) ? hero.graphics : [];
const anchorPosition = hero?.sequenceState?.position;
const leftHandEffectorLocal = hero?.sequenceState?.measurements?.hands?.left?.effectorCoordinate;
const rightHandEffectorLocal = hero?.sequenceState?.measurements?.hands?.right?.effectorCoordinate;
const overlayGraphics = [];
if (Array.isArray(anchorPosition) && anchorPosition.length >= 2) {
  if (Array.isArray(leftHandEffectorLocal) && leftHandEffectorLocal.length >= 2) {
    overlayGraphics.push(...createEffectorOverlay('L', anchorPosition, leftHandEffectorLocal));
  }
  if (Array.isArray(rightHandEffectorLocal) && rightHandEffectorLocal.length >= 2) {
    overlayGraphics.push(...createEffectorOverlay('R', anchorPosition, rightHandEffectorLocal));
  }
}
overlayGraphics.push(...createDebugText({
  left: leftHandEffectorLocal,
  right: rightHandEffectorLocal,
  anchor: anchorPosition
}));

return {
  view,
  graphics: [...figureGraphics, ...overlayGraphics]
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

function addPoints(a, b) {
  if (!Array.isArray(a) || !Array.isArray(b)) {
    return [0, 0];
  }
  return [a[0] + b[0], a[1] + b[1]];
}

function createEffectorOverlay(label, anchor, effectorOffset) {
  const worldPoint = addPoints(anchor, effectorOffset);
  const ahead = effectorOffset[0] >= 0;
  const labelColor = ahead ? '#22c55e' : '#ef4444';
  const status = ahead ? 'ahead' : 'behind';
  return [
    {
      type: 'circle',
      center: worldPoint,
      radius: 0.7,
      fill: labelColor,
      opacity: 0.85
    },
    {
      type: 'line',
      from: anchor,
      to: worldPoint,
      stroke: labelColor,
      width: 0.35,
      opacity: 0.7
    }
  ];
}

function createDebugText({ left, right, anchor }) {
  const lines = [];
  const basePos = [view.left + 2, view.top - 2];
  const lineHeight = 2.2;

  const formatPoint = (p) => (Array.isArray(p) ? `(${p[0].toFixed(2)}, ${p[1].toFixed(2)})` : 'n/a');
  lines.push(`anchor: ${formatPoint(anchor)}`);
  lines.push(`left (rel): ${formatPoint(left)}`);
  lines.push(`right (rel): ${formatPoint(right)}`);

  const graphics = [];
  lines.forEach((text, index) => {
    graphics.push({
      type: 'text',
      text,
      position: [basePos[0], basePos[1] - index * lineHeight],
      fontSize: 2,
      align: 'left',
      fill: '#e2e8f0'
    });
  });
  return graphics;
}

const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const staticBuilder = typeof stickmanModule?.static === 'function' ? stickmanModule.static : null;
const zoomWalkBuilder = typeof stickmanModule?.zoomWalkMan === 'function' ? stickmanModule.zoomWalkMan : null;
const stepper = typeof stickmanModule?.steperManZoom === 'function' ? stickmanModule.steperManZoom : null;
const consts = typeof constants === 'object' && constants ? constants : {};

const view = consts.zoomedInView ?? consts.view ?? { left: -400, bottom: -300, right: 400, top: 300 };
const timeValue = typeof t === 'number' ? t : 0;
const cycle = 4;
const progress = clamp01((timeValue % cycle) / cycle);

if (!stepper || !staticBuilder || !zoomWalkBuilder) {
  return {
    view,
    graphics: [
      {
        type: 'text',
        text: 'steperManZoom/static unavailable – install @funcdraw/testlib',
        position: [0, 8],
        fill: '#ef4444',
        fontSize: consts.fontSize ?? 12,
        align: 'center'
      }
    ]
  };
}

const depthDelta = -24; // zooming in (more negative Y)
const baseMeasurements = buildMeasurements();
const zoomWalk = zoomWalkBuilder({
  initialPosition: consts.shared?.anchor ?? [0, 20],
  initialMeasurements: baseMeasurements,
  depthDelta,
  zoom: 0.4,
  progress,
  stepper
});
const anchorPoint = isPoint(zoomWalk?.position) ? zoomWalk.position : (consts.shared?.anchor ?? [0, 20]);

const hero = staticBuilder({
  position: zoomWalk.position || anchorPoint,
  measurements: zoomWalk.measurements || baseMeasurements,
  palette: { overlayLeg: '#22d3ee', overlayHand: '#f97316' }
});

return {
  view,
  graphics: [
    createGroundLine(view.left, view.right),
    ...(Array.isArray(hero.graphics) ? hero.graphics : []),
    createLabel(progress, depthDelta, anchorPoint[1])
  ].filter(Boolean)
};

function buildMeasurements() {
  return {
    torso: { height: consts.shared?.torso?.height ?? 22, width: consts.shared?.torso?.width ?? 12, direction: 'front' },
    head: { verticalExtent: consts.shared?.head?.verticalExtent ?? 9, direction: 'front' },
    hands: {
      left: { effectorCoordinate: consts.shared?.hands?.left ?? [-7.8, 4.7] },
      right: { effectorCoordinate: consts.shared?.hands?.right ?? [7.8, 4.7] }
    },
    legs: {
      left: { effectorCoordinate: consts.shared?.legs?.sideWalkOffsets?.left ?? [0, -20] },
      right: { effectorCoordinate: consts.shared?.legs?.sideWalkOffsets?.right ?? [-8, -20] }
    }
  };
}

function createGroundLine(minX, maxX) {
  return { type: 'line', from: [minX, 0], to: [maxX, 0], stroke: '#94a3b8', width: 0.5 };
}

function createLabel(prog, delta, anchorY) {
  return {
    type: 'text',
    text: [
      'zoom walk',
      `progress ${Math.round(prog * 100)}%`,
      `depth delta ${delta}`,
      `anchor y ${anchorY.toFixed(1)}`
    ].join('  |  '),
    position: [0, view.top - (consts.fontSize ?? 12) * 1.5],
    fill: '#0f172a',
    fontSize: consts.fontSize ?? 12,
    align: 'center'
  };
}

function isPoint(value) {
  return (
    Array.isArray(value) &&
    value.length >= 2 &&
    typeof value[0] === 'number' &&
    typeof value[1] === 'number' &&
    Number.isFinite(value[0]) &&
    Number.isFinite(value[1])
  );
}

function clamp01(value) {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    value = Number(value);
  }
  if (!Number.isFinite(value)) return 0;
  if (value < 0) return 0;
  if (value > 1) return 1;
  return value;
}

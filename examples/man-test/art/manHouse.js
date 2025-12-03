const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
const stickmanModule = cartoonLibrary?.stickman ?? {};
const houseBuilder = typeof cartoonLibrary?.house === 'function' ? cartoonLibrary.house : null;
const staticBuilder = typeof stickmanModule?.static === 'function' ? stickmanModule.static : null;
const zoomWalkBuilder = typeof stickmanModule?.zoomWalkMan === 'function' ? stickmanModule.zoomWalkMan : null;
const zoomStepper = typeof stickmanModule?.steperManZoom === 'function' ? stickmanModule.steperManZoom : null;
const consts = typeof constants === 'object' && constants ? constants : {};

const view = consts.view ?? { left: -400, bottom: -300, right: 400, top: 300 };
const groundY = 0;
const timeValue = typeof t === 'number' ? t : 0;
const doorDuration = 1.5;
const zoomDuration = 3;
const totalDuration = doorDuration + zoomDuration;
const cycleTime = timeValue % totalDuration;
const doorProgress = clamp01(cycleTime / doorDuration);
const zoomProgress = clamp01((cycleTime - doorDuration) / zoomDuration);

const housePosition = [0, groundY];
const houseWidth = 180;

const anchorBase = [
  housePosition[0],
  (consts.manHouse?.anchor?.[1] ?? consts.shared?.anchor?.[1] ?? 24)
];
const baseMeasurements = buildMeasurements();

const viewHeight = (typeof view?.top === 'number' && typeof view?.bottom === 'number')
  ? view.top - view.bottom
  : 600;
const depthDelta = -(viewHeight * 0.25); // move downward by a quarter of the view height
const zoomTarget = 1.3; // reduce zoom intensity (was 4x)
const zoomValue = 1 + (zoomTarget - 1) * zoomProgress;
const movingSide = 'right';
const baseLegOffset = consts.manHouse?.legOffsets?.[movingSide] ?? [-10, -26];
const anchorPosition = [
  anchorBase[0],
  anchorBase[1] + depthDelta * zoomProgress
];
const movingFootTargetY = anchorBase[1] + (baseLegOffset[1] ?? -20) + depthDelta * zoomProgress;

const walk = zoomWalkBuilder
  ? zoomWalkBuilder({
      initialPosition: anchorBase,
      initialMeasurements: baseMeasurements,
      depthDelta,
      zoom: zoomTarget,
      progress: zoomProgress,
      stepper: zoomStepper
    })
  : zoomStepper
    ? zoomStepper({
        position: anchorPosition,
        measurements: baseMeasurements,
        movingSide,
        movingFootTargetY,
        zoom: zoomValue,
        progress: zoomProgress
      })
    : { position: anchorPosition, measurements: baseMeasurements };
const anchorPoint = isPoint(walk.position) ? walk.position : anchorPosition;

const hero = staticBuilder
    ? staticBuilder({
        position: anchorPoint,
        measurements: walk.measurements || baseMeasurements,
        palette: {
          overlayLeg: '#22d3ee',
          overlayHand: '#f97316',
          legWidth: 3.2,
          footStrokeWidth: 1.2
      }
    })
  : { graphics: [] };

const heroGraphics = Array.isArray(hero.graphics) ? hero.graphics : [];
const isDoorFullyOpen = doorProgress >= 1;

const house = houseBuilder
  ? houseBuilder({
      position: housePosition,
      width: houseWidth,
      doorOpenLevel: doorProgress,
      type: 'classic',
      interior: isDoorFullyOpen ? [] : heroGraphics
    })
  : { graphics: [] };

const graphics = [
  createGroundLine(view.left, view.right),
  ...(Array.isArray(house.graphics) ? house.graphics : []),
  ...(isDoorFullyOpen ? heroGraphics : []),
  createLabel(
    doorProgress,
    zoomProgress,
    zoomTarget,
    depthDelta,
    anchorPoint[1],
    Boolean(zoomWalkBuilder),
    Boolean(zoomStepper)
  )
].filter(Boolean);

return {
  view,
  graphics
};

function buildMeasurements() {
  return {
    torso: {
      height: consts.manHouse?.torso?.height ?? 16,
      width: consts.manHouse?.torso?.width ?? 10,
      direction: 'front'
    },
    head: {
      verticalExtent: consts.manHouse?.head?.verticalExtent ?? 9,
      direction: 'front'
    },
    hands: {
      left: { effectorCoordinate: consts.manHouse?.hands?.left ?? [-8, 5] },
      right: { effectorCoordinate: consts.manHouse?.hands?.right ?? [8, 5] }
    },
    legs: {
      left: {
        effectorCoordinate: consts.manHouse?.legOffsets?.left ?? [0, -26],
        upperLength: consts.manHouse?.legs?.lengths?.upper ?? 16,
        lowerLength: consts.manHouse?.legs?.lengths?.lower ?? 15
      },
      right: {
        effectorCoordinate: consts.manHouse?.legOffsets?.right ?? [-10, -26],
        upperLength: consts.manHouse?.legs?.lengths?.upper ?? 16,
        lowerLength: consts.manHouse?.legs?.lengths?.lower ?? 15
      }
    }
  };
}

function createGroundLine(minX, maxX) {
  return {
    type: 'line',
    from: [minX, groundY],
    to: [maxX, groundY],
    stroke: '#94a3b8',
    width: 0.5
  };
}

function createLabel(doorProg, walkProg, zoom, delta, anchorY, hasZoomWalk, hasStepper) {
  return {
    type: 'text',
    text: [
      'house exit',
      `door ${Math.round(doorProg * 100)}%`,
      `zoom-in ${Math.round(walkProg * 100)}%`,
      `target zoom ${zoom.toFixed(1)}`,
      `depth delta ${delta.toFixed(1)}`,
      `anchor y ${anchorY.toFixed(1)}`,
      `zoomWalk ${hasZoomWalk ? 'yes' : 'no'}`,
      `stepper ${hasStepper ? 'yes' : 'no'}`
    ].join('  |  '),
    position: [0, view.top - (consts.fontSize ?? 12) * 1.5],
    fontSize: consts.fontSize ?? 12,
    fill: '#0f172a',
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

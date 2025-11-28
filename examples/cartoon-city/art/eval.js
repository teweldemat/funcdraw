const skyModule = typeof sky === 'function' ? sky : null;
const groundModule = typeof ground === 'function' ? ground : null;
const skylineModule = typeof skyline === 'function' ? skyline : null;
const walkerModule = typeof walker === 'function' ? walker : null;

const BASE_VIEW = { width: 1000, height: 800 };
const DEBUG_WALKER_ONLY = false;
const PI_APPROX = typeof Math === 'object' && Number.isFinite(Math.PI) ? Math.PI : 3.141592653589793;
const TAU_APPROX = PI_APPROX * 2;
const FRAME_COLOR = '#1f2937';
const BASE_HERO = {
  torsoWidth: 56.25,
  torsoHeight: 170,
  shoulderExtension: 21.875,
  headExtent: 68,
  handLeft: { effector: [-31.25, -50], upper: 76, lower: 62, positiveBend: true },
  handRight: { effector: [62.5, 260], upper: 86, lower: 72, positiveBend: false },
  legLeft: { effector: [-14.375, -184], upper: 96, lower: 88, positiveBend: false },
  legRight: { effector: [14.375, -184], upper: 96, lower: 88, positiveBend: false },
  palette: {
    overlayHand: '#f472b6',
    overlayLeg: '#38bdf8',
    torsoFill: '#0f172a',
    headFill: '#fde68a',
    handWidth: 8,
    legWidth: 12
  }
};

function parseDimension(value, fallback) {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric > 0 ? numeric : fallback;
}

function resolveCanvasSize() {
  const canvasGlobal = typeof canvas !== 'undefined' ? canvas : null;
  const canvasData = canvasGlobal && typeof canvasGlobal === 'object' ? canvasGlobal : {};
  const rawSize = canvasData.size && typeof canvasData.size === 'object' ? canvasData.size : {};
  const width = parseDimension(rawSize.width, BASE_VIEW.width);
  const height = parseDimension(rawSize.height, BASE_VIEW.height);
  return { width, height };
}

function resolveCartoonLib() {
  if (typeof package !== 'function') {
    return {};
  }
  const lib = package('@funcdraw/testlib');
  return lib && lib.cartoon ? lib.cartoon : {};
}

function ensureArray(value) {
  return Array.isArray(value) ? value : [];
}

function mergeLimbOverrides(...sources) {
  const merged = {};
  for (const source of sources) {
    if (!source || typeof source !== 'object') {
      continue;
    }
    for (const [group, groupValue] of Object.entries(source)) {
      if (!groupValue || typeof groupValue !== 'object') {
        continue;
      }
      if (!merged[group]) {
        merged[group] = {};
      }
      for (const [side, sideValue] of Object.entries(groupValue)) {
        if (!sideValue || typeof sideValue !== 'object') {
          continue;
        }
        merged[group][side] = { ...(merged[group][side] || {}), ...sideValue };
      }
    }
  }
  return Object.keys(merged).length === 0 ? null : merged;
}

function pingPongState(time, durationSeconds) {
  if (!Number.isFinite(time) || !Number.isFinite(durationSeconds) || durationSeconds <= 0) {
    return { progress: 0, forward: true, phase: 0 };
  }
  const doubleDuration = durationSeconds * 2;
  const normalizedTime = ((time % doubleDuration) + doubleDuration) % doubleDuration;
  const forward = normalizedTime <= durationSeconds;
  let unit = normalizedTime / durationSeconds;
  if (!forward) {
    unit = 2 - unit;
  }
  const phase = unit * TAU_APPROX;
  return { progress: unit, forward, phase };
}

function buildScene() {
  const canvasSize = resolveCanvasSize();
  const viewWidth = canvasSize.width;
  const viewHeight = canvasSize.height;
  const scaleX = viewWidth / BASE_VIEW.width;
  const scaleY = viewHeight / BASE_VIEW.height;
  const uniformScale = (scaleX + scaleY) / 2;
  const timelineSeconds = t;

  const view = {
    left: 0,
    bottom: 0,
    right: viewWidth,
    top: viewHeight
  };

  const groundY = 80 * scaleY;

  const cartoon = resolveCartoonLib();
  const stickBuilder =
    typeof cartoon.stickman === 'function'
      ? cartoon.stickman
      : () => ({ graphics: [], overlays: [], skeleton: null });
  const hWalkerHelper = typeof cartoon.hWalker === 'function' ? cartoon.hWalker : null;
  const treeBuilder =
    typeof cartoon.tree === 'function'
      ? cartoon.tree
      : () => ({ graphics: [] });
  const skyBuilder = typeof skyModule === 'function' ? skyModule : () => ({ graphics: [] });
  const groundBuilder = typeof groundModule === 'function' ? groundModule : () => ({ graphics: [] });
  const skylineBuilder = typeof skylineModule === 'function' ? skylineModule : () => ({ graphics: [] });

  function applySwing(baseValue, swingValue, magnitude) {
    if (!Number.isFinite(swingValue) || swingValue === 0) {
      return baseValue;
    }
    const swingAmount = magnitude * swingValue;
    return baseValue + swingAmount;
  }

  function buildStickOptions({
    positionX,
    positionY,
    direction,
    scaleMultiplier = 1,
    paletteOverrides = {},
    limbOverrides = null
  }) {
    const horizontalScale = scaleX * scaleMultiplier;
    const verticalScale = scaleY * scaleMultiplier;
    const resolvedOverrides = limbOverrides && typeof limbOverrides === 'object' ? limbOverrides : null;

    function getOverride(group, side) {
      if (!resolvedOverrides || typeof resolvedOverrides[group] !== 'object') {
        return {};
      }
      const sideConfig = resolvedOverrides[group][side];
      return sideConfig && typeof sideConfig === 'object' ? sideConfig : {};
    }

    function resolveScaled(baseValue, overrideValue, scaleFactor) {
      const numeric = Number.isFinite(overrideValue) ? overrideValue : baseValue;
      return numeric * scaleFactor;
    }

    function resolveEffector(basePoint, overridePoint) {
      if (Array.isArray(overridePoint) && overridePoint.length >= 2) {
        return [overridePoint[0] * horizontalScale, overridePoint[1] * verticalScale];
      }
      return [basePoint[0] * horizontalScale, basePoint[1] * verticalScale];
    }

    function buildHandConfig(side) {
      const base = side === 'left' ? BASE_HERO.handLeft : BASE_HERO.handRight;
      const override = getOverride('hands', side);
      const effector = resolveEffector(base.effector, override.effector);
      const swingValue = Number.isFinite(override.swing) ? override.swing : 0;
      const swingMagnitude =
        Number.isFinite(override.swingMagnitude) ? override.swingMagnitude * horizontalScale : 15 * horizontalScale;
      return {
        effectorCoordinate: [applySwing(effector[0], swingValue, swingMagnitude), effector[1]],
        upperLength: resolveScaled(base.upper, override.upper, verticalScale),
        lowerLength: resolveScaled(base.lower, override.lower, verticalScale),
        positiveBend: typeof override.positiveBend === 'boolean' ? override.positiveBend : base.positiveBend
      };
    }

    function buildLegConfig(side) {
      const base = side === 'left' ? BASE_HERO.legLeft : BASE_HERO.legRight;
      const override = getOverride('legs', side);
      const effector = resolveEffector(base.effector, override.effector);
      const swingValue = Number.isFinite(override.swing) ? override.swing : 0;
      const swingAxis = typeof override.swingAxis === 'string' ? override.swingAxis.toLowerCase() : 'y';
      const swingScale = swingAxis === 'x' ? horizontalScale : verticalScale;
      const defaultSwingMagnitude = swingAxis === 'x' ? 20 * horizontalScale : 25 * verticalScale;
      const swingMagnitude =
        Number.isFinite(override.swingMagnitude) && swingScale > 0
          ? override.swingMagnitude * swingScale
          : defaultSwingMagnitude;
      const effectorX = swingAxis === 'x' ? applySwing(effector[0], swingValue, swingMagnitude) : effector[0];
      const effectorY = swingAxis === 'x' ? effector[1] : applySwing(effector[1], swingValue, swingMagnitude);
      return {
        effectorCoordinate: [effectorX, effectorY],
        upperLength: resolveScaled(base.upper, override.upper, verticalScale),
        lowerLength: resolveScaled(base.lower, override.lower, verticalScale),
        positiveBend: typeof override.positiveBend === 'boolean' ? override.positiveBend : base.positiveBend
      };
    }

    return {
      position: [positionX, positionY],
      palette: {
        ...BASE_HERO.palette,
        handWidth: BASE_HERO.palette.handWidth * scaleMultiplier,
        legWidth: BASE_HERO.palette.legWidth * scaleMultiplier,
        ...paletteOverrides
      },
      measurements: {
        torso: {
          width: BASE_HERO.torsoWidth * horizontalScale,
          height: BASE_HERO.torsoHeight * verticalScale,
          shoulderExtension: BASE_HERO.shoulderExtension * horizontalScale,
          direction
        },
        head: {
          verticalExtent: BASE_HERO.headExtent * verticalScale,
          direction
        },
        hands: {
          left: buildHandConfig('left'),
          right: buildHandConfig('right')
        },
        legs: {
          left: buildLegConfig('left'),
          right: buildLegConfig('right')
        }
      }
    };
  }

  const skylineSetback = 160 * scaleY;
  const houseRatios = [0.18, 0.5, 0.82];
  const houseWidths = [260 * scaleX, 320 * scaleX, 240 * scaleX];
  const houseCenters = houseRatios.map((ratio) => view.left + viewWidth * ratio);
  const treeBaseY = groundY + skylineSetback - 10 * scaleY;
  const treeBehindOffset = 20 * scaleY;
  const treeEdgeOffset = 50 * scaleX;
  const treeGapOffset = 30 * scaleX;
  const treeHeight1 = 420 * scaleY;
  const treeHeight2 = 540 * scaleY;
  const treeHeight3 = 380 * scaleY;
  const treeHeightEdge = 360 * scaleY;

  const treesCustom = [
    { type: 'round', position: [houseCenters[0], treeBaseY + treeBehindOffset], height: treeHeight1 },
    { type: 'column', position: [houseCenters[0] - treeEdgeOffset, treeBaseY - 5 * scaleY], height: treeHeightEdge },
    { type: 'pine', position: [(houseCenters[0] + houseCenters[1]) / 2, treeBaseY], height: treeHeight2 },
    { type: 'round', position: [houseCenters[1] + treeGapOffset, treeBaseY + treeBehindOffset], height: treeHeight3 },
    { type: 'column', position: [(houseCenters[1] + houseCenters[2]) / 2, treeBaseY + 8 * scaleY], height: treeHeight1 },
    { type: 'pine', position: [houseCenters[2] + treeEdgeOffset, treeBaseY], height: treeHeight2 },
    { type: 'round', position: [houseCenters[2], treeBaseY + treeBehindOffset], height: treeHeight3 }
  ];

  const frameWidth = Math.max(10 * uniformScale, 2);
  const frameGraphics = [
    { type: 'line', from: [view.left, view.bottom], to: [view.right, view.bottom], stroke: FRAME_COLOR, width: frameWidth },
    { type: 'line', from: [view.left, view.top], to: [view.right, view.top], stroke: FRAME_COLOR, width: frameWidth },
    { type: 'line', from: [view.left, view.bottom], to: [view.left, view.top], stroke: FRAME_COLOR, width: frameWidth },
    { type: 'line', from: [view.right, view.bottom], to: [view.right, view.top], stroke: FRAME_COLOR, width: frameWidth }
  ];

  const skyModel = skyBuilder({
    view,
    helpers: {
      sun: typeof cartoon.sun === 'function' ? cartoon.sun : undefined,
      cloud: typeof cartoon.cloud === 'function' ? cartoon.cloud : undefined
    }
  });

  const groundModel = groundBuilder({
    view,
    groundLevel: groundY,
    fieldDepth: 280 * scaleY,
    roadWidth: 80 * scaleY,
    roadOffset: 180 * scaleY,
    roadStripeLength: 70 * scaleX,
    roadStripeGap: 55 * scaleX,
    roadStripeMargin: 12 * scaleX
  });

  const skylineModel = skylineBuilder({
    view,
    groundLevel: groundY,
    setback: skylineSetback,
    houseWidths,
    houseRatios,
    houseYOffset: [0, 0, 0],
    trees: [],
    helpers: {
      house: typeof cartoon.house === 'function' ? cartoon.house : undefined
    }
  });

  const treeModels = treesCustom.map((treeConfig) => treeBuilder(treeConfig));
  const treeGraphics = treeModels.flatMap((model) => ensureArray(model.graphics));

  const miniScale = 0.35;
  const miniBaseY = groundY + 184 * scaleY * miniScale;
  const miniConfigs = [{ direction: 'front', color: '#0891b2', animated: true }];
  const miniSpacing = viewWidth * 0.15;
  const miniStart = viewWidth * 0.2;
  const miniTrackMargin = Math.max(60 * scaleX, viewWidth * 0.08);
  const miniTrackLeft = view.left + miniTrackMargin;
  const miniTrackRight = view.right - miniTrackMargin;
  const miniTrackSpan = Math.max(miniTrackRight - miniTrackLeft, viewWidth * 0.1);
  const miniAnimationDuration = 8;
  const miniMotionState = pingPongState(timelineSeconds, miniAnimationDuration);
  const animatedMiniX = miniTrackLeft + miniTrackSpan * miniMotionState.progress;
  const animatedMiniDirection = miniMotionState.forward ? 'right' : 'left';
  const walkerStartX = miniMotionState.forward ? miniTrackLeft : miniTrackRight;
  const walkerTargetX = miniMotionState.forward ? miniTrackRight : miniTrackLeft;
  const walkerProgressDistance = Math.abs(animatedMiniX - walkerStartX);
  const swingFrequencyMultiplier = 4;
  const swingCyclesPerSecond = swingFrequencyMultiplier / miniAnimationDuration;
  const walkingPhase = timelineSeconds * swingCyclesPerSecond * TAU_APPROX;
  const miniHorizontalScale = scaleX * miniScale;
  const miniVerticalScale = scaleY * miniScale;
  const safeHorizontal = miniHorizontalScale || 1;
  const safeVertical = miniVerticalScale || 1;
  const swingAmplitudeHand = 0.45;
  const swingAmplitudeLeg = 0.3;
  const legPhase = Math.sin(walkingPhase);
  const counterPhase = Math.sin(walkingPhase + PI_APPROX);
  const leftLegSwing = legPhase * swingAmplitudeLeg;
  const rightLegSwing = counterPhase * swingAmplitudeLeg;
  const leftHandSwing = counterPhase * swingAmplitudeHand;
  const rightHandSwing = legPhase * swingAmplitudeHand;
  const walkerHandBaseX = 34;
  const walkerHandBaseY = -150;
  const walkerHandSwingDistance = 150;
  const walkerHandsPoseOverrides = {
    hands: {
      left: {
        effector: [-walkerHandBaseX, walkerHandBaseY],
        upper: 120,
        lower: 110,
        positiveBend: false,
        swingMagnitude: walkerHandSwingDistance
      },
      right: {
        effector: [walkerHandBaseX, walkerHandBaseY],
        upper: 120,
        lower: 110,
        positiveBend: false,
        swingMagnitude: walkerHandSwingDistance
      }
    }
  };
  const animatedHandOverrides = {
    hands: {
      left: { swing: leftHandSwing },
      right: { swing: rightHandSwing }
    }
  };
  const animatedLegOverrides = {
    legs: {
      left: { swing: leftLegSwing, swingAxis: 'x', swingMagnitude: 60 },
      right: { swing: rightLegSwing, swingAxis: 'x', swingMagnitude: 60 }
    }
  };
  let walkerLegOverrides = null;
  let walkerHandOverrides = null;

  if (walkerModule) {
    const miniTorsoHeight = BASE_HERO.torsoHeight * miniVerticalScale;
    const baseHandYUnits = BASE_HERO.handLeft.effector[1];
    const walkerDimensions = {
      groundY,
      hipY: miniBaseY,
      handY: miniBaseY + baseHandYUnits * miniVerticalScale,
      legs: {
        upper: BASE_HERO.legLeft.upper * miniVerticalScale,
        lower: BASE_HERO.legLeft.lower * miniVerticalScale,
        strideRatio: 0.65,
        lift: (BASE_HERO.legLeft.upper + BASE_HERO.legLeft.lower) * miniVerticalScale * 0.08
      },
      arms: {
        upper: 120 * miniVerticalScale,
        lower: 110 * miniVerticalScale,
        restOffset: 34 * miniHorizontalScale,
        swing: 22 * miniHorizontalScale
      }
    };
    const walkerState = walkerModule({
      startX: walkerStartX,
      currentX: animatedMiniX,
      dimensions: walkerDimensions
    });
    if (walkerState && walkerState.legs) {
      walkerLegOverrides = {
        legs: {
          left: {
            effector: [
              (walkerState.legs.left[0] - animatedMiniX) / safeHorizontal,
              (walkerState.legs.left[1] - miniBaseY) / safeVertical
            ],
            positiveBend: BASE_HERO.legLeft.positiveBend
          },
          right: {
            effector: [
              (walkerState.legs.right[0] - animatedMiniX) / safeHorizontal,
              (walkerState.legs.right[1] - miniBaseY) / safeVertical
            ],
            positiveBend: BASE_HERO.legRight.positiveBend
          }
        }
      };
    }
    if (
      walkerState &&
      walkerState.hands &&
      Array.isArray(walkerState.hands.left) &&
      Array.isArray(walkerState.hands.right)
    ) {
      const convertEffector = (worldPoint) => [
        (worldPoint[0] - animatedMiniX) / safeHorizontal,
        (worldPoint[1] - miniBaseY) / safeVertical
      ];
      const normalizeLength = (value) => {
        const numeric = Number(value);
        return Number.isFinite(numeric) ? numeric / safeVertical : undefined;
      };
      const armsLengths = walkerState.armsLengths || {};
      const overrideUpper = normalizeLength(armsLengths.upper);
      const overrideLower = normalizeLength(armsLengths.lower);
      walkerHandOverrides = {
        hands: {
          left: {
            effector: convertEffector(walkerState.hands.left)
          },
          right: {
            effector: convertEffector(walkerState.hands.right)
          }
        }
      };
      if (overrideUpper != null) {
        walkerHandOverrides.hands.left.upper = overrideUpper;
        walkerHandOverrides.hands.right.upper = overrideUpper;
      }
      if (overrideLower != null) {
        walkerHandOverrides.hands.left.lower = overrideLower;
        walkerHandOverrides.hands.right.lower = overrideLower;
      }
    }
  }
  const animatedOverrideSources = [walkerHandsPoseOverrides];
  if (walkerHandOverrides) {
    animatedOverrideSources.push(walkerHandOverrides);
  } else {
    animatedOverrideSources.push(animatedHandOverrides);
  }
  if (walkerLegOverrides) {
    animatedOverrideSources.push(walkerLegOverrides);
  } else {
    animatedOverrideSources.push(animatedLegOverrides);
  }
  const miniPositionsX = miniConfigs.map((config, index) =>
    config.animated ? animatedMiniX : miniStart + miniSpacing * index
  );
  const miniDirections = miniConfigs.map((config) => (config.animated ? animatedMiniDirection : config.direction));
  const miniHeroes = miniConfigs.map((config, index) => {
    const posX = miniPositionsX[index];
    const limbOverrides = config.animated
      ? mergeLimbOverrides(...animatedOverrideSources)
      : null;
    const baseOptions = buildStickOptions({
      positionX: posX,
      positionY: miniBaseY,
      direction: miniDirections[index],
      limbOverrides,
      scaleMultiplier: miniScale,
      paletteOverrides: {
        torsoFill: config.color,
        headFill: '#fde68a'
      }
    });
    const walkerAdjustedOptions =
      config.animated && hWalkerHelper
        ? hWalkerHelper(baseOptions, walkerStartX, walkerTargetX, walkerProgressDistance)
        : baseOptions;
    return stickBuilder(walkerAdjustedOptions);
  });
  const hero = miniHeroes[0] || null;
  const skyGraphics = ensureArray(skyModel.graphics);
  const groundGraphics = ensureArray(groundModel.graphics);
  const skylineGraphics = ensureArray(skylineModel.graphics);
  const heroGraphics = miniHeroes.flatMap((figure) => ensureArray(figure.graphics));

  const baseLayer = [...skyGraphics, ...groundGraphics];
  const treeLayer = [...baseLayer, ...treeGraphics];
  const skylineLayer = [...treeLayer, ...skylineGraphics];
  const topLayer = [...skylineLayer, ...heroGraphics];
  const graphics = [...topLayer, ...frameGraphics];

  let finalGraphics = graphics;
  let finalLayers = {
    sky: skyGraphics,
    ground: groundGraphics,
    trees: treeGraphics,
    skyline: skylineGraphics,
    heroes: heroGraphics,
    frame: frameGraphics
  };

  if (DEBUG_WALKER_ONLY) {
    const walkerIndex = miniConfigs.findIndex((config) => config.animated);
    const walker = walkerIndex >= 0 ? miniHeroes[walkerIndex] : null;
    const walkerGraphics = walker ? ensureArray(walker.graphics) : [];
    finalGraphics = walkerGraphics;
    finalLayers = {
      walker: walkerGraphics
    };
  }

  return {
    view,
    graphics: finalGraphics,
    hero,
    miniHeroes,
    layers: finalLayers
  };
}

const sceneResult = buildScene();

if (typeof module !== 'undefined' && module.exports) {
  module.exports = buildScene;
}

return sceneResult;

function scene2(sceneTime = 0, previousScene = null) {
  const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
  const stickmanModule = cartoonLibrary?.stickman ?? {};
  const staticBuilder = typeof stickmanModule?.static === 'function' ? stickmanModule.static : null;
  const walkBuilder = typeof stickmanModule?.sideWalkMan === 'function' ? stickmanModule.sideWalkMan : null;
  const houseBuilder = typeof cartoonLibrary?.house === 'function' ? cartoonLibrary.house : null;
  const consts = typeof constants === 'object' && constants ? constants : {};
  const legBend = resolveLegBend(consts);

  const view = consts.view ?? { left: -400, bottom: -300, right: 400, top: 300 };
  const groundY = 0;
  const duration = 3;
  const timeValue = typeof sceneTime === 'number' ? sceneTime : 0;
  const clampedTime = clampRange(timeValue, 0, duration);
  const walkProgress = clamp01(clampedTime / duration);

  if (!staticBuilder || !walkBuilder) {
    return {
      view,
      graphics: [
        {
          type: 'text',
          text: 'cartoon stickman walkers unavailable (static/sideWalkMan missing)',
          position: [0, 12],
          fill: '#ef4444',
          fontSize: 12,
          align: 'center'
        }
      ]
    };
  }

  const baseHousePosition = Array.isArray(consts.scene2?.housePosition)
    ? consts.scene2.housePosition
    : [0, groundY];
  const desiredHouseWidth = Number.isFinite(consts.scene2?.houseWidth) ? consts.scene2.houseWidth : 180;
  const {
    primary: housePosition,
    secondary: otherHousePosition,
    width: houseWidth
  } = resolveHouseLayout(view, baseHousePosition, desiredHouseWidth, groundY);

  const startPose = resolveStartPose(previousScene, consts, groundY, view, housePosition);
  const startX = startPose.position[0];
  const targetX = resolveTargetX(view, consts, startX, otherHousePosition?.[0]);
  const displacement = Number.isFinite(consts.scene2?.displacement)
    ? consts.scene2.displacement
    : targetX - startX;

  const walk = walkBuilder({
    initialPosition: startPose.position,
    initialMeasurements: startPose.measurements,
    displacement,
    progress: walkProgress,
    direction: displacement >= 0 ? 'right' : 'left',
    handSwing: consts.scene2?.handSwing ?? true,
    strideLength: consts.scene2?.strideLength
  });

  const posePosition = isPoint(walk.position) ? walk.position : startPose.position;
  const poseMeasurements = applyLegBend(walk.measurements || startPose.measurements, legBend);

  const hero = staticBuilder({
    position: posePosition,
    measurements: poseMeasurements,
    palette: {
      overlayLeg: '#22d3ee',
      overlayHand: '#f97316',
      legWidth: 3.2,
      footStrokeWidth: 1.2
    }
  }) || { graphics: [] };

  const groundLine = typeof road === 'function'
    ? road({ left: view.left, right: view.right, y: groundY, stroke: '#94a3b8', width: 0.5 })
    : createGroundLine(view.left, view.right, groundY);

  const doorOpenLevel = clamp01(
    Number.isFinite(consts.scene2?.doorOpenLevel) ? consts.scene2.doorOpenLevel : 1
  );
  const house = houseBuilder
    ? houseBuilder({
        position: housePosition,
        width: houseWidth,
        doorOpenLevel,
        type: 'classic',
        interior: []
      })
    : { graphics: [] };
  const otherHouse = houseBuilder
    ? houseBuilder({
        position: otherHousePosition,
        width: houseWidth,
        doorOpenLevel: 0,
        type: 'classic',
        interior: []
      })
    : { graphics: [] };

  const graphics = [
    groundLine,
    ...(Array.isArray(house.graphics) ? house.graphics : []),
    ...(Array.isArray(otherHouse.graphics) ? otherHouse.graphics : []),
    ...(Array.isArray(hero.graphics) ? hero.graphics : []),
    createLabel(walkProgress, displacement, posePosition, view, consts)
  ].filter(Boolean);

  return {
    view,
    graphics,
    manPosition: posePosition,
    manMeasurements: poseMeasurements
  };
}

function resolveStartPose(previousScene, consts, groundY, view, housePosition) {
  const scene1Snapshot = sampleScene1(consts);
  const provided = pickPose(previousScene) || pickPose(consts.scene2?.startPose) || pickPose(scene1Snapshot);
  const legBend = resolveLegBend(consts);
  const measurements = applyLegBend(
    enforceFacing(provided?.measurements || buildMeasurements(consts), consts.scene2?.direction || 'right'),
    legBend
  );
  const providedPosition = isPoint(provided?.position) ? provided.position : null;
  const fallbackAnchorY = resolveGroundAnchor(measurements, groundY);
  const fallbackX = consts.scene2?.startX ?? (isPoint(housePosition) ? housePosition[0] : view.left + 80);
  const startX = clampRange(
    Number.isFinite(providedPosition?.[0]) ? providedPosition[0] : fallbackX,
    view.left + 40,
    view.right - 40
  );
  const startY = Number.isFinite(providedPosition?.[1]) ? providedPosition[1] : fallbackAnchorY;

  return {
    position: [startX, startY],
    measurements
  };
}

function sampleScene1(consts) {
  const totalDuration = consts.scene1Duration ?? 4.5;
  const sampleTime = Math.max(0, totalDuration - 1e-3);
  if (typeof scene1 === 'function') {
    return scene1(sampleTime);
  }
  return null;
}

function resolveTargetX(view, consts, startX, destinationX) {
  if (Number.isFinite(consts.scene2?.targetX)) {
    return consts.scene2.targetX;
  }
  const margin = consts.scene2?.margin ?? 60;
  if (Number.isFinite(destinationX)) {
    return clampRange(destinationX, view.left + margin, view.right - margin);
  }
  const mid = (view.left + view.right) / 2;
  const defaultTarget = startX <= mid ? view.right - margin : view.left + margin;
  return clampRange(defaultTarget, view.left + margin, view.right - margin);
}

function resolveHouseLayout(view, preferredPosition, preferredWidth, groundY, gapMultiplier = 2) {
  const viewLeft = Number.isFinite(view?.left) ? view.left : -400;
  const viewRight = Number.isFinite(view?.right) ? view.right : 400;
  const width = Number.isFinite(preferredWidth) ? preferredWidth : 180;
  const gap = width * gapMultiplier;
  const span = width * 2 + gap;
  const halfSpan = span / 2;
  const preferredCenter = Number.isFinite(preferredPosition?.[0])
    ? preferredPosition[0]
    : (viewLeft + viewRight) / 2;
  const y = Number.isFinite(preferredPosition?.[1]) ? preferredPosition[1] : groundY;
  const clampedCenter = clampRange(preferredCenter, viewLeft + halfSpan, viewRight - halfSpan);
  const offset = gap / 2 + width / 2;
  return {
    primary: [clampedCenter - offset, y],
    secondary: [clampedCenter + offset, y],
    width,
    gap
  };
}

function pickPose(value) {
  if (!value || typeof value !== 'object') return null;
  const position = toPoint(value.manPosition || value.position);
  const measurements = isObject(value.manMeasurements || value.measurements)
    ? (value.manMeasurements || value.measurements)
    : null;
  if (!position && !measurements) return null;
  return { position, measurements };
}

function resolveGroundAnchor(measurements, groundY) {
  const left = toPoint(measurements?.legs?.left?.effectorCoordinate, [0, -20]);
  const right = toPoint(measurements?.legs?.right?.effectorCoordinate, [0, -20]);
  const avgOffsetY = averageNumbers([left[1], right[1]]);
  return groundY - avgOffsetY;
}

function enforceFacing(measurements, direction) {
  const base = isObject(measurements) ? measurements : {};
  const torso = isObject(base.torso) ? base.torso : {};
  const head = isObject(base.head) ? base.head : {};
  return {
    ...base,
    torso: { ...torso, direction },
    head: { ...head, direction }
  };
}

function buildMeasurements(consts) {
  const legBend = resolveLegBend(consts);
  return {
    torso: {
      height: consts.manHouse?.torso?.height ?? 16,
      width: consts.manHouse?.torso?.width ?? 10,
      direction: 'right'
    },
    head: {
      verticalExtent: consts.manHouse?.head?.verticalExtent ?? 9,
      direction: 'right'
    },
    hands: {
      left: { effectorCoordinate: consts.manHouse?.hands?.left ?? [-8, 5] },
      right: { effectorCoordinate: consts.manHouse?.hands?.right ?? [8, 5] }
    },
    legs: {
      left: {
        effectorCoordinate: consts.manHouse?.legOffsets?.left ?? [0, -26],
        upperLength: consts.manHouse?.legs?.lengths?.upper ?? 16,
        lowerLength: consts.manHouse?.legs?.lengths?.lower ?? 15,
        positiveBend: legBend.left
      },
      right: {
        effectorCoordinate: consts.manHouse?.legOffsets?.right ?? [-10, -26],
        upperLength: consts.manHouse?.legs?.lengths?.upper ?? 16,
        lowerLength: consts.manHouse?.legs?.lengths?.lower ?? 15,
        positiveBend: legBend.right
      }
    }
  };
}

function createGroundLine(minX, maxX, groundY, stroke = '#94a3b8', width = 0.5) {
  return {
    type: 'line',
    from: [minX, groundY],
    to: [maxX, groundY],
    stroke,
    width
  };
}

function createLabel(progress, displacement, position, view, consts) {
  return {
    type: 'text',
    text: [
      'scene2: walk across',
      `walk ${Math.round(progress * 100)}%`,
      `delta ${Number(displacement || 0).toFixed(1)}px`,
      `x ${position?.[0]?.toFixed ? position[0].toFixed(1) : 'n/a'}`,
      `y ${position?.[1]?.toFixed ? position[1].toFixed(1) : 'n/a'}`
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

function resolveLegBend(consts) {
  const override = consts?.scene2?.positiveBend ?? consts?.manHouse?.legs?.positiveBend;
  if (typeof override === 'boolean') {
    return { left: override, right: override };
  }
  if (isObject(override)) {
    const left = resolveBoolean(override.left, undefined);
    const right = resolveBoolean(override.right, undefined);
    if (left !== undefined || right !== undefined) {
      return {
        left: left !== undefined ? left : Boolean(right),
        right: right !== undefined ? right : Boolean(left)
      };
    }
  }
  return { left: false, right: false };
}

function applyLegBend(measurements, legBend) {
  const base = isObject(measurements) ? measurements : {};
  const legs = isObject(base.legs) ? base.legs : {};
  const left = isObject(legs.left) ? legs.left : {};
  const right = isObject(legs.right) ? legs.right : {};
  return {
    ...base,
    legs: {
      left: { ...left, positiveBend: !resolveBoolean(legBend?.left, resolveBoolean(left.positiveBend, false)) },
      right: { ...right, positiveBend: !resolveBoolean(legBend?.right, resolveBoolean(right.positiveBend, false)) }
    }
  };
}

function resolveBoolean(value, fallback = false) {
  if (typeof value === 'boolean') return value;
  return fallback;
}

function toPoint(value, fallback) {
  if (Array.isArray(value) && value.length >= 2) {
    const x = Number(value[0]);
    const y = Number(value[1]);
    if (Number.isFinite(x) && Number.isFinite(y)) return [x, y];
  }
  if (Array.isArray(fallback) && fallback.length >= 2) {
    return [Number(fallback[0]) || 0, Number(fallback[1]) || 0];
  }
  return null;
}

function isObject(value) {
  return Boolean(value && typeof value === 'object');
}

function averageNumbers(values) {
  const nums = Array.isArray(values) ? values.filter((v) => Number.isFinite(v)) : [];
  if (!nums.length) return 0;
  return nums.reduce((sum, val) => sum + val, 0) / nums.length;
}

function clampRange(value, min, max) {
  const num = Number.isFinite(value) ? value : Number(value);
  if (!Number.isFinite(num)) return min;
  return Math.min(Math.max(num, min), max);
}

function clamp01(value) {
  const num = Number.isFinite(value) ? value : Number(value);
  if (!Number.isFinite(num)) return 0;
  if (num < 0) return 0;
  if (num > 1) return 1;
  return num;
}

return scene2;

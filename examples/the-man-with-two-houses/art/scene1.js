function scene1(sceneTime = 0) {
  const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
  const stickmanModule = cartoonLibrary?.stickman ?? {};
  const houseBuilder = typeof cartoonLibrary?.house === 'function' ? cartoonLibrary.house : null;
  const staticBuilder = typeof stickmanModule?.static === 'function' ? stickmanModule.static : null;
  const zoomWalkBuilder = typeof stickmanModule?.zoomWalkMan === 'function' ? stickmanModule.zoomWalkMan : null;
  const zoomStepper = typeof stickmanModule?.steperManZoom === 'function' ? stickmanModule.steperManZoom : null;
  const consts = typeof constants === 'object' && constants ? constants : {};

  const view = consts.view ?? { left: -400, bottom: -300, right: 400, top: 300 };
  const groundY = 0;
  const timeValue = typeof sceneTime === 'number' ? sceneTime : 0;
  const doorDuration = 1.5;
  const zoomDuration = 3;
  const totalDuration = doorDuration + zoomDuration;
  const cycleTime = timeValue % totalDuration;
  const doorProgress = clamp01(cycleTime / doorDuration);
  const zoomProgress = clamp01((cycleTime - doorDuration) / zoomDuration);

  const baseHousePosition = Array.isArray(consts.scene1?.housePosition)
    ? consts.scene1.housePosition
    : [0, groundY];
  const desiredHouseWidth = Number.isFinite(consts.scene1?.houseWidth) ? consts.scene1.houseWidth : 180;
  const {
    primary: housePosition,
    secondary: otherHousePosition,
    width: houseWidth
  } = resolveHouseLayout(view, baseHousePosition, desiredHouseWidth, groundY);

  const anchorBase = [
    housePosition[0],
    (consts.manHouse?.anchor?.[1] ?? consts.shared?.anchor?.[1] ?? 26)
  ];
  const baseMeasurements = buildMeasurements(consts);

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
  const insideOffset = resolveInsideOffset(consts);

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
  const insideAnchorPoint = offsetPoint(anchorPoint, insideOffset);
  const heroMeasurements = sanitizeMeasurements(walk.measurements, baseMeasurements);

  const heroOutside = staticBuilder
    ? staticBuilder({
        position: anchorPoint,
        measurements: heroMeasurements,
        palette: {
          overlayLeg: '#22d3ee',
          overlayHand: '#f97316',
          legWidth: 3.2,
          footStrokeWidth: 1.2
        }
      })
    : { graphics: [] };
  const heroInside = staticBuilder
    ? staticBuilder({
        position: insideAnchorPoint,
        measurements: heroMeasurements,
        palette: {
          overlayLeg: '#22d3ee',
          overlayHand: '#f97316',
          legWidth: 3.2,
          footStrokeWidth: 1.2
        }
      })
    : { graphics: [] };

  const heroGraphicsOutside = Array.isArray(heroOutside.graphics) ? heroOutside.graphics : [];
  const heroGraphicsInside = Array.isArray(heroInside.graphics) ? heroInside.graphics : [];
  const exteriorReveal = clamp01(consts.scene1?.outsideReveal ?? 0.9);
  const showOutside = doorProgress >= exteriorReveal;
  const posePosition = showOutside ? anchorPoint : insideAnchorPoint;

  const house = houseBuilder
    ? houseBuilder({
        position: housePosition,
        width: houseWidth,
        doorOpenLevel: doorProgress,
        type: 'classic',
        interior: showOutside ? [] : heroGraphicsInside
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
    road({ left: view.left, right: view.right, y: groundY, stroke: '#94a3b8', width: 0.5 }),
    ...(Array.isArray(house.graphics) ? house.graphics : []),
    ...(Array.isArray(otherHouse.graphics) ? otherHouse.graphics : []),
    ...(showOutside ? heroGraphicsOutside : []),
    createLabel(
      doorProgress,
      zoomProgress,
      zoomTarget,
      depthDelta,
      anchorPoint[1],
      Boolean(zoomWalkBuilder),
      Boolean(zoomStepper),
      view,
      consts
    )
  ].filter(Boolean);

  return {
    view,
    graphics,
    manPosition: posePosition,
    manMeasurements: heroMeasurements
  };
}

function buildMeasurements(consts) {
  const legBend = resolveLegBend(consts);
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



function createLabel(doorProg, walkProg, zoom, delta, anchorY, hasZoomWalk, hasStepper, view, consts) {
  return {
    type: 'text',
    text: [
      'scene1: house exit',
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

function resolveLegBend(consts) {
  const override = consts?.manHouse?.legs?.positiveBend;
  if (typeof override === 'boolean') {
    return { left: override, right: override };
  }
  if (isObject(override)) {
    const left = typeof override.left === 'boolean' ? override.left : undefined;
    const right = typeof override.right === 'boolean' ? override.right : undefined;
    if (typeof left === 'boolean' || typeof right === 'boolean') {
      return {
        left: typeof left === 'boolean' ? left : Boolean(right),
        right: typeof right === 'boolean' ? right : Boolean(left)
      };
    }
  }
  return { left: false, right: false };
}

function resolveInsideOffset(consts) {
  const inside = consts.manHouse?.insideOffset || consts.shared?.insideOffset || [0, 6];
  if (Array.isArray(inside) && inside.length >= 2) {
    const x = Number(inside[0]);
    const y = Number(inside[1]);
    if (Number.isFinite(x) && Number.isFinite(y)) {
      return [x, y];
    }
  }
  return [0, 6];
}

function offsetPoint(point, offset) {
  if (!Array.isArray(point) || point.length < 2) return point;
  if (!Array.isArray(offset) || offset.length < 2) return point;
  return [point[0] + (Number(offset[0]) || 0), point[1] + (Number(offset[1]) || 0)];
}

function sanitizeMeasurements(candidate, base) {
  const validObj = (value) => Boolean(value && typeof value === 'object' && value.__fsKind !== 'FsError');
  const toNumber = (value) => {
    const num = Number(value);
    return Number.isFinite(num) ? num : null;
  };
  const toPoint = (value) => {
    if (!Array.isArray(value) || value.length < 2) return null;
    const x = toNumber(value[0]);
    const y = toNumber(value[1]);
    return x !== null && y !== null ? [x, y] : null;
  };
  const validEffector = (value, refUpper, refLower) => {
    const point = toPoint(value);
    if (!point) return null;
    const magnitude = Math.abs(point[0]) + Math.abs(point[1]);
    if (magnitude < 1e-3) return null;
    const reachLimit = (refUpper ?? 0) + (refLower ?? 0);
    if (reachLimit > 0 && Math.hypot(point[0], point[1]) > reachLimit * maxScale) {
      return null;
    }
    return point;
  };

  const baseSafe = validObj(base) ? base : {};
  const input = validObj(candidate) ? candidate : {};
  const torsoBase = validObj(baseSafe.torso) ? baseSafe.torso : {};
  const headBase = validObj(baseSafe.head) ? baseSafe.head : {};
  const legsBase = validObj(baseSafe.legs) ? baseSafe.legs : {};
  const handsBase = validObj(baseSafe.hands) ? baseSafe.hands : {};
  const maxScale = 3;

  const baseTorsoHeight = toNumber(torsoBase.height) ?? 16;
  const baseTorsoWidth = toNumber(torsoBase.width) ?? 10;
  const baseHeadExtent = toNumber(headBase.verticalExtent) ?? 9;
  const defaultShoulder =
    toNumber(torsoBase.shoulderExtension) ??
    (baseTorsoWidth ? baseTorsoWidth * 0.15 : 0);

  const sanitizeTorso = (torsoRaw) => {
    const torso = validObj(torsoRaw) ? torsoRaw : {};
    const h = toNumber(torso.height);
    const w = toNumber(torso.width);
    const shoulder = toNumber(torso.shoulderExtension);
    return {
      ...torsoBase,
      ...torso,
      height: h !== null && h > 0 && h <= baseTorsoHeight * maxScale ? h : baseTorsoHeight,
      width: w !== null && w > 0 && w <= baseTorsoWidth * maxScale ? w : baseTorsoWidth,
      shoulderExtension:
        shoulder !== null && shoulder >= 0 && shoulder <= baseTorsoWidth * maxScale
          ? shoulder
          : defaultShoulder,
      direction: torso.direction ?? torsoBase.direction ?? 'front'
    };
  };

  const sanitizeHead = (headRaw) => {
    const head = validObj(headRaw) ? headRaw : {};
    const v = toNumber(head.verticalExtent);
    return {
      ...headBase,
      ...head,
      verticalExtent: v !== null && v > 0 && v <= baseHeadExtent * maxScale ? v : baseHeadExtent,
      direction: head.direction ?? headBase.direction ?? torsoBase.direction ?? 'front'
    };
  };

  const sanitizeLimb = (limbRaw, baseLimb, defaults) => {
    const limb = validObj(limbRaw) ? limbRaw : {};
    const baseSafeLimb = validObj(baseLimb) ? baseLimb : {};
    const refUpper = toNumber(baseSafeLimb.upperLength) ?? toNumber(defaults.upperLength);
    const refLower = toNumber(baseSafeLimb.lowerLength) ?? toNumber(defaults.lowerLength);
    const upperVal = toNumber(limb.upperLength);
    const lowerVal = toNumber(limb.lowerLength);
    const upperLength =
      upperVal !== null && upperVal > 0 && (!refUpper || upperVal <= refUpper * maxScale)
        ? upperVal
        : refUpper ?? defaults.upperLength;
    const lowerLength =
      lowerVal !== null && lowerVal > 0 && (!refLower || lowerVal <= refLower * maxScale)
        ? lowerVal
        : refLower ?? defaults.lowerLength;

    const effector =
      validEffector(limb.effectorCoordinate, refUpper, refLower) ??
      validEffector(baseSafeLimb.effectorCoordinate, refUpper, refLower) ??
      validEffector(defaults.effectorCoordinate, refUpper, refLower);

    const foot = validObj(limb.foot)
      ? limb.foot
      : validObj(baseSafeLimb.foot)
        ? baseSafeLimb.foot
        : undefined;

    const positiveBend =
      typeof limb.positiveBend === 'boolean'
        ? limb.positiveBend
        : typeof baseSafeLimb.positiveBend === 'boolean'
          ? baseSafeLimb.positiveBend
          : typeof defaults.positiveBend === 'boolean'
            ? defaults.positiveBend
            : undefined;

    return {
      ...defaults,
      ...baseSafeLimb,
      ...limb,
      upperLength,
      lowerLength,
      effectorCoordinate: effector,
      foot,
      positiveBend
    };
  };

  const defaultLegUpper = toNumber(legsBase.left?.upperLength) ?? toNumber(legsBase.right?.upperLength) ?? 16;
  const defaultLegLower = toNumber(legsBase.left?.lowerLength) ?? toNumber(legsBase.right?.lowerLength) ?? 15;
  const defaultHandUpper = toNumber(handsBase.left?.upperLength) ?? toNumber(handsBase.right?.upperLength) ?? baseTorsoHeight * 0.55;
  const defaultHandLower = toNumber(handsBase.left?.lowerLength) ?? toNumber(handsBase.right?.lowerLength) ?? baseTorsoHeight * 0.45;

  const legEffector = (side) =>
    toPoint(legsBase[side]?.effectorCoordinate) ??
    (side === 'left'
      ? [0, -Math.max(baseTorsoHeight * 1.6, defaultLegUpper + defaultLegLower)]
      : [-Math.max(baseTorsoWidth * 0.9, 6), -Math.max(baseTorsoHeight * 1.6, defaultLegUpper + defaultLegLower)]);

  const handEffector = (side) =>
    toPoint(handsBase[side]?.effectorCoordinate) ??
    (side === 'left'
      ? [-Math.max(baseTorsoWidth * 0.75, 1), Math.max(baseTorsoHeight * 0.3, 1)]
      : [Math.max(baseTorsoWidth * 0.75, 1), Math.max(baseTorsoHeight * 0.3, 1)]);

  return {
    ...baseSafe,
    ...input,
    torso: sanitizeTorso(input.torso),
    head: sanitizeHead(input.head),
    legs: {
      left: sanitizeLimb(input.legs?.left, legsBase.left, {
        upperLength: defaultLegUpper,
        lowerLength: defaultLegLower,
        effectorCoordinate: legEffector('left'),
        positiveBend: legsBase.left?.positiveBend,
        foot: legsBase.left?.foot
      }),
      right: sanitizeLimb(input.legs?.right, legsBase.right, {
        upperLength: defaultLegUpper,
        lowerLength: defaultLegLower,
        effectorCoordinate: legEffector('right'),
        positiveBend: legsBase.right?.positiveBend,
        foot: legsBase.right?.foot
      })
    },
    hands: {
      left: sanitizeLimb(input.hands?.left, handsBase.left, {
        upperLength: defaultHandUpper,
        lowerLength: defaultHandLower,
        effectorCoordinate: handEffector('left'),
        positiveBend: handsBase.left?.positiveBend
      }),
      right: sanitizeLimb(input.hands?.right, handsBase.right, {
        upperLength: defaultHandUpper,
        lowerLength: defaultHandLower,
        effectorCoordinate: handEffector('right'),
        positiveBend: handsBase.right?.positiveBend
      })
    }
  };
}

function resolveHouseLayout(view, preferredPosition, preferredWidth, groundY, gapMultiplier = 2) {
  const viewLeft = Number.isFinite(view?.left) ? view.left : -400;
  const viewRight = Number.isFinite(view?.right) ? view.right : 400;
  const width = Number.isFinite(preferredWidth) ? preferredWidth : 180;
  const gap = width * gapMultiplier; // two-house gap between edges by default
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

function clampRange(value, min, max) {
  const num = Number.isFinite(value) ? value : Number(value);
  if (!Number.isFinite(num)) return min;
  return Math.min(Math.max(num, min), max);
}

function isObject(value) {
  return Boolean(value && typeof value === 'object');
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
return scene1;

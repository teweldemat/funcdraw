function scene3(sceneTime = 0, previousScene = null) {
  const cartoonLibrary = package('@funcdraw/testlib')?.cartoon ?? {};
  const stickmanModule = cartoonLibrary?.stickman ?? {};
  const houseBuilder = typeof cartoonLibrary?.house === 'function' ? cartoonLibrary.house : null;
  const staticBuilder = typeof stickmanModule?.static === 'function' ? stickmanModule.static : null;
  const zoomWalkBuilder = typeof stickmanModule?.zoomWalkMan === 'function' ? stickmanModule.zoomWalkMan : null;
  const zoomStepper = typeof stickmanModule?.steperManZoom === 'function' ? stickmanModule.steperManZoom : null;
  const consts = typeof constants === 'object' && constants ? constants : {};

  const view = consts.view ?? { left: -400, bottom: -300, right: 400, top: 300 };
  const groundY = 0;
  const doorDuration = Number.isFinite(consts.scene3?.doorDuration) ? consts.scene3.doorDuration : 1.5;
  const zoomDuration = Number.isFinite(consts.scene3?.zoomDuration) ? consts.scene3.zoomDuration : 3;
  const totalDuration = doorDuration + zoomDuration;
  const timeValue = typeof sceneTime === 'number' ? sceneTime : 0;
  const clampedTime = clampRange(timeValue, 0, totalDuration);
  const reverseTime = totalDuration - clampedTime;
  const doorProgress = clamp01(reverseTime / doorDuration);
  const zoomProgress = clamp01((reverseTime - doorDuration) / zoomDuration);

  if (!staticBuilder) {
    return {
      view,
      graphics: [
        {
          type: 'text',
          text: 'cartoon stickman static builder unavailable',
          position: [0, 12],
          fill: '#ef4444',
          fontSize: 12,
          align: 'center'
        }
      ]
    };
  }

  const baseHousePosition = Array.isArray(consts.scene3?.housePosition)
    ? consts.scene3.housePosition
    : [0, groundY];
  const desiredHouseWidth = Number.isFinite(consts.scene3?.houseWidth) ? consts.scene3.houseWidth : 180;
  const {
    primary: firstHousePosition,
    secondary: secondHousePosition,
    width: houseWidth
  } = resolveHouseLayout(view, baseHousePosition, desiredHouseWidth, groundY);

  const anchorBase = [
    secondHousePosition[0],
    (consts.manHouse?.anchor?.[1] ?? consts.shared?.anchor?.[1] ?? 26)
  ];
  const baseMeasurements = buildMeasurements(consts);

  const viewHeight = (typeof view?.top === 'number' && typeof view?.bottom === 'number')
    ? view.top - view.bottom
    : 600;
  const depthDelta = -(viewHeight * 0.25);
  const zoomTarget = Number.isFinite(consts.scene3?.zoomTarget) ? consts.scene3.zoomTarget : 1.3;
  const zoomValue = 1 + (zoomTarget - 1) * zoomProgress;
  const movingSide = consts.scene3?.movingSide ?? 'right';
  const baseLegOffset = consts.manHouse?.legOffsets?.[movingSide] ?? [-10, -26];
  const anchorPosition = [
    anchorBase[0],
    anchorBase[1] + depthDelta * zoomProgress
  ];
  const movingFootTargetY = anchorBase[1] + (baseLegOffset[1] ?? -20) + depthDelta * zoomProgress;
  const insideOffset = resolveInsideOffset(consts);
  const interiorHoldStart = Number.isFinite(consts.scene3?.insideHoldStart) ? consts.scene3.insideHoldStart : 0.2;
  const interiorHoldEnd = Number.isFinite(consts.scene3?.insideHoldEnd) ? consts.scene3.insideHoldEnd : 0.2;

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
  const exteriorReveal = clamp01(consts.scene3?.outsideReveal ?? 0.9);
  const showOutside =
    clampedTime >= interiorHoldStart &&
    clampedTime <= totalDuration - interiorHoldEnd &&
    doorProgress >= exteriorReveal;
  const posePosition = showOutside ? anchorPoint : insideAnchorPoint;

  const primaryDoorLevel = clamp01(consts.scene3?.primaryDoorOpenLevel ?? 0);

  const heroHouse = houseBuilder
    ? houseBuilder({
        position: secondHousePosition,
        width: houseWidth,
        doorOpenLevel: doorProgress,
        type: 'classic',
        interior: showOutside ? [] : heroGraphicsInside
      })
    : { graphics: [] };

  const otherHouse = houseBuilder
    ? houseBuilder({
        position: firstHousePosition,
        width: houseWidth,
        doorOpenLevel: primaryDoorLevel,
        type: 'classic',
        interior: []
      })
    : { graphics: [] };

  const graphics = [
    road({ left: view.left, right: view.right, y: groundY, stroke: '#94a3b8', width: 0.5 }),
    ...(Array.isArray(heroHouse.graphics) ? heroHouse.graphics : []),
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

function createLabel(doorProg, zoomProg, zoomTarget, delta, anchorY, hasZoomWalk, hasStepper, view, consts) {
  return {
    type: 'text',
    text: [
      'scene3: reverse house entry',
      `door closing ${Math.round((1 - doorProg) * 100)}%`,
      `zoom-out ${Math.round((1 - zoomProg) * 100)}%`,
      `target zoom ${zoomTarget.toFixed(1)}`,
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

function resolveLegBend(consts) {
  const override = consts?.scene3?.positiveBend ?? consts?.manHouse?.legs?.positiveBend;
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

function pickPose(value) {
  if (!value || typeof value !== 'object') return null;
  const position = isPoint(value.manPosition || value.position) ? (value.manPosition || value.position) : null;
  const measurements = isObject(value.manMeasurements || value.measurements)
    ? (value.manMeasurements || value.measurements)
    : null;
  if (!position && !measurements) return null;
  return { position, measurements };
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

function isObject(value) {
  return Boolean(value && typeof value === 'object');
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

return scene3;

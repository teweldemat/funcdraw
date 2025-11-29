const cartoonLib = package('@funcdraw/testlib')?.cartoon ?? {};
const createStickman = typeof cartoonLib.stickman === 'function' ? cartoonLib.stickman : () => ({ graphics: [] });

const time = typeof t === 'number' ? t : 0;
const phase = time * 3;
const leftLiftAmount = Math.max(Math.sin(phase), 0);
const rightLiftAmount = Math.max(Math.sin(phase + Math.PI), 0);
const heroPosition = [0, 10];

const referenceSkeleton = createStickman({ position: heroPosition })?.skeleton ?? {};
const leftLegBaseDrop = resolveLegBaseDrop(referenceSkeleton, 'left');
const rightLegBaseDrop = resolveLegBaseDrop(referenceSkeleton, 'right');

const leftFootLift = leftLegBaseDrop + leftLiftAmount * 3.2;
const leftFootOffsetX = -2 + leftLiftAmount * 0.6;
const rightFootLift = rightLegBaseDrop + rightLiftAmount * 3.2;
const rightFootOffsetX = 2 - rightLiftAmount * 0.6;
const leftLegTarget = [leftFootOffsetX, leftFootLift];
const rightLegTarget = [rightFootOffsetX, rightFootLift];

const hero = createStickman({
  position: heroPosition,
  measurements: {
    torso: { direction: 'front' },
    legs: {
      left: {
        effectorCoordinate: leftLegTarget
      },
      right: {
        effectorCoordinate: rightLegTarget
      }
    }
  }
});

const armSpeed = time * 1.2;
const leftAttachment = hero.skeleton?.hands?.left?.attachmentPoint ?? [-3, 22];
const rightAttachment = hero.skeleton?.hands?.right?.attachmentPoint ?? [3, 22];
const leftArmLength =
  (hero.skeleton?.hands?.left?.lengths?.upper ?? 4) + (hero.skeleton?.hands?.left?.lengths?.lower ?? 3);
const rightArmLength =
  (hero.skeleton?.hands?.right?.lengths?.upper ?? 4) + (hero.skeleton?.hands?.right?.lengths?.lower ?? 3);

const leftHandTargetWorld = [
  leftAttachment[0] + leftArmLength * Math.cos(armSpeed),
  leftAttachment[1] + leftArmLength * Math.sin(armSpeed)
];
const rightHandTargetWorld = [
  rightAttachment[0] + rightArmLength * Math.cos(armSpeed + Math.PI),
  rightAttachment[1] + rightArmLength * Math.sin(armSpeed + Math.PI)
];
const leftHandEffector = [
  leftHandTargetWorld[0] - heroPosition[0],
  leftHandTargetWorld[1] - heroPosition[1]
];
const rightHandEffector = [
  rightHandTargetWorld[0] - heroPosition[0],
  rightHandTargetWorld[1] - heroPosition[1]
];

const heroWithArms = createStickman({
  position: heroPosition,
  measurements: {
    torso: { direction: 'front' },
    hands: {
      left: { effectorCoordinate: leftHandEffector },
      right: { effectorCoordinate: rightHandEffector }
    },
    legs: {
      left: { effectorCoordinate: leftLegTarget },
      right: { effectorCoordinate: rightLegTarget }
    }
  }
});

const ground = {
  type: 'line',
  from: [-24, 0],
  to: [24, 0],
  stroke: '#94a3b8',
  width: 0.6
};

const caption = {
  type: 'text',
  text: 'Left leg lift exercise',
  position: [0, -3],
  fill: '#0f172a',
  fontSize: 3,
  align: 'center'
};

function resolveLegBaseDrop(referenceSkeleton, side) {
  const upperLength = referenceSkeleton?.legs?.[side]?.lengths?.upper;
  const lowerLength = referenceSkeleton?.legs?.[side]?.lengths?.lower;
  const fallbackUpper = 5.2;
  const fallbackLower = 4.8;
  const safeUpper = typeof upperLength === 'number' && Number.isFinite(upperLength) ? upperLength : fallbackUpper;
  const safeLower = typeof lowerLength === 'number' && Number.isFinite(lowerLength) ? lowerLength : fallbackLower;
  return -(safeUpper + safeLower);
}

function createDebugCross(center, size = 1.6) {
  if (!Array.isArray(center) || center.length < 2) {
    return [];
  }
  const half = size * 0.5;
  const stroke = '#ffffff';
  const width = Math.max(size * 0.18, 0.18);
  return [
    {
      type: 'line',
      from: [center[0] - half, center[1]],
      to: [center[0] + half, center[1]],
      stroke,
      width
    },
    {
      type: 'line',
      from: [center[0], center[1] - half],
      to: [center[0], center[1] + half],
      stroke,
      width
    }
  ];
}

const finalSkeleton = heroWithArms.skeleton ?? hero.skeleton ?? {};
const finalHands = finalSkeleton.hands ?? {};
const finalLegs = finalSkeleton.legs ?? {};

const finalLeftShoulder = finalHands.left?.attachmentPoint ?? leftAttachment;
const finalRightShoulder = finalHands.right?.attachmentPoint ?? rightAttachment;
const finalLeftHandTarget = finalHands.left?.targetPoint ?? leftHandTargetWorld;
const finalRightHandTarget = finalHands.right?.targetPoint ?? rightHandTargetWorld;
const finalLeftLegTarget =
  finalLegs.left?.targetPoint ?? [heroPosition[0] + leftLegTarget[0], heroPosition[1] + leftLegTarget[1]];
const finalRightLegTarget =
  finalLegs.right?.targetPoint ?? [heroPosition[0] + rightLegTarget[0], heroPosition[1] + rightLegTarget[1]];

const debugPoints = [
  ...createDebugCross(finalLeftShoulder),
  ...createDebugCross(finalRightShoulder),
  ...createDebugCross(finalLeftHandTarget),
  ...createDebugCross(finalRightHandTarget),
  ...createDebugCross(finalLeftLegTarget, 1.8),
  ...createDebugCross(finalRightLegTarget, 1.8)
];

function isPoint(value) {
  return (
    Array.isArray(value) &&
    value.length === 2 &&
    typeof value[0] === 'number' &&
    typeof value[1] === 'number' &&
    Number.isFinite(value[0]) &&
    Number.isFinite(value[1])
  );
}

function shouldTreatKeyAsPoint(key) {
  if (!key) {
    return false;
  }
  const text = String(key).toLowerCase();
  return (
    text.includes('point') ||
    text.includes('center') ||
    text.includes('target') ||
    text.includes('attachment') ||
    text.includes('position') ||
    text.includes('joint') ||
    text.includes('hinge') ||
    text.includes('effector')
  );
}

function collectSkeletonPoints(source) {
  if (!source || typeof source !== 'object') {
    return [];
  }
  const points = [];
  const seen = new Set();
  const stack = [{ value: source, pointContext: false }];
  while (stack.length > 0) {
    const { value, pointContext } = stack.pop();
    if (isPoint(value) && pointContext) {
      const key = `${value[0]},${value[1]}`;
      if (!seen.has(key)) {
        seen.add(key);
        points.push(value);
      }
      continue;
    }
    if (Array.isArray(value)) {
      for (const item of value) {
        if (item && (typeof item === 'object' || Array.isArray(item))) {
          stack.push({ value: item, pointContext });
        }
      }
      continue;
    }
    if (value && typeof value === 'object') {
      for (const [key, child] of Object.entries(value)) {
        if (child && (typeof child === 'object' || Array.isArray(child))) {
          const nextContext = pointContext || shouldTreatKeyAsPoint(key);
          stack.push({ value: child, pointContext: nextContext });
        }
      }
    }
  }
  return points;
}

function createDebugDot(center) {
  if (!isPoint(center)) {
    return null;
  }
  return {
    type: 'circle',
    center,
    radius:0.1,
    fill: '#dc2626',
    stroke: '#991b1b',
    width: 0.08
  };
}

const skeletonPoints = collectSkeletonPoints(finalSkeleton);
const skeletonPointDots = skeletonPoints.map(createDebugDot).filter(Boolean);

return {
  view: { left: -26, bottom: -6, right: 26, top: 28 },
  graphics: [
    ground,
    ...(heroWithArms.graphics ?? []),
    ...debugPoints,
    ...skeletonPointDots,
    caption
  ]
};

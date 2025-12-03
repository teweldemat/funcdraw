const buildOptions = (progress, extras = {}) => ({
  progress,
  ...extras
});

const distance = (a, b) => {
  const dx = (a[0] || 0) - (b[0] || 0);
  const dy = (a[1] || 0) - (b[1] || 0);
  return Math.sqrt(dx * dx + dy * dy);
};

const zoomScalingSuite = {
  name: "zoom scales torso dimensions",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const factor = 2;
    const base = steperFn(buildOptions(0, { zoomProgress: 0, zoomFactor: factor }));
    const zoomed = steperFn(buildOptions(0, { zoomProgress: 1, zoomFactor: factor }));
    const baseMeasurements = base.sequenceState.measurements.torso;
    const zoomMeasurements = zoomed.sequenceState.measurements.torso;
    const baseWidth = Math.max(1e-6, baseMeasurements.width);
    const baseHeight = Math.max(1e-6, baseMeasurements.height);
    const widthRatio = zoomMeasurements.width / baseWidth;
    const heightRatio = zoomMeasurements.height / baseHeight;

    return [
      assert.greater(zoomMeasurements.width, baseMeasurements.width),
      assert.greater(zoomMeasurements.height, baseMeasurements.height),
      assert.less(Math.abs(widthRatio - factor), 1e-6),
      assert.less(Math.abs(heightRatio - factor), 1e-6)
    ];
  }
};

const swingSuite = {
  name: "moving leg swings while fixed leg stays put",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const base = steperFn(buildOptions(0, { movingFeetStartY: -11, movingFeetEndY: -6 }));
    const mid = steperFn(buildOptions(0.5, { movingFeetStartY: -11, movingFeetEndY: -6 }));

    const baseLeftY = base.skeleton.legs.left.reachTarget[1];
    const baseRightY = base.skeleton.legs.right.reachTarget[1];

    const midLeftY = mid.skeleton.legs.left.reachTarget[1];
    const midRightY = mid.skeleton.legs.right.reachTarget[1];

    return [
      assert.less(Math.abs(baseLeftY - midLeftY), 1e-6), // fixed leg unchanged in world space
      assert.greater(midRightY, baseRightY) // moving leg lifts toward target
    ];
  }
};

const metadataSuite = {
  name: "step metadata mirrors inputs",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const progress = 0.42;
    const zoomProgress = 0.7;
    const zoomFactor = 1.6;
    const result = steperFn(buildOptions(progress, { zoomProgress, zoomFactor }));
    const anchor = result.step.anchorPoint;
    const position = result.sequenceState.position;

    return [
      assert.less(Math.abs(result.step.progress - progress), 1e-6),
      assert.less(Math.abs(result.step.zoomProgress - zoomProgress), 1e-6),
      assert.less(Math.abs(result.step.zoomFactor - zoomFactor), 1e-6),
      assert.less(distance(anchor, position), 1e-6)
    ];
  }
};

const straightLegSuite = {
  name: "legs remain straight (no visible knee bend)",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const sample = steperFn(buildOptions(0.25, { swing: 0.5 }));
    const alt = steperFn(buildOptions(0.75, { swing: 0.5 }));
    const legsA = sample?.skeleton?.legs || {};
    const legsB = alt?.skeleton?.legs || {};

    const checkDeviation = (leg) => {
      if (!leg?.joints?.attachment || !leg?.joints?.hinge || !leg?.joints?.effector) {
        return Number.MAX_SAFE_INTEGER;
      }
      return pointLineDistance(leg.joints.hinge, leg.joints.attachment, leg.joints.effector);
    };

    const deviations = [
      checkDeviation(legsA.left),
      checkDeviation(legsA.right),
      checkDeviation(legsB.left),
      checkDeviation(legsB.right)
    ];

    return deviations.map((dev) => assert.less(dev, 1e-3));
  }
};

const yOnlyFootInputSuite = {
  name: "y-only foot input keeps x controlled by model",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const overrideDepth = -14;
    const progress = 0.3;
    const result = steperFn(buildOptions(progress, {
      measurements: {
        legs: {
          left: { effectorCoordinate: { x: -2, y: overrideDepth } },
          right: { effectorCoordinate: { x: 2, y: overrideDepth } }
        }
      }
    }));
    const legs = result.sequenceState.measurements.legs;
    const leftX = legs.left.effectorCoordinate[0];
    const rightX = legs.right.effectorCoordinate[0];
    const leftY = legs.left.effectorCoordinate[1];
    const rightY = legs.right.effectorCoordinate[1];

    return [
      assert.less(leftX, 0),
      assert.greater(rightX, 0),
      assert.less(Math.abs(leftY - overrideDepth), 0.2),
      assert.less(Math.abs(rightY - overrideDepth), 0.2)
    ];
  }
};

const fixedFootWorldSuite = {
  name: "fixed leg stays in place across the stride",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const depth = -12;
    const endDepth = -6;
    const baseMeasurements = {
      legs: {
        left: { effectorCoordinate: { x: -2, y: depth } },
        right: { effectorCoordinate: { x: 2, y: depth } }
      }
    };
    const start = steperFn(buildOptions(0, {
      zoomProgress: 0,
      zoomFactor: 1,
      measurements: baseMeasurements,
      movingFeetStartY: depth,
      movingFeetEndY: endDepth
    }));
    const end = steperFn(buildOptions(1, {
      zoomProgress: 0,
      zoomFactor: 1,
      measurements: baseMeasurements,
      movingFeetStartY: depth,
      movingFeetEndY: endDepth
    }));
    const startWorld = start?.skeleton?.legs?.left?.reachTarget;
    const endWorld = end?.skeleton?.legs?.left?.reachTarget;
    const startLength = start?.sequenceState?.measurements?.legs?.left;
    const endLength = end?.sequenceState?.measurements?.legs?.left;
    if (!isPoint(startWorld) || !isPoint(endWorld)) {
      return [assert.fail("missing leg reach target")];
    }

    return [
      assert.less(distance(startWorld, endWorld), 0.2),
      assert.less(Math.abs(startWorld[1] - endWorld[1]), 0.2),
      assert.greater(
        Math.abs((endLength.upperLength + endLength.lowerLength) - (startLength.upperLength + startLength.lowerLength)),
        0.1
      )
    ];
  }
};

function pointLineDistance(point, a, b) {
  const px = point?.[0] ?? 0;
  const py = point?.[1] ?? 0;
  const ax = a?.[0] ?? 0;
  const ay = a?.[1] ?? 0;
  const bx = b?.[0] ?? 0;
  const by = b?.[1] ?? 0;
  const dx = bx - ax;
  const dy = by - ay;
  const denom = Math.sqrt(dx * dx + dy * dy) || 1;
  return Math.abs((px - ax) * dy - (py - ay) * dx) / denom;
}

function isPoint(value) {
  return (
    Array.isArray(value) &&
    value.length >= 2 &&
    typeof value[0] === "number" &&
    typeof value[1] === "number" &&
    Number.isFinite(value[0]) &&
    Number.isFinite(value[1])
  );
}

return [zoomScalingSuite, swingSuite, metadataSuite, straightLegSuite, yOnlyFootInputSuite, fixedFootWorldSuite];

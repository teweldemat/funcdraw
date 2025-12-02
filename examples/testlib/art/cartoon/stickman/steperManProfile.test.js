const buildExampleOptions = (progress) => {
  const fixedPoint = [-16, 0];
  const movingStart = [-24, 0];
  const movingTarget = [-8, 0];
  const anchorX = fixedPoint[0] + (movingTarget[0] - fixedPoint[0]) * (progress * 0.5);
  const anchorLift = Math.sin(Math.PI * progress) * 0.6;
  return {
    fixedFeet: "left",
    fixedFeetPoint: fixedPoint,
    movingFeetStartPoint: movingStart,
    movingFeetTargetPoint: movingTarget,
    position: [anchorX, 9.4 + anchorLift],
    measurements: { torso: { direction: "right" }, head: { direction: "right" } },
    handSwing: { amplitude: 12, forwardOffset: 0, lift: 0.45, mode: "sine" },
    progress
  };
};

const suite = {
  name: "hands move from behind to ahead with similar magnitude (example params)",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const behind = steperFn(buildExampleOptions(0));
    const ahead = steperFn(buildExampleOptions(1));
    const behindLeft = behind.sequenceState.measurements.hands.left.effectorCoordinate[0];
    const aheadLeft = ahead.sequenceState.measurements.hands.left.effectorCoordinate[0];
    const behindRight = behind.sequenceState.measurements.hands.right.effectorCoordinate[0];
    const aheadRight = ahead.sequenceState.measurements.hands.right.effectorCoordinate[0];

    const results = [];
    results.push(assert.less(behindLeft, 0));
    results.push(assert.greater(aheadLeft, 0));
    results.push(assert.greater(behindRight, 0));
    results.push(assert.less(aheadRight, 0)); // right hand swings across

    const checkRatio = (a, b) => {
      const bigger = Math.max(Math.abs(a), Math.abs(b));
      const smaller = Math.max(1e-6, Math.min(Math.abs(a), Math.abs(b)));
      return bigger / smaller;
    };
    results.push(assert.less(checkRatio(behindLeft, aheadLeft), 5)); // left magnitude stays comparable
    results.push(assert.less(checkRatio(behindRight, aheadRight), 5)); // right magnitude stays comparable

    return results;
  }
};

const exampleSuite = {
  name: "example mirror swing matches overlay scenario",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const behind = steperFn(buildExampleOptions(0));
    const ahead = steperFn(buildExampleOptions(1));
    const behindLeft = behind.sequenceState.measurements.hands.left.effectorCoordinate[0];
    const aheadLeft = ahead.sequenceState.measurements.hands.left.effectorCoordinate[0];
    const behindRight = behind.sequenceState.measurements.hands.right.effectorCoordinate[0];
    const aheadRight = ahead.sequenceState.measurements.hands.right.effectorCoordinate[0];

    console.log(behindLeft);
    console.log(aheadLeft);

    return [
      assert.less(behindLeft, 0),
      assert.greater(aheadLeft, 0),
      assert.greater(behindRight, 0),
      assert.less(aheadRight, 0),
      assert.less(Math.abs(Math.abs(behindLeft) - Math.abs(aheadLeft)), Math.max(Math.abs(behindLeft), Math.abs(aheadLeft)) * 0.05),
      assert.less(Math.abs(Math.abs(behindRight) - Math.abs(aheadRight)), Math.max(Math.abs(behindRight), Math.abs(aheadRight)) * 0.05)
    ];
  }
};

return [suite, exampleSuite];

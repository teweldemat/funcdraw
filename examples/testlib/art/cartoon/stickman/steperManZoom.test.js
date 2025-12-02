const buildOptions = (progress, extras = {}) => ({
  progress,
  swing: 0.5,
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
  name: "legs swing opposite in depth as progress advances",
  cases: [{}],
  test: (steperFn, _caseData) => {
    const base = steperFn(buildOptions(0, { swing: 0.5 }));
    const quarter = steperFn(buildOptions(0.25, { swing: 0.5 }));
    const threeQuarter = steperFn(buildOptions(0.75, { swing: 0.5 }));

    const baseLeftY = base.sequenceState.measurements.legs.left.effectorCoordinate[1];
    const baseRightY = base.sequenceState.measurements.legs.right.effectorCoordinate[1];

    const quarterLeftY = quarter.sequenceState.measurements.legs.left.effectorCoordinate[1];
    const quarterRightY = quarter.sequenceState.measurements.legs.right.effectorCoordinate[1];

    const threeLeftY = threeQuarter.sequenceState.measurements.legs.left.effectorCoordinate[1];
    const threeRightY = threeQuarter.sequenceState.measurements.legs.right.effectorCoordinate[1];

    return [
      assert.greater(quarterLeftY, baseLeftY),  // left leg lifts (shorter drop)
      assert.less(quarterRightY, baseRightY),   // right leg drops deeper
      assert.greater(quarterLeftY, quarterRightY),
      assert.less(threeLeftY, baseLeftY),       // swing reverses later in the cycle
      assert.greater(threeRightY, baseRightY),
      assert.less(threeLeftY, threeRightY)
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

return [zoomScalingSuite, swingSuite, metadataSuite];

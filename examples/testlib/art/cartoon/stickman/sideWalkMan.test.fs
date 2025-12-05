{
  baseline: {
    name: "returns base pose when displacement is zero";
    cases: [
      {
        options: { initialPosition: [1, 5]; displacement: 0; progress: 0 };
        expectedPosition: [1, 5];
        expectedLeftOffset: [-2, -11];
        expectedRightOffset: [2, -11];
        expectedDirection: "right";
      }
    ];
    test: (res, caseData) => {
      pos:if res = null then [0, 0] else helpers.normalizePoint(res.position, [0, 0]);
      meas:helpers.normalizeInput(if res = null then null else res.measurements, {});
      torso:helpers.normalizeInput(meas.torso, {});
      head:helpers.normalizeInput(meas.head, {});
      legs:helpers.normalizeInput(meas.legs, {});
      leftLeg:helpers.normalizeInput(legs.left, {});
      rightLeg:helpers.normalizeInput(legs.right, {});
      leftOffset:helpers.normalizePoint(leftLeg.effectorCoordinate, [0, 0]);
      rightOffset:helpers.normalizePoint(rightLeg.effectorCoordinate, [0, 0]);
      eval [
      assert.approx(pos[0], caseData.expectedPosition[0], 0.0001);
      assert.approx(pos[1], caseData.expectedPosition[1], 0.0001);
      assert.equal(torso.direction, caseData.expectedDirection);
      assert.equal(head.direction, caseData.expectedDirection);
      assert.approx(leftOffset[0], caseData.expectedLeftOffset[0], 0.0001);
      assert.approx(leftOffset[1], caseData.expectedLeftOffset[1], 0.0001);
      assert.approx(rightOffset[0], caseData.expectedRightOffset[0], 0.0001);
      assert.approx(rightOffset[1], caseData.expectedRightOffset[1], 0.0001);
      ]
    };
  };

  facing: {
    name: "normalizes facing direction";
    cases: [
      { options: { direction: "left"; displacement: 0; progress: 0 }; expectedDirection: "left" }
    ];
    test: (res, caseData) => {
      meas:helpers.normalizeInput(if res = null then null else res.measurements, {});
      torso:helpers.normalizeInput(meas.torso, {});
      head:helpers.normalizeInput(meas.head, {});
      eval [ assert.equal(torso.direction, caseData.expectedDirection);
      assert.equal(head.direction, caseData.expectedDirection);]
    };
  };

  movement: {
    name: "shifts anchor in stride direction";
    cases: [
      { options: { displacement: 8; progress: 1 }; expectedSign: 1 },
      { options: { displacement: -6; progress: 1 }; expectedSign: -1 }
    ];
    test: (res, caseData) => {
      opts:helpers.normalizeInput(caseData.options ?? {}, {});
      base:helpers.normalizePoint(opts.initialPosition, [0, 0]);
      pos:if res = null then [0, 0] else helpers.normalizePoint(res.position, [0, 0]);
      deltaX:pos[0] - base[0];
      expectedSign:helpers.resolveNumber(caseData.expectedSign, 1);
      eval if expectedSign > 0 then assert.greater(deltaX, 0) else assert.greater(0, deltaX);
    };
  };

  eval [baseline, facing, movement];
}

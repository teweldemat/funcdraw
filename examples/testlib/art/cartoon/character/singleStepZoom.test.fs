{
  eval [
    {
      name: "moves the moving foot toward target y and shifts anchor half";
      test: (fn) =>
      {
        anchor: [0, 0];
        base:
        {
          direction: "front";
          leftLeg: { end: [-3, -14]; };
          rightLeg: { end: [3, -14]; };
        };
        targetY: -10;
        progress: 0.5;
        zoom: 0.1;
        profile: fn(anchor, base, "left", targetY, progress, zoom);

        startLeftWorldY: anchor[1] + base.leftLeg.end[1];
        dy: targetY - startLeftWorldY;
        expectedAnchorY: anchor[1] + dy * progress * 0.5;
        fixedFootWorldY: anchor[1] + base.rightLeg.end[1];
        shiftedAnchorY: expectedAnchorY;
        scale: 1 - (shiftedAnchorY - anchor[1]) * zoom;
        expectedZoomedAnchorY: fixedFootWorldY + (shiftedAnchorY - fixedFootWorldY) * scale;
        expectedMovingWorldY: startLeftWorldY + dy * progress;
        expectedRightWorldY: anchor[1] + base.rightLeg.end[1];

        eval
        [
          assert.approx(profile.anchor[1], expectedZoomedAnchorY, 0.0001),
          assert.approx(profile.leftLeg.end[1] + profile.anchor[1], expectedMovingWorldY, 0.0001),
          assert.approx(profile.rightLeg.end[1] + profile.anchor[1], expectedRightWorldY, 0.0001),
          assert.equal(profile.leftLeg.end[0], 0),
          assert.equal(profile.rightLeg.end[0], 0),
          assert.approx(profile.leftLeg.upper + profile.leftLeg.lower, math.Abs(profile.leftLeg.end[1]), 0.0001),
          assert.approx(profile.rightLeg.upper + profile.rightLeg.lower, math.Abs(profile.rightLeg.end[1]), 0.0001)
        ];
      };
    },
    {
      name: "scales body dimensions from vertical displacement";
      test: (fn) =>
      {
        anchor: [0, 0];
        base: {};
        targetY: -20;
        progress: 0.25;
        zoom: 0.02;
        profile: fn(anchor, base, "right", targetY, progress, zoom);

        startWorldY: anchor[1] + defaultMeasurements.rightLeg.end[1];
        dy: targetY - startWorldY;
        bodyShift: dy * progress * 0.5;
        scale: 1 - bodyShift * zoom;
        fixedFootWorldY: anchor[1] + defaultMeasurements.leftLeg.end[1];
        shiftedAnchorY: anchor[1] + bodyShift;
        expectedZoomedAnchorY: fixedFootWorldY + (shiftedAnchorY - fixedFootWorldY) * scale;

        eval
        [
          assert.approx(profile.anchor[1], expectedZoomedAnchorY, 0.0001),
          assert.approx(profile.height, defaultMeasurements.height * scale, 0.0001),
          assert.approx(profile.headRadius, defaultMeasurements.headRadius * scale, 0.0001),
          assert.approx(profile.neckLength, defaultMeasurements.neckLength * scale, 0.0001),
          assert.approx(profile.shoulderWidth, defaultMeasurements.shoulderWidth * scale, 0.0001),
          assert.approx(profile.thighWidth, defaultMeasurements.thighWidth * scale, 0.0001),
          assert.approx(profile.leftLeg.upper + profile.leftLeg.lower, math.Abs(profile.leftLeg.end[1]), 0.0001),
          assert.approx(profile.rightLeg.upper + profile.rightLeg.lower, math.Abs(profile.rightLeg.end[1]), 0.0001),
          assert.approx(profile.leftHand.upper + profile.leftHand.lower, math.Abs(profile.leftHand.end[1]), 0.0001),
          assert.approx(profile.rightHand.upper + profile.rightHand.lower, math.Abs(profile.rightHand.end[1]), 0.0001)
        ];
      };
    },
    {
      name: "keeps hands vertical and straight";
      test: (fn) =>
      {
        anchor: [0, 0];
        targetY: -12;
        progress: 0.5;
        zoom: 0.05;
        profile: fn(anchor, {}, "left", targetY, progress, zoom);
        eval
        [
          assert.equal(profile.leftHand.end[0], 0),
          assert.equal(profile.rightHand.end[0], 0),
          assert.approx(profile.leftHand.upper + profile.leftHand.lower, math.Abs(profile.leftHand.end[1]), 0.0001),
          assert.approx(profile.rightHand.upper + profile.rightHand.lower, math.Abs(profile.rightHand.end[1]), 0.0001)
        ];
      };
    },
    {
      name: "mirrors hand extension against leg extension";
      test: (fn) =>
      {
        anchor: [0, 0];
        base:
        {
          direction: "front";
          leftLeg: { end: [-3, -11]; };
          rightLeg: { end: [3, -17]; };
        };
        targetY: -22;
        progress: 0.5;
        zoom: 0.05;
        profile: fn(anchor, base, "left", targetY, progress, zoom);
        eval
        [
          assert.approx(profile.leftHand.end[1] - profile.rightHand.end[1], profile.rightLeg.end[1] - profile.leftLeg.end[1], 0.0001),
          assert.approx((profile.leftHand.end[1] + profile.rightHand.end[1]) / 2, defaultMeasurements.leftHand.end[1] * (profile.height / defaultMeasurements.height), 0.0001)
        ];
      };
    },
    {
      name: "requires a positive zoomFactor";
      test: (fn) =>
      {
        res: fn([0, 0], {}, "left", -10, 0.5, 0);
        eval [assert.iserror(res)];
      };
    }
    ,
    {
      name: "can chain across steps without jumps";
      test: (fn) =>
      {
        first: fn([0, 0], {}, "left", -10, 1, 0.05);
        second: fn(first.anchor, first, "right", -999, 0, 0.05);
        eval
        [
          assert.approx(second.anchor[0], first.anchor[0], 0.0001),
          assert.approx(second.anchor[1], first.anchor[1], 0.0001),
          assert.approx(second.height, first.height, 0.0001),
          assert.approx(second.leftHand.end[1], first.leftHand.end[1], 0.0001),
          assert.approx(second.rightHand.end[1], first.rightHand.end[1], 0.0001),
          assert.approx(second.leftLeg.end[1], first.leftLeg.end[1], 0.0001),
          assert.approx(second.rightLeg.end[1], first.rightLeg.end[1], 0.0001)
        ];
      };
    }
  ];
}

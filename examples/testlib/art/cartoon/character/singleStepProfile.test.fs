{
  eval [
    {
      name: "moves the left foot toward a world target with lift";
      test: (fn) =>
      {
        anchor: [0, 0];
        target: [-1, -10];
        base:
        {
          leftLeg: { end: [-3, -14]; };
          rightLeg: { end: [3, -14]; };
        };
        profile: fn(anchor, base, "left", target, 0.5);
        dx: target[0] - (anchor[0] + base.leftLeg.end[0]);
        dy: target[1] - (anchor[1] + base.leftLeg.end[1]);
        expectedYLinear: base.leftLeg.end[1] + dy * 0.5;
        stepLift: (defaultMeasurements.leftLeg.upper + defaultMeasurements.leftLeg.lower) * 0.2;
        expectedY: expectedYLinear + math.Sin(math.Pi * 0.5) * stepLift;
        staticWorld: [anchor[0] + base.rightLeg.end[0], anchor[1] + base.rightLeg.end[1]];
        eval
        [
          assert.approx(profile.leftLeg.end[1], expectedY, 0.0001),
          assert.equal(profile.rightLeg.end[0] + profile.anchor[0], staticWorld[0]),
          assert.equal(profile.rightLeg.end[1] + profile.anchor[1], staticWorld[1])
        ];
      };
    },
    {
      name: "moves the right foot and keeps lengths and sign";
      test: (fn) =>
      {
        anchor: [1, -2];
        target: [5, -12];
        base: {};
        profile: fn(anchor, base, "right", target, 1);
        startWorldX: anchor[0] + defaultMeasurements.rightLeg.end[0];
        dx: target[0] - startWorldX;
        anchorShift: dx * 0.5;
        expected: [defaultMeasurements.rightLeg.end[0] + dx * 0.5, target[1] - anchor[1]];
        eval
        [
          assert.equal(profile.rightLeg.end[0], expected[0]),
          assert.equal(profile.rightLeg.end[1], expected[1]),
          assert.equal(profile.rightLeg.upper, defaultMeasurements.rightLeg.upper),
          assert.equal(profile.rightLeg.lower, defaultMeasurements.rightLeg.lower),
          assert.equal(profile.rightLeg.sign, defaultMeasurements.rightLeg.sign),
          assert.equal(profile.leftLeg.end[0] + profile.anchor[0], anchor[0] + defaultMeasurements.leftLeg.end[0]),
          assert.equal(profile.leftLeg.end[1] + profile.anchor[1], anchor[1] + defaultMeasurements.leftLeg.end[1])
        ];
      };
    }
    ,
    {
      name: "keeps start and end foot heights aligned";
      test: (fn) =>
      {
        anchor: [2, -1];
        base:
        {
          leftLeg: { end: [-3, -14]; };
          rightLeg: { end: [3, -14]; };
        };
        target: [anchor[0] - 8, anchor[1] - 14];
        startProfile: fn(anchor, base, "left", target, 0);
        endProfile: fn(anchor, base, "left", target, 1);
        dx: target[0] - (anchor[0] + base.leftLeg.end[0]);
        staticWorld: [anchor[0] + base.rightLeg.end[0], anchor[1] + base.rightLeg.end[1]];

        eval
        [
          assert.equal(startProfile.anchor[0], anchor[0]),
          assert.equal(startProfile.anchor[0], anchor[0]),
          assert.equal(endProfile.anchor[0], anchor[0] + dx * 0.5),
          assert.equal(startProfile.leftLeg.end[1], base.leftLeg.end[1]),
          assert.equal(endProfile.leftLeg.end[1], base.leftLeg.end[1]),
          assert.equal(startProfile.rightLeg.end[1], base.rightLeg.end[1]),
          assert.equal(endProfile.rightLeg.end[1] + endProfile.anchor[1], staticWorld[1]),
          assert.equal(endProfile.rightLeg.end[0] + endProfile.anchor[0], staticWorld[0])
        ];
      };
    }
    ,
    {
      name: "shifts anchor horizontally with the moving foot";
      test: (fn) =>
      {
        anchor: [0, 0];
        base:
        {
          leftLeg: { end: [-3, -14]; };
        };
        target: [3, -14];
        progress: 0.5;
        dx: target[0] - (anchor[0] + base.leftLeg.end[0]);
        profile: fn(anchor, base, "left", target, progress);

        eval
        [
          assert.approx(profile.anchor[0], anchor[0] + dx * 0.5 * progress, 0.0001),
          assert.equal(profile.anchor[1], anchor[1])
        ];
      };
    }
  ];
}

[
    {
      name: "moves anchor linearly with progress";
      test: (fn) =>
      {
        start: [2, -1];
        distance: 12;
        stride: 6;
        p: 0.25;
        profile: fn(start, {}, distance, stride, p);
        eval
        [
          assert.approx(profile.anchor[0], start[0] + distance * p, 0.0001),
          assert.equal(profile.anchor[1], start[1])
        ];
      };
    },
    {
      name: "reaches end anchor at progress 1";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 18;
        stride: 6;
        profile: fn(start, {}, distance, stride, 1);
        eval
        [
          assert.approx(profile.anchor[0], start[0] + distance, 0.0001),
          assert.equal(profile.anchor[1], start[1])
        ];
      };
    },
    {
      name: "supports a partial final step";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 10;
        stride: 6;
        profile: fn(start, {}, distance, stride, 1);
        eval
        [
          assert.approx(profile.anchor[0], start[0] + distance, 0.0001),
          assert.equal(profile.anchor[1], start[1])
        ];
      };
    },
    {
      name: "starts by moving the left leg";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 24;
        stride: 6;
        progress: 0.1;
        base:
        {
          direction: "right";
          leftLeg: { sign: 1; };
          rightLeg: { sign: 1; };
        };
        profile: fn(start, base, distance, stride, progress);

        traveled: distance * progress;
        localProgress: traveled / stride;
        dx: 2 * stride;
        anchorShift: dx * localProgress * 0.5;
        expectedLeftX: defaultMeasurements.leftLeg.end[0] + dx * localProgress - anchorShift;
        expectedRightX: defaultMeasurements.rightLeg.end[0] - anchorShift;

        eval
        [
          assert.approx(profile.leftLeg.end[0], expectedLeftX, 0.0001),
          assert.approx(profile.rightLeg.end[0], expectedRightX, 0.0001)
        ];
      };
    },
    {
      name: "moves left for negative distances";
      test: (fn) =>
      {
        start: [0, 0];
        distance: -12;
        stride: 6;
        p: 0.5;
        profile: fn(start, {}, distance, stride, p);
        eval
        [
          assert.approx(profile.anchor[0], start[0] + distance * p, 0.0001),
          assert.equal(profile.anchor[1], start[1])
        ];
      };
    }
]

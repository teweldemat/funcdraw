[
    {
      name: "moves feet midpoint linearly with progress";
      test: (fn) =>
      {
        start: [2, -1];
        distance: 12;
        stride: 6;
        zoom: 0.05;
        p: 0.25;
        profile0: fn(start, {}, distance, stride, 0, zoom);
        profile: fn(start, {}, distance, stride, p, zoom);
        mid0: (profile0.anchor[1] + profile0.leftLeg.end[1] + profile0.anchor[1] + profile0.rightLeg.end[1]) / 2;
        mid: (profile.anchor[1] + profile.leftLeg.end[1] + profile.anchor[1] + profile.rightLeg.end[1]) / 2;
        eval
        [
          assert.equal(profile.anchor[0], start[0]),
          assert.approx(mid, mid0 + distance * p, 0.0001)
        ];
      };
    },
    {
      name: "reaches end feet midpoint at progress 1";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 18;
        stride: 6;
        zoom: 0.05;
        profile0: fn(start, {}, distance, stride, 0, zoom);
        profile: fn(start, {}, distance, stride, 1, zoom);
        mid0: (profile0.anchor[1] + profile0.leftLeg.end[1] + profile0.anchor[1] + profile0.rightLeg.end[1]) / 2;
        mid: (profile.anchor[1] + profile.leftLeg.end[1] + profile.anchor[1] + profile.rightLeg.end[1]) / 2;
        eval
        [
          assert.equal(profile.anchor[0], start[0]),
          assert.approx(mid, mid0 + distance, 0.0001)
        ];
      };
    },
    {
      name: "keeps one foot planted within a step";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 24;
        stride: 6;
        zoom: 0.05;

        // Both p values are within the first step (travel < stride).
        p0: 0.10;
        p1: 0.20;
        a: fn(start, {}, distance, stride, p0, zoom);
        b: fn(start, {}, distance, stride, p1, zoom);

        attachmentsY: (anchor, profile) =>
        {
          bodyDir: [math.Cos(profile.bodyAngle), math.Sin(profile.bodyAngle)];
          perpendicular: [-bodyDir[1], bodyDir[0]];
          spread: if profile.direction == "front" then 1
            else if profile.direction == "back" then 1
            else if profile.direction == "left" then 0
            else if profile.direction == "right" then 0
            else error("expected direction left|right|front|back");
          thighSpread: profile.thighWidth * spread;
          eval
          {
            left: anchor[1] + perpendicular[1] * thighSpread;
            right: anchor[1] - perpendicular[1] * thighSpread;
          };
        };

        attachA: attachmentsY(a.anchor, a);
        attachB: attachmentsY(b.anchor, b);
        rightFootA: attachA.right + a.rightLeg.end[1];
        rightFootB: attachB.right + b.rightLeg.end[1];

        eval [assert.approx(rightFootA, rightFootB, 0.0001)];
      };
    },
    {
      name: "supports negative distances";
      test: (fn) =>
      {
        start: [0, 0];
        distance: -12;
        stride: 6;
        zoom: 0.05;
        p: 0.5;
        profile0: fn(start, {}, distance, stride, 0, zoom);
        profile: fn(start, {}, distance, stride, p, zoom);
        mid0: (profile0.anchor[1] + profile0.leftLeg.end[1] + profile0.anchor[1] + profile0.rightLeg.end[1]) / 2;
        mid: (profile.anchor[1] + profile.leftLeg.end[1] + profile.anchor[1] + profile.rightLeg.end[1]) / 2;
        eval
        [
          assert.equal(profile.anchor[0], start[0]),
          assert.approx(mid, mid0 + distance * p, 0.0001)
        ];
      };
    },
    {
      name: "scales the initial stride from leg size";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 24;
        stride: 6;
        zoom: 0.05;
        s: 0.35;

        scaleVec: (v) => [v[0] * s, v[1] * s];
        scaleLimb: (limb) =>
        {
          end: scaleVec(limb.end);
          upper: limb.upper * s;
          lower: limb.lower * s;
          sign: limb.sign;
        };

        m:
        {
          height: defaultMeasurements.height * s;
          leftHand: scaleLimb(defaultMeasurements.leftHand);
          rightHand: scaleLimb(defaultMeasurements.rightHand);
          leftLeg: scaleLimb(defaultMeasurements.leftLeg);
          rightLeg: scaleLimb(defaultMeasurements.rightLeg);
          neckLength: defaultMeasurements.neckLength * s;
          headRadius: defaultMeasurements.headRadius * s;
          bodyAngle: defaultMeasurements.bodyAngle;
          neckAngle: defaultMeasurements.neckAngle;
          handPhaseOffset: defaultMeasurements.handPhaseOffset;
          shoulderWidth: defaultMeasurements.shoulderWidth * s;
          thighWidth: defaultMeasurements.thighWidth * s;
        };

        profile0: fn(start, m, distance, stride, 0, zoom);
        diff0: math.Abs(profile0.leftLeg.end[1] - profile0.rightLeg.end[1]);
        eval [assert.approx(diff0, math.Abs(stride) * s, 0.0001)];
      };
    }
    ,
    {
      name: "resets limbs to equal size at progress 1";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 24;
        stride: 6;
        zoom: 0.05;
        profile: fn(start, {}, distance, stride, 1, zoom);
        eval
        [
          assert.approx(profile.leftLeg.end[1], profile.rightLeg.end[1], 0.0001),
          assert.approx(profile.leftLeg.upper, profile.rightLeg.upper, 0.0001),
          assert.approx(profile.leftLeg.lower, profile.rightLeg.lower, 0.0001),
          assert.approx(profile.leftHand.end[1], profile.rightHand.end[1], 0.0001),
          assert.approx(profile.leftHand.upper, profile.rightHand.upper, 0.0001),
          assert.approx(profile.leftHand.lower, profile.rightHand.lower, 0.0001)
        ];
      };
    }
    ,
    {
      name: "keeps legs vertical and straight";
      test: (fn) =>
      {
        start: [0, 0];
        distance: -24;
        stride: 6;
        zoom: 0.05;
        profile0: fn(start, {}, distance, stride, 0, zoom);
        profile1: fn(start, {}, distance, stride, 1, zoom);
        eval
        [
          assert.equal(profile0.leftLeg.end[0], 0),
          assert.equal(profile0.rightLeg.end[0], 0),
          assert.approx(profile0.leftLeg.upper + profile0.leftLeg.lower, math.Abs(profile0.leftLeg.end[1]), 0.0001),
          assert.approx(profile0.rightLeg.upper + profile0.rightLeg.lower, math.Abs(profile0.rightLeg.end[1]), 0.0001),
          assert.equal(profile1.leftLeg.end[0], 0),
          assert.equal(profile1.rightLeg.end[0], 0),
          assert.approx(profile1.leftLeg.upper + profile1.leftLeg.lower, math.Abs(profile1.leftLeg.end[1]), 0.0001),
          assert.approx(profile1.rightLeg.upper + profile1.rightLeg.lower, math.Abs(profile1.rightLeg.end[1]), 0.0001)
        ];
      };
    }
]

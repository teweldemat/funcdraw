{
  eval [
    {
      name: "moves anchor linearly with progress";
      test: (fn) =>
      {
        start: [2, -1];
        distance: 12;
        stride: 6;
        zoom: 0.05;
        p: 0.25;
        profile: fn(start, {}, distance, stride, p, zoom);
        eval
        [
          assert.equal(profile.anchor[0], start[0]),
          assert.approx(profile.anchor[1], start[1] + distance * p, 0.0001)
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
        zoom: 0.05;
        profile: fn(start, {}, distance, stride, 1, zoom);
        eval
        [
          assert.equal(profile.anchor[0], start[0]),
          assert.approx(profile.anchor[1], start[1] + distance, 0.0001)
        ];
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
        profile: fn(start, {}, distance, stride, p, zoom);
        eval
        [
          assert.equal(profile.anchor[0], start[0]),
          assert.approx(profile.anchor[1], start[1] + distance * p, 0.0001)
        ];
      };
    },
    {
      name: "keeps steps symmetric by passing the fixed foot";
      test: (fn) =>
      {
        start: [0, 0];
        distance: 24;
        stride: 6;
        zoom: 0.05;
        strideAbs: math.Abs(stride);
        sign: math.Sign(distance);
        p0: 0;
        p1: strideAbs / math.Abs(distance);
        p2: 2 * strideAbs / math.Abs(distance);
        profile0: fn(start, {}, distance, stride, p0, zoom);
        profile1: fn(start, {}, distance, stride, p1, zoom);
        profile2: fn(start, {}, distance, stride, p2, zoom);

        left0: profile0.anchor[1] + profile0.leftLeg.end[1];
        right0: profile0.anchor[1] + profile0.rightLeg.end[1];
        left1: profile1.anchor[1] + profile1.leftLeg.end[1];
        right1: profile1.anchor[1] + profile1.rightLeg.end[1];
        left2: profile2.anchor[1] + profile2.leftLeg.end[1];
        right2: profile2.anchor[1] + profile2.rightLeg.end[1];

        eval
        [
          assert.approx(left0 - right0, -strideAbs * sign, 0.0001),
          assert.approx(left1 - right1, strideAbs * sign, 0.0001),
          assert.approx(left2 - right2, -strideAbs * sign, 0.0001)
        ];
      };
    }
  ];
}

{
  distance: (from, to) =>
  {
    dx: to[0] - from[0];
    dy: to[1] - from[1];
    eval math.Sqrt(dx * dx + dy * dy);
  };

  cross: (origin, target, joint) =>
  {
    toTarget: [target[0] - origin[0], target[1] - origin[1]];
    toJoint: [joint[0] - origin[0], joint[1] - origin[1]];
    eval toTarget[0] * toJoint[1] - toTarget[1] * toJoint[0];
  };

  widthOf: (points) =>
  {
    xs: [points[0][0], points[1][0], points[2][0], points[3][0], points[4][0]];
    minX: math.Min(xs[0], math.Min(xs[1], math.Min(xs[2], math.Min(xs[3], xs[4]))));
    maxX: math.Max(xs[0], math.Max(xs[1], math.Max(xs[2], math.Max(xs[3], xs[4]))));
    eval maxX - minX;
  };

  eval [
    {
      name: "merges default measurements and palette";
      test: (mod) =>
      {
        palette:
        {
          body: "#111111";
          limb: "#222222";
        };
        anchor: [1, 2];
        measurements: { rightHand: { end: [3, 1]; }; height: 8; };
        impl: mod.static;
        geometry: skeleton.build(anchor, measurements);
        headCenter:
        [
          geometry.neck.to[0] + geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
          geometry.neck.to[1] + geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
        ];
        result: impl(anchor, measurements, palette);
        eval
        [
          assert.equal(result[0].type, "line"),
          assert.equal(result[0].from[0], geometry.leftHandAttachment[0]),
          assert.equal(result[1].from[0], geometry.leftLegAttachment[0]),
          assert.equal(result[2].to[0], geometry.body.to[0]),
          assert.equal(result[2].to[1], geometry.body.to[1]),
          assert.equal(result[2].tag[0], anchor[0]),
          assert.equal(result[2].tag[1], anchor[1]),
          assert.equal(result[2].stroke, palette.body),
          assert.equal(geometry.measurements.bodyAngle, defaultMeasurements.bodyAngle),
          assert.equal(geometry.measurements.neckAngle, defaultMeasurements.neckAngle),

          assert.equal(result[3].from[0], geometry.neck.from[0]),
          assert.equal(result[3].to[1], geometry.neck.to[1]),

          assert.equal(result[4].center[0], headCenter[0]),
          assert.equal(result[4].center[1], headCenter[1]),
          assert.equal(result[4].radius, geometry.measurements.headRadius),
          assert.equal(result[4].stroke, palette.body),

          assert.equal(geometry.measurements.rightHand.end[0], measurements.rightHand.end[0]),
          assert.equal(geometry.measurements.rightHand.upper, defaultMeasurements.rightHand.upper),
          assert.equal(geometry.measurements.leftHand.sign, defaultMeasurements.leftHand.sign),
          assert.equal(result[5].to[0], geometry.leftHand.joint[0]),
          assert.equal(result[5].to[1], geometry.leftHand.joint[1]),
          assert.equal(result[6].to[0], geometry.leftHand.to[0]),
          assert.equal(result[6].to[1], geometry.leftHand.to[1]),
          assert.equal(result[7].to[0], geometry.rightHand.joint[0]),
          assert.equal(result[8].to[1], geometry.rightHand.to[1]),

          assert.equal(result[9].to[0], geometry.leftLeg.joint[0]),
          assert.equal(result[10].to[1], geometry.leftLeg.to[1]),
          assert.equal(result[11].to[0], geometry.rightLeg.joint[0]),
          assert.equal(result[12].to[1], geometry.rightLeg.to[1])
        ];
      };
    },
    {
      name: "uses limb lengths to solve joint positions";
      test: (mod) =>
      {
        palette:
        {
          body: "#aaaaaa";
          limb: "#bbbbbb";
        };
        anchor: [0, 0];
        measurements:
        {
          height: 12;
          leftLeg: { end: [-3, -6]; upper: 5; lower: 7; sign: -1; };
          rightHand: { end: [6, -4]; upper: 9; lower: 4; sign: -1; };
        };
        impl: mod.static;
        geometry: skeleton.build(anchor, measurements);
        primitives: impl(anchor, measurements, palette);
        leftUpper: distance(geometry.leftLeg.from, geometry.leftLeg.joint);
        leftLower: distance(geometry.leftLeg.joint, geometry.leftLeg.to);
        rightUpper: distance(geometry.rightHand.from, geometry.rightHand.joint);
        rightLower: distance(geometry.rightHand.joint, geometry.rightHand.to);
        eval
        [
          assert.approx(leftUpper, geometry.measurements.leftLeg.upper, 0.0001),
          assert.approx(leftLower, geometry.measurements.leftLeg.lower, 0.0001),
          assert.approx(rightUpper, geometry.measurements.rightHand.upper, 0.0001),
          assert.approx(rightLower, geometry.measurements.rightHand.lower, 0.0001),
          assert.less(cross(geometry.leftLeg.from, geometry.leftLeg.to, geometry.leftLeg.joint), 0),
          assert.less(cross(geometry.rightHand.from, geometry.rightHand.to, geometry.rightHand.joint), 0),
          assert.equal(primitives[5].from[0], geometry.leftHand.from[0]),
          assert.equal(primitives[6].to[0], geometry.leftHand.to[0]),
          assert.equal(primitives[8].to[1], geometry.rightHand.to[1]),
          assert.equal(primitives[11].to[0], geometry.rightLeg.joint[0])
        ];
      };
    },
    {
      name: "fills all missing measurements from defaults";
      test: (mod) =>
      {
        palette:
        {
          body: "#111111";
          limb: "#222222";
        };
        anchor: [2, 3];
        impl: mod.static;
        geometry: skeleton.build(anchor, {});
        primitives: impl(anchor, {}, palette);

        eval
        [
          assert.equal(geometry.measurements.height, defaultMeasurements.height),
          assert.equal(geometry.measurements.leftHand.end[0], defaultMeasurements.leftHand.end[0]),
          assert.equal(geometry.measurements.rightHand.end[1], defaultMeasurements.rightHand.end[1]),
          assert.equal(geometry.measurements.leftLeg.upper, defaultMeasurements.leftLeg.upper),
          assert.equal(geometry.measurements.rightLeg.lower, defaultMeasurements.rightLeg.lower),
          assert.equal(geometry.measurements.neckLength, defaultMeasurements.neckLength),
          assert.equal(geometry.measurements.headRadius, defaultMeasurements.headRadius),
          assert.equal(geometry.measurements.bodyAngle, defaultMeasurements.bodyAngle),
          assert.equal(geometry.measurements.neckAngle, defaultMeasurements.neckAngle),
          assert.equal(geometry.measurements.shoulderWidth, defaultMeasurements.shoulderWidth),
          assert.equal(geometry.measurements.thighWidth, defaultMeasurements.thighWidth),
          assert.equal(geometry.measurements.direction, defaultMeasurements.direction),

          assert.equal(primitives[0].from[0], geometry.leftHandAttachment[0]),
          assert.equal(primitives[1].from[0], geometry.leftLegAttachment[0]),
          assert.equal(primitives[2].to[0], geometry.body.to[0]),
          assert.equal(primitives[3].to[1], geometry.neck.to[1]),
          assert.equal(primitives[4].center[0], geometry.neck.to[0] + defaultMeasurements.headRadius * math.Cos(defaultMeasurements.neckAngle)),
          assert.equal(primitives[4].center[1], geometry.neck.to[1] + defaultMeasurements.headRadius * math.Sin(defaultMeasurements.neckAngle)),
          assert.equal(primitives[5].to[0], geometry.leftHand.joint[0]),
          assert.equal(primitives[6].to[0], geometry.leftHand.to[0]),
          assert.equal(primitives[7].to[0], geometry.rightHand.joint[0]),
          assert.equal(primitives[8].to[0], geometry.rightHand.to[0]),
          assert.equal(primitives[9].to[1], geometry.leftLeg.joint[1]),
          assert.equal(primitives[10].to[1], geometry.leftLeg.to[1]),
          assert.equal(primitives[11].to[1], geometry.rightLeg.joint[1]),
          assert.equal(primitives[12].to[1], geometry.rightLeg.to[1])
        ];
      };
    },
    {
      name: "applies partial overrides without clobbering other limbs";
      test: (mod) =>
      {
        palette:
        {
          body: "#333333";
          limb: "#444444";
        };
        anchor: [1, -1];
        measurements:
        {
          height: 14;
          leftHand: { end: [-6, 2]; upper: 9; };
          rightLeg: { end: [4, -7]; sign: -1; };
        };
        impl: mod.static;
        geometry: skeleton.build(anchor, measurements);
        primitives: impl(anchor, measurements, palette);

        eval
        [
          assert.equal(geometry.measurements.height, measurements.height),
          assert.equal(geometry.measurements.leftHand.upper, measurements.leftHand.upper),
          assert.equal(geometry.measurements.leftHand.lower, defaultMeasurements.leftHand.lower),
          assert.equal(geometry.measurements.rightHand.sign, defaultMeasurements.rightHand.sign),
          assert.equal(geometry.measurements.rightLeg.sign, measurements.rightLeg.sign),
          assert.equal(geometry.measurements.neckLength, defaultMeasurements.neckLength),
          assert.equal(geometry.measurements.headRadius, defaultMeasurements.headRadius),
          assert.equal(geometry.measurements.bodyAngle, defaultMeasurements.bodyAngle),
          assert.equal(geometry.measurements.neckAngle, defaultMeasurements.neckAngle),

          assert.equal(primitives[5].to[0], geometry.leftHand.joint[0]),
          assert.equal(primitives[6].to[0], geometry.leftHand.to[0]),
          assert.equal(primitives[7].to[0], geometry.rightHand.joint[0]),
          assert.equal(primitives[8].to[0], geometry.rightHand.to[0]),
          assert.equal(primitives[9].to[1], geometry.leftLeg.joint[1]),
          assert.equal(primitives[10].to[1], geometry.leftLeg.to[1]),
          assert.equal(primitives[11].to[0], geometry.rightLeg.joint[0]),
          assert.equal(primitives[12].to[0], geometry.rightLeg.to[0])
        ];
      };
    },
    {
      name: "exposes skins collection for rendering skeleton output";
      test: (mod) =>
      {
        palette:
        {
          body: "#161616";
          limb: "#282828";
        };
        anchor: [0, 0];
        geometry: mod.skeleton.build(anchor, {});
        primitives: mod.skins.stick(geometry, palette);

        eval
        [
          assert.equal(primitives[2].tag[0], anchor[0]),
          assert.equal(primitives[2].stroke, palette.body),
          assert.equal(primitives[5].stroke, palette.limb),
          assert.equal(primitives[10].stroke, palette.limb),
          assert.equal(primitives[13].type, "circle"),
          assert.equal(primitives[14].type, "circle")
        ];
      };
    },
    {
      name: "poly skin uses polygonal shapes with directional profile";
      test: (mod) =>
      {
        palette:
        {
          body: "#f1f5f9";
          limb: "#0f172a";
        };
        anchor: [0, 0];
        frontGeometry: mod.skeleton.build(anchor, {});
        leftGeometry: mod.skeleton.build(anchor, { direction: "left"; });
        front: mod.skins.poly(frontGeometry, palette);
        left: mod.skins.poly(leftGeometry, palette);
        frontHead: front[2];
        leftHead: left[4];
        frontHeadWidth: widthOf(frontHead.points);
        leftHeadWidth: widthOf(leftHead.points);

        eval
        [
          assert.equal(front[0].name, "body"),
          assert.equal(front[0].type, "polygon"),
          assert.equal(front[0].points[0][0], frontGeometry.leftHandAttachment[0]),
          assert.equal(front[0].points[2][0], frontGeometry.rightLegAttachment[0]),
          assert.equal(frontHead.name, "head"),
          assert.greater(frontHeadWidth, leftHeadWidth),
          assert.equal(front[3].type, "polygon"),
          assert.equal(front[6].fill, palette.limb),
          assert.equal(left[5].type, "polygon")
        ];
      };
    },
    {
      name: "orders hands around the body based on facing direction";
      test: (mod) =>
      {
        palette:
        {
          body: "#121212";
          limb: "#232323";
        };
        anchor: [0, 0];
        facingLeft: mod.skeleton.build(anchor, { direction: "left"; });
        facingRight: mod.skeleton.build(anchor, { direction: "right"; });
        leftPrimitives: mod.skins.stick(facingLeft, palette);
        rightPrimitives: mod.skins.stick(facingRight, palette);

        eval
        [
          assert.equal(leftPrimitives[0].from[0], facingLeft.rightHand.from[0]),
          assert.equal(leftPrimitives[6].name, "body"),
          assert.equal(leftPrimitives[9].from[0], facingLeft.leftHand.from[0]),

          assert.equal(rightPrimitives[0].from[0], facingRight.leftHand.from[0]),
          assert.equal(rightPrimitives[6].name, "body"),
          assert.equal(rightPrimitives[9].from[0], facingRight.rightHand.from[0])
        ];
      };
    },
    {
      name: "adjusts limb attachment spread based on direction";
      test: (mod) =>
      {
        anchor: [0, 0];
        front: mod.skeleton.build(anchor, {});
        back: mod.skeleton.build(anchor, { direction: "back"; });
        left: mod.skeleton.build(anchor, { direction: "left"; });
        right: mod.skeleton.build(anchor, { direction: "right"; });

        eval
        [
          assert.equal(math.Abs(front.rightHandAttachment[0] - front.leftHandAttachment[0]), defaultMeasurements.shoulderWidth * 2),
          assert.equal(math.Abs(front.rightLegAttachment[0] - front.leftLegAttachment[0]), defaultMeasurements.thighWidth * 2),
          assert.equal(math.Abs(back.rightHandAttachment[0] - back.leftHandAttachment[0]), defaultMeasurements.shoulderWidth * 2),
          assert.equal(math.Abs(back.rightLegAttachment[0] - back.leftLegAttachment[0]), defaultMeasurements.thighWidth * 2),
          assert.equal(left.leftHandAttachment[0], left.rightHandAttachment[0]),
          assert.equal(left.leftLegAttachment[0], left.rightLegAttachment[0]),
          assert.equal(right.leftHandAttachment[0], right.rightHandAttachment[0]),
          assert.equal(right.leftLegAttachment[0], right.rightLegAttachment[0])
        ];
      };
    },
    {
      name: "positions eyes according to head direction";
      test: (mod) =>
      {
        palette:
        {
          body: "#191919";
          limb: "#2a2a2a";
        };
        anchor: [0, 0];
        frontGeometry: mod.skeleton.build(anchor, {});
        headCenter:
        [
          frontGeometry.neck.to[0] + frontGeometry.measurements.headRadius * math.Cos(frontGeometry.measurements.neckAngle),
          frontGeometry.neck.to[1] + frontGeometry.measurements.headRadius * math.Sin(frontGeometry.measurements.neckAngle)
        ];
        front: mod.skins.stick(frontGeometry, palette);
        right: mod.skins.stick(mod.skeleton.build(anchor, { direction: "right"; }), palette);
        back: mod.skins.stick(mod.skeleton.build(anchor, { direction: "back"; }), palette);

        eval
        [
          assert.greater(front[14].center[0], headCenter[0]),
          assert.greater(right[13].center[0], headCenter[0]),
          assert.equal(back[13].type, "line")
        ];
      };
    }
  ];
}

{
  eval [
    {
      name: "merges default measurements and palette";
      test: (fn) =>
      {
        palette:
        {
          body: "#111111";
          limb: "#222222";
        };
        anchor: [1, 2];
        measurements: { rightHand: [3, 1]; height: 8; };
        geometry: skeleton.build(anchor, measurements);
        headCenter:
        [
          geometry.neck.to[0] + geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
          geometry.neck.to[1] + geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
        ];
        result: fn(anchor, measurements, palette);
        eval
        [
          assert.equal(result[0].type, "line"),
          assert.equal(result[0].to[0], geometry.body.to[0]),
          assert.equal(result[0].to[1], geometry.body.to[1]),
          assert.equal(result[0].stroke, palette.body),
          assert.equal(result[0].width, 0.35),
          assert.equal(geometry.measurements.bodyAngle, defaultMeasurements.bodyAngle),
          assert.equal(geometry.measurements.neckAngle, defaultMeasurements.neckAngle),

          assert.equal(result[1].from[0], geometry.neck.from[0]),
          assert.equal(result[1].to[1], geometry.neck.to[1]),
          assert.equal(result[1].stroke, palette.body),
          assert.equal(result[1].width, 0.25),

          assert.equal(result[2].center[0], headCenter[0]),
          assert.equal(result[2].center[1], headCenter[1]),
          assert.equal(result[2].radius, geometry.measurements.headRadius),
          assert.equal(result[2].stroke, palette.body),
          assert.equal(result[2].width, 0.25),

          assert.equal(result[3].from[0], geometry.leftHand.from[0]),
          assert.equal(result[3].from[1], geometry.leftHand.from[1]),
          assert.equal(result[3].to[0], geometry.leftHand.to[0]),
          assert.equal(result[3].to[1], geometry.leftHand.to[1]),
          assert.equal(result[3].stroke, palette.limb),

          assert.equal(result[4].from[0], geometry.rightHand.from[0]),
          assert.equal(result[4].from[1], geometry.rightHand.from[1]),
          assert.equal(result[4].to[0], geometry.rightHand.to[0]),
          assert.equal(result[4].to[1], geometry.rightHand.to[1]),
          assert.equal(result[4].stroke, palette.limb),

          assert.equal(result[5].to[0], geometry.leftLeg.to[0]),
          assert.equal(result[5].to[1], geometry.leftLeg.to[1]),
          assert.equal(result[5].stroke, palette.limb),

          assert.equal(result[6].to[0], geometry.rightLeg.to[0]),
          assert.equal(result[6].to[1], geometry.rightLeg.to[1]),
          assert.equal(result[6].stroke, palette.limb)
        ];
      };
    },
    {
      name: "exposes skeleton geometry used for rendering";
      test: (fn) =>
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
          leftLeg: [-3, -6];
        };
        geometry: skeleton.build(anchor, measurements);
        headCenter:
        [
          geometry.neck.to[0] + geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
          geometry.neck.to[1] + geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
        ];
        primitives: fn(anchor, measurements, palette);

        eval
        [
          assert.equal(geometry.measurements.rightHand[0], defaultMeasurements.rightHand[0]),
          assert.equal(geometry.body.to[1], geometry.measurements.height),
          assert.equal(geometry.neck.to[1], geometry.body.to[1] + geometry.measurements.neckLength),
          assert.equal(geometry.leftHand.from[0], geometry.body.to[0]),
          assert.equal(geometry.leftLeg.to[0], anchor[0] + geometry.measurements.leftLeg[0]),

          assert.equal(primitives[0].width, 0.35),
          assert.equal(primitives[1].to[1], geometry.neck.to[1]),
          assert.equal(primitives[2].center[1], headCenter[1]),
          assert.equal(primitives[3].to[1], geometry.leftHand.to[1]),
          assert.equal(primitives[6].to[0], geometry.rightLeg.to[0])
        ];
      };
    },
    {
      name: "fills all missing measurements from defaults";
      test: (fn) =>
      {
        palette:
        {
          body: "#111111";
          limb: "#222222";
        };
        anchor: [2, 3];
        geometry: skeleton.build(anchor, {});
        primitives: fn(anchor, {}, palette);

        eval
        [
          assert.equal(geometry.measurements.height, defaultMeasurements.height),
          assert.equal(geometry.measurements.leftHand[0], defaultMeasurements.leftHand[0]),
          assert.equal(geometry.measurements.rightHand[1], defaultMeasurements.rightHand[1]),
          assert.equal(geometry.measurements.leftLeg[0], defaultMeasurements.leftLeg[0]),
          assert.equal(geometry.measurements.rightLeg[1], defaultMeasurements.rightLeg[1]),
          assert.equal(geometry.measurements.neckLength, defaultMeasurements.neckLength),
          assert.equal(geometry.measurements.headRadius, defaultMeasurements.headRadius),
          assert.equal(geometry.measurements.bodyAngle, defaultMeasurements.bodyAngle),
          assert.equal(geometry.measurements.neckAngle, defaultMeasurements.neckAngle),

          assert.equal(primitives[0].to[0], geometry.body.to[0]),
          assert.equal(primitives[0].to[1], geometry.body.to[1]),
          assert.equal(primitives[1].to[0], geometry.neck.to[0]),
          assert.equal(primitives[1].to[1], geometry.neck.to[1]),
          assert.equal(primitives[2].center[0], geometry.neck.to[0] + defaultMeasurements.headRadius * math.Cos(defaultMeasurements.neckAngle)),
          assert.equal(primitives[2].center[1], geometry.neck.to[1] + defaultMeasurements.headRadius * math.Sin(defaultMeasurements.neckAngle)),
          assert.equal(primitives[3].to[0], geometry.leftHand.to[0]),
          assert.equal(primitives[3].to[1], geometry.leftHand.to[1]),
          assert.equal(primitives[4].to[0], geometry.rightHand.to[0]),
          assert.equal(primitives[4].to[1], geometry.rightHand.to[1]),
          assert.equal(primitives[5].to[0], geometry.leftLeg.to[0]),
          assert.equal(primitives[5].to[1], geometry.leftLeg.to[1]),
          assert.equal(primitives[6].to[0], geometry.rightLeg.to[0]),
          assert.equal(primitives[6].to[1], geometry.rightLeg.to[1])
        ];
      };
    },
    {
      name: "applies partial overrides without clobbering other limbs";
      test: (fn) =>
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
          leftHand: [-6, 2];
          rightLeg: [4, -7];
        };
        geometry: skeleton.build(anchor, measurements);
        primitives: fn(anchor, measurements, palette);

        eval
        [
          assert.equal(geometry.measurements.height, measurements.height),
          assert.equal(geometry.measurements.leftHand[0], measurements.leftHand[0]),
          assert.equal(geometry.measurements.rightHand[0], defaultMeasurements.rightHand[0]),
          assert.equal(geometry.measurements.leftLeg[1], defaultMeasurements.leftLeg[1]),
          assert.equal(geometry.measurements.rightLeg[1], measurements.rightLeg[1]),
          assert.equal(geometry.measurements.neckLength, defaultMeasurements.neckLength),
          assert.equal(geometry.measurements.headRadius, defaultMeasurements.headRadius),
          assert.equal(geometry.measurements.bodyAngle, defaultMeasurements.bodyAngle),
          assert.equal(geometry.measurements.neckAngle, defaultMeasurements.neckAngle),

          assert.equal(primitives[3].to[0], geometry.leftHand.to[0]),
          assert.equal(primitives[3].to[1], geometry.leftHand.to[1]),
          assert.equal(primitives[4].to[0], geometry.rightHand.to[0]),
          assert.equal(primitives[4].to[1], geometry.rightHand.to[1]),
          assert.equal(primitives[5].to[0], geometry.leftLeg.to[0]),
          assert.equal(primitives[5].to[1], geometry.leftLeg.to[1]),
          assert.equal(primitives[6].to[0], geometry.rightLeg.to[0]),
          assert.equal(primitives[6].to[1], geometry.rightLeg.to[1])
        ];
      };
    }
  ];
}

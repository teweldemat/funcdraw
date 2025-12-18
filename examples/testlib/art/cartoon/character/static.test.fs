[
    {
      name: "exposes static character with jointed limbs";
      test: (fn) =>
      {
        palette:
        {
          body: "#444444";
          limb: "#555555";
        };
        anchor: [0, 0];
        result: fn(anchor, {}, palette);

        eval
        [
          assert.equal(result[0].type, "line"),
          assert.equal(result[4].type, "circle"),
          assert.equal(result[5].to[0], result[6].from[0]),
          assert.equal(result[5].to[1], result[6].from[1]),
          assert.equal(result[12].type, "line")
        ];
      };
    },
    {
      name: "accepts custom skin expression";
      test: (fn) =>
      {
        palette:
        {
          body: "#121212";
          limb: "#565656";
        };
        anchor: [2, -3];
        skin: (geometry, skinPalette) =>
        {
          eval
          [
            {
              type: "skinCheck";
              anchor: geometry.anchor;
              stroke: skinPalette.body;
            }
          ];
        };
        result: fn(anchor, {}, palette, skin);

        eval
        [
          assert.equal(result[0].type, "skinCheck"),
          assert.equal(result[0].anchor[0], anchor[0]),
          assert.equal(result[0].anchor[1], anchor[1]),
          assert.equal(result[0].stroke, palette.body)
        ];
      };
    },
    {
      name: "renders eyes based on head direction";
      test: (fn) =>
      {
        palette:
        {
          body: "#101010";
          limb: "#202020";
        };
        anchor: [0, 0];
        geometry: skeleton.build(anchor, {});
        headCenter:
        [
          geometry.neck.to[0] + geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
          geometry.neck.to[1] + geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
        ];
        front: fn(anchor, {}, palette);
        left: fn(anchor, { direction: "left"; }, palette);
        back: fn(anchor, { direction: "back"; }, palette);

        eval
        [
          assert.equal(front[13].type, "circle"),
          assert.equal(front[14].type, "circle"),
          assert.less(front[13].center[0], headCenter[0]),
          assert.greater(front[14].center[0], headCenter[0]),

          assert.equal(left[13].type, "circle"),
          assert.less(left[13].center[0], headCenter[0]),

          assert.equal(back[13].type, "line")
        ];
      };
    }
]

{
  eval [
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
          assert.equal(result[2].type, "circle"),
          assert.equal(result[3].to[0], result[4].from[0]),
          assert.equal(result[3].to[1], result[4].from[1]),
          assert.equal(result[10].type, "line")
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
    }
  ];
}

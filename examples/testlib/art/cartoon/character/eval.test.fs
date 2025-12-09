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
        result: fn(anchor, { rightHand: [3, 1]; height: 8; }, palette);
        eval
        [
          assert.equal(result[0].type, "line"),
          assert.equal(result[0].to[0], anchor[0]),
          assert.equal(result[0].to[1], anchor[1] + 8),
          assert.equal(result[0].stroke, palette.body),

          assert.equal(result[1].from[0], anchor[0]),
          assert.equal(result[1].from[1], anchor[1] + 8),
          assert.equal(result[1].to[0], anchor[0] - 4),
          assert.equal(result[1].to[1], anchor[1] + 8),
          assert.equal(result[1].stroke, palette.limb),

          assert.equal(result[2].from[0], anchor[0]),
          assert.equal(result[2].from[1], anchor[1] + 8),
          assert.equal(result[2].to[0], anchor[0] + 3),
          assert.equal(result[2].to[1], anchor[1] + 9),
          assert.equal(result[2].stroke, palette.limb),

          assert.equal(result[3].to[0], anchor[0] - 2),
          assert.equal(result[3].to[1], anchor[1] - 4),
          assert.equal(result[3].stroke, palette.limb),

          assert.equal(result[4].to[0], anchor[0] + 2),
          assert.equal(result[4].to[1], anchor[1] - 4),
          assert.equal(result[4].stroke, palette.limb)
        ];
      };
    }
  ];
}

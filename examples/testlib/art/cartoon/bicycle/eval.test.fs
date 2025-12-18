[
  {
    name: "returns graphics and geometry metadata";
    test: (fn) =>
    {
      rear: [10, -4];
      r: 9;
      angle: 1.25;
      bike: fn(rear, r, angle, "#111111", "#222222");
      eval
      [
        assert.equal(bike.leftWheelCenter[0], rear[0]),
        assert.equal(bike.leftWheelCenter[1], rear[1]),
        assert.approx(bike.rightWheelCenter[0] - bike.leftWheelCenter[0], bike.wheelBase, 0.0001),
        assert.equal(bike.wheelRadius, r),
        assert.equal(bike.wheelAngle, angle),
        assert.equal(bike.graphics[0] == null, false),
        assert.equal(bike.pedalOrbitRadius == null, false),
        assert.equal(bike.attachments.seat == null, false),
        assert.equal(bike.attachments.pedals.a == null, false),
        assert.equal(bike.attachments.pedals.b == null, false),
        assert.equal(bike.attachments.pedals.near == null, false),
        assert.equal(bike.attachments.pedals.far == null, false),
        assert.equal(bike.attachments.handlebar.from == null, false),
        assert.equal(bike.attachments.handlebar.to == null, false)
      ];
    };
  }
]

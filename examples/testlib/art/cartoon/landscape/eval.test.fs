{
  countNamed: (graphics, name) => Len(graphics filter (g) => g.name == name);

  eval [
    {
      name: "sun draws a circle and rays";
      test: (mod) =>
      {
        graphics:
          mod.sun(
            {
              center: [0, 0];
              radius: 5;
              fill: "#fde047";
              stroke: "#f59e0b";
              width: 0.25;
              rayCount: 12;
              rayLength: 3;
              rayColor: "#fde047";
              rayWidth: 0.2;
              rotation: 0;
            });
        sun: First(graphics, (g) => g.name == "sun");
        eval
        [
          assert.equal(sun.type, "circle"),
          assert.equal(countNamed(graphics, "sun-ray"), 12)
        ];
      };
    },
    {
      name: "cloud draws a cluster of circles";
      test: (mod) =>
      {
        graphics:
          mod.cloud(
            {
              center: [0, 0];
              width: 20;
              height: 10;
              fill: "#e2e8f0";
              stroke: "#94a3b8";
              strokeWidth: 0.2;
            });
        eval
        [
          assert.equal(Len(graphics), 7),
          assert.equal(Len(graphics filter (g) => g.type == "circle"), 7),
          assert.equal(First(graphics, (g) => g.name == "cloud").fill, "#e2e8f0")
        ];
      };
    }
  ];
}

{
  xWidth: (points) =>
  {
    xs: [points[0][0], points[1][0], points[2][0], points[3][0]];
    minX: math.Min(xs[0], math.Min(xs[1], math.Min(xs[2], xs[3])));
    maxX: math.Max(xs[0], math.Max(xs[1], math.Max(xs[2], xs[3])));
    eval maxX - minX;
  };

  countNamed: (graphics, name) => Len(graphics filter (g) => g.name == name);

  eval [
    {
      name: "cottage draws windows per story and supports door open";
      test: (mod) =>
      {
        light: "#fef9c3";
        closed:
          mod.types.cottage(
            {
              anchor: [0, 0];
              width: 20;
              stories: 2;
              doorOpen: 0;
              lightColor: light;
            });
        open:
          mod.types.cottage(
            {
              anchor: [0, 0];
              width: 20;
              stories: 2;
              doorOpen: 0.7;
              lightColor: light;
            });

        closedDoor: First(closed, (g) => g.name == "door");
        openDoor: First(open, (g) => g.name == "door");
        opening: First(open, (g) => g.name == "door-opening");

        eval
        [
          assert.equal(countNamed(closed, "window"), 4),
          assert.equal(opening.fill, light),
          assert.less(xWidth(openDoor.points), xWidth(closedDoor.points))
        ];
      };
    },
    {
      name: "townhouse windows scale with stories";
      test: (mod) =>
      {
        graphics:
          mod.types.townhouse(
            {
              anchor: [0, 0];
              width: 18;
              stories: 3;
              doorOpen: 0.3;
              lightColor: "#a7f3d0";
            });
        eval [assert.equal(countNamed(graphics, "window"), 9)];
      };
    },
    {
      name: "igloo draws a dome and a lit window";
      test: (mod) =>
      {
        light: "#93c5fd";
        graphics:
          mod.types.igloo(
            {
              anchor: [0, 0];
              width: 22;
              stories: 99;
              doorOpen: 0.5;
              lightColor: light;
            });
        dome: First(graphics, (g) => g.name == "dome");
        window: First(graphics, (g) => g.name == "window");
        eval
        [
          assert.equal(dome.type, "circle"),
          assert.equal(window.fill, light)
        ];
      };
    }
  ];
}

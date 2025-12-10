{
  // Minimal repro: pull Pi/2 from @funcdraw/testlib/art/bugexp.
  // JS player should give ~1.5708; .NET currently faults on packaged Pi/2.
  eval
  {
    view: { left: -2; bottom: -2; right: 2; top: 2; };
    angle: package("@funcdraw/testlib").bugexp.angle;
    graphics:
    {
      type: "line";
      from: [0, 0];
      to: [angle, 0];
      stroke: "#ffffff";
      width: 0.2;
    };
  };
}

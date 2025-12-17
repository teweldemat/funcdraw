{
  transport: package("@funcdraw/testlib").cartoon.transport;
  character: package("@funcdraw/testlib").cartoon.character;

  busAnchor: [-46, -10];
  busSize: [92, 30];

  doorOpen: (math.Sin(t * 0.8) + 1) / 2;

  busGraphics:
    transport.bus(
      {
        anchor: busAnchor;
        size: busSize;
        doorOpen: doorOpen;
        fill: "#f97316";
        stroke: "#0f172a";
        width: 0.35;
      });

  busGlass:
    busGraphics filter (g) =>
      g.name == "bus-window-glass" or g.name == "bus-windshield-glass";

  busCore:
    busGraphics filter (g) =>
      g.name != "bus-window-glass" and g.name != "bus-windshield-glass";

  windshield: First(busGraphics, (g) => g.name == "bus-windshield");

  driverPalette:
  {
    body: "#38bdf8";
    limb: "#bd8c31";
  };

  driverBase: character.static([0, 0], { direction: "right"; }, driverPalette, character.skins.poly);
  driverUpper:
    driverBase filter (g) =>
    {
      n: g?.name;
      eval n == "head" or n == "neck" or n == "body" or n == "head-back-mark";
    };

  driverBbox0: fd.boundingbox(driverUpper);
  driverTargetW: windshield.size[0] * 0.92;
  driverTargetH: windshield.size[1] * 0.92;
  driverScale: math.Min(driverTargetW / driverBbox0.width, driverTargetH / driverBbox0.height);
  driverScaled: fd.scale(driverUpper, [0, 0], driverScale, driverScale);
  driverBbox: fd.boundingbox(driverScaled);

  driverTargetTop: windshield.position[1] + windshield.size[1] - windshield.size[1] * 0.08;
  driverTargetCenterX: windshield.position[0] + windshield.size[0] * 0.46;

  driverDx: driverTargetCenterX - (driverBbox.left + driverBbox.width / 2);
  driverDy: driverTargetTop - (driverBbox.bottom + driverBbox.height);

  driver: fd.translate(driverScaled, driverDx, driverDy);

  eval
  {
    valueHooks: { t: t };
    view:
    {
      left: -60;
      bottom: -25;
      right: 60;
      top: 35;
    };
    graphics: busCore + driver + busGlass;
  };
}

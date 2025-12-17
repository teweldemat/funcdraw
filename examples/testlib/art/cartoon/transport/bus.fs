(options) =>
{
  anchor: options.anchor;
  size: options.size;
  doorOpen: options.doorOpen;
  fill: options.fill;
  stroke: options.stroke;
  width: options.width;

  w: size[0];
  h: size[1];

  wheelRadius: h * 0.18;
  wheelY: anchor[1] - wheelRadius * 0.2;
  wheelStroke: "#0f172a";
  wheelWidth: width;

  body:
  {
    type: "rect";
    name: "bus-body";
    position: anchor;
    size;
    fill;
    stroke;
    width;
  };

  wheel: (cx) =>
  {
    type: "circle";
    name: "bus-wheel";
    center: [cx, wheelY];
    radius: wheelRadius;
    fill: "#1e293b";
    stroke: wheelStroke;
    width: wheelWidth;
  };

  wheelHub: (cx) =>
  {
    type: "circle";
    name: "bus-wheel-hub";
    center: [cx, wheelY];
    radius: wheelRadius * 0.42;
    fill: "#94a3b8";
    stroke: wheelStroke;
    width: wheelWidth * 0.55;
  };

  wheelInset: w * 0.18;
  wheels:
  [
    wheel(anchor[0] + wheelInset),
    wheel(anchor[0] + w - wheelInset),
    wheelHub(anchor[0] + wheelInset),
    wheelHub(anchor[0] + w - wheelInset)
  ];

  doorWidth: w * 0.14;
  doorHeight: h * 0.72;
  doorPos: [anchor[0] + w * 0.08, anchor[1] + h * 0.02];

  doorFrame:
  {
    type: "rect";
    name: "bus-door-opening";
    position: doorPos;
    size: [doorWidth, doorHeight];
    fill: "#0f172a";
    stroke: "none";
    width: 0;
  };

  interiorPos: [doorPos[0] + doorWidth * 0.12, doorPos[1] + doorHeight * 0.06];
  interiorSize: [doorWidth * 0.76, doorHeight * 0.9];
  doorInterior:
  {
    type: "rect";
    name: "bus-door-interior";
    position: interiorPos;
    size: interiorSize;
    fill: "#e2e8f0";
    stroke: "none";
    width: 0;
  };

  floorH: interiorSize[1] * 0.22;
  floor:
  {
    type: "rect";
    name: "bus-door-floor";
    position: interiorPos;
    size: [interiorSize[0], floorH];
    fill: "#cbd5e1";
    stroke: "none";
    width: 0;
  };

  stepW: interiorSize[0] * 0.88;
  stepX: interiorPos[0] + interiorSize[0] * 0.06;
  stepH: floorH * 0.34;
  step1:
  {
    type: "rect";
    name: "bus-door-step";
    position: [stepX, interiorPos[1] + floorH * 0.1];
    size: [stepW, stepH];
    fill: fd.color.alpha("#0f172a", 0.12);
    stroke: "none";
    width: 0;
  };
  step2:
  {
    type: "rect";
    name: "bus-door-step";
    position: [stepX, interiorPos[1] + floorH * 0.52];
    size: [stepW, stepH];
    fill: fd.color.alpha("#0f172a", 0.08);
    stroke: "none";
    width: 0;
  };

  poleW: doorWidth * 0.06;
  pole:
  {
    type: "rect";
    name: "bus-door-pole";
    position: [interiorPos[0] + interiorSize[0] - poleW * 1.2, interiorPos[1] + floorH * 0.9];
    size: [poleW, interiorSize[1] - floorH * 1.1];
    fill: stroke;
    stroke: "none";
    width: 0;
  };

  doorSlide: doorWidth * 0.92 * doorOpen;
  door:
  {
    type: "rect";
    name: "bus-door";
    position: [doorPos[0] + doorSlide, doorPos[1]];
    size: [doorWidth, doorHeight];
    fill;
    stroke;
    width;
  };

  windowCount: 6;
  windowGap: w * 0.02;
  windowW: (w * 0.58 - (windowCount - 1) * windowGap) / windowCount;
  windowH: h * 0.28;
  windowY: anchor[1] + h * 0.58;
  windowStartX: anchor[0] + w * 0.32;

  windowHole: (i) =>
  {
    x: windowStartX + i * (windowW + windowGap);
    eval
    {
      type: "rect";
      name: "bus-window";
      index: i;
      position: [x, windowY];
      size: [windowW, windowH];
      fill: "#0f172a";
      stroke: "none";
      width: 0;
    };
  };

  windowGlass: (i) =>
  {
    x: windowStartX + i * (windowW + windowGap);
    glassStrokeWidth: width * 0.6;
    eval
    {
      type: "rect";
      name: "bus-window-glass";
      index: i;
      position: [x, windowY];
      size: [windowW, windowH];
      fill: fd.color.alpha("#93c5fd", 0.28);
      stroke: fd.color.alpha("#e2e8f0", 0.35);
      width: glassStrokeWidth;
      blendMode: "multiply";
      opacity: 0.9;
    };
  };

  windows: Range(0, windowCount) map (k, idx) => windowHole(k);
  glass: Range(0, windowCount) map (k, idx) => windowGlass(k);

  windshieldInsetX: w * 0.02;
  windshieldW: w * 0.08;
  windshieldH: h * 0.38;
  windshieldX: anchor[0] + w - windshieldInsetX - windshieldW;
  windshieldY: anchor[1] + h * 0.54;

  windshield:
  {
    type: "rect";
    name: "bus-windshield";
    position: [windshieldX, windshieldY];
    size: [windshieldW, windshieldH];
    fill: "#0f172a";
    stroke: "none";
    width: 0;
  };

  windshieldGlassStrokeWidth: width * 0.6;
  windshieldGlass:
  {
    type: "rect";
    name: "bus-windshield-glass";
    position: [windshieldX, windshieldY];
    size: [windshieldW, windshieldH];
    fill: fd.color.alpha("#93c5fd", 0.28);
    stroke: fd.color.alpha("#e2e8f0", 0.35);
    width: windshieldGlassStrokeWidth;
    blendMode: "multiply";
    opacity: 0.9;
  };

  frontCapW: w * 0.06;
  frontCapX: anchor[0] + w - frontCapW;
  frontCap:
  {
    type: "rect";
    name: "bus-front-cap";
    position: [frontCapX, anchor[1] + h * 0.02];
    size: [frontCapW, h * 0.96];
    fill: fd.color.alpha("#0f172a", 0.09);
    stroke: "none";
    width: 0;
  };

  bumperH: h * 0.24;
  bumper:
  {
    type: "rect";
    name: "bus-front-bumper";
    position: [frontCapX, anchor[1] + h * 0.02];
    size: [frontCapW, bumperH];
    fill: fd.color.alpha("#0f172a", 0.18);
    stroke: "none";
    width: 0;
  };

  frontLightStrokeWidth: width * 0.6;
  frontLight:
  {
    type: "circle";
    name: "bus-light";
    center: [anchor[0] + w - w * 0.06, anchor[1] + h * 0.18];
    radius: h * 0.06;
    fill: "#fde047";
    stroke: "#f59e0b";
    width: frontLightStrokeWidth;
  };

  tailLightStrokeWidth: width * 0.6;
  tailLight:
  {
    type: "circle";
    name: "bus-tail-light";
    center: [anchor[0] + w * 0.045, anchor[1] + h * 0.18];
    radius: h * 0.05;
    fill: "#fb7185";
    stroke: "#be123c";
    width: tailLightStrokeWidth;
  };

  stripeH: h * 0.12;
  stripeY: anchor[1] + h * 0.34;
  stripe:
  {
    type: "rect";
    name: "bus-stripe";
    position: [anchor[0] + w * 0.02, stripeY];
    size: [w * 0.96, stripeH];
    fill: fd.color.alpha("#0f172a", 0.12);
    stroke: "none";
    width: 0;
  };

  doorInteriorGraphics: [doorFrame, doorInterior, floor, step1, step2, pole];

  eval wheels
    + [body, stripe]
    + windows
    + [windshield, frontCap, bumper]
    + doorInteriorGraphics
    + [door, frontLight, tailLight]
    + glass
    + [windshieldGlass];
}

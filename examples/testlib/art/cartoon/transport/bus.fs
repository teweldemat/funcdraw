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

  wheelInset: w * 0.18;
  wheels:
  [
    wheel(anchor[0] + wheelInset),
    wheel(anchor[0] + w - wheelInset)
  ];

  doorWidth: w * 0.14;
  doorHeight: h * 0.72;
  doorPos: [anchor[0] + w * 0.08, anchor[1] + h * 0.02];

  doorOpening:
  {
    type: "rect";
    name: "bus-door-opening";
    position: doorPos;
    size: [doorWidth, doorHeight];
    fill: "#0f172a";
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
    eval
    {
      type: "rect";
      name: "bus-window-glass";
      index: i;
      position: [x, windowY];
      size: [windowW, windowH];
      fill: fd.color.alpha("#93c5fd", 0.28);
      stroke: fd.color.alpha("#e2e8f0", 0.35);
      width: width * 0.6;
      blendMode: "multiply";
      opacity: 0.9;
    };
  };

  windows: Range(0, windowCount) map (k, idx) => windowHole(k);
  glass: Range(0, windowCount) map (k, idx) => windowGlass(k);

  frontLight:
  {
    type: "circle";
    name: "bus-light";
    center: [anchor[0] + w - w * 0.06, anchor[1] + h * 0.18];
    radius: h * 0.06;
    fill: "#fde047";
    stroke: "#f59e0b";
    width: width * 0.6;
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

  eval wheels + [body, stripe] + windows + [doorOpening, door, frontLight] + glass;
}


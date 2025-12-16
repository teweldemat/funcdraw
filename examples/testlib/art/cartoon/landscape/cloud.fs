(options) =>
{
  center: options.center;
  cloudWidth: options.width;
  cloudHeight: options.height;
  fill: options.fill;
  stroke: options.stroke;
  strokeWidth: options.strokeWidth;

  eval if cloudWidth <= 0 then error("expected width > 0") else
  if cloudHeight <= 0 then error("expected height > 0") else
  {
    x: center[0];
    y: center[1];
    w: cloudWidth;
    h: cloudHeight;

    rSmall: h * 0.28;
    rMid: h * 0.32;
    rBig: h * 0.38;

    circles:
    [
      { type: "circle"; name: "cloud"; center: [x - w * 0.28, y]; radius: rMid; fill; stroke; width: strokeWidth; },
      { type: "circle"; name: "cloud"; center: [x - w * 0.08, y + h * 0.10]; radius: rBig; fill; stroke; width: strokeWidth; },
      { type: "circle"; name: "cloud"; center: [x + w * 0.15, y + h * 0.12]; radius: rBig; fill; stroke; width: strokeWidth; },
      { type: "circle"; name: "cloud"; center: [x + w * 0.36, y]; radius: rMid; fill; stroke; width: strokeWidth; },
      { type: "circle"; name: "cloud"; center: [x - w * 0.18, y - h * 0.10]; radius: rSmall; fill; stroke; width: strokeWidth; },
      { type: "circle"; name: "cloud"; center: [x + w * 0.04, y - h * 0.12]; radius: rSmall; fill; stroke; width: strokeWidth; },
      { type: "circle"; name: "cloud"; center: [x + w * 0.22, y - h * 0.10]; radius: rSmall; fill; stroke; width: strokeWidth; }
    ];

    eval circles;
  };
}

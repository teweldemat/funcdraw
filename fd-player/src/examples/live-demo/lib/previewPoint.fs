(pointParam, centerParam, scaleParam) => {
  point: pointParam ?? { x: 0; y: 0 };
  center: centerParam ?? { x: 0; y: 0 };
  scale: scaleParam ?? 1;
  safeScale: if (scale = 0) then 1 else math.abs(scale);

  px: point.x ?? 0;
  py: point.y ?? 0;
  cx: center.x ?? 0;
  cy: center.y ?? 0;

  eval {
    x: cx + px * safeScale;
    y: cy + py * safeScale;
  };
}

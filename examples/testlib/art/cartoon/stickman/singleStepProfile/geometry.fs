{
  clamp01:(value)=> {
    v:value ?? 0;
    eval if v < 0 then 0 else if v > 1 then 1 else v;
  };

  normalizeSide:(value, fallback)=> {
    defaultSide:fallback ?? "left";
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "left" then "left" else if textValue = "right" then "right" else defaultSide;
  };

  normalizeDirectionValue:(value, fallback)=> {
    defaultDir:fallback ?? "front";
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "left" then "left"
    else if textValue = "right" then "right"
    else if textValue = "front" then "front"
    else if textValue = "back" then "back"
    else defaultDir;
  };

  lerpPoint:(start, end, t)=> [
    start[0] + (end[0] - start[0]) * t,
    start[1] + (end[1] - start[1]) * t
  ];

  computeArcPoint:(start, end, progress, height)=> {
    t:clamp01(progress);
    base:lerpPoint(start, end, t);
    lift:math.sin(math.pi * t) * height;
    eval [base[0], base[1] + lift];
  };

  resolveArcHeight:(start, end, distanceHelper)=> {
    span:distanceBetweenPoints(start, end, distanceHelper);
    eval math.max(1.5, span * 0.25);
  };

  distanceBetweenPoints:(a, b, distanceHelper)=> {
    eval if distanceHelper != null then distanceHelper(a, b) else {
      p1:a ?? [0,0];
      p2:b ?? [0,0];
      dx:p2[0] - p1[0];
      dy:p2[1] - p1[1];
      eval math.sqrt(dx * dx + dy * dy);
    };
  };

  clampAnchorVerticalDrift:(candidate, baseline, defaultPosition, maxVerticalAnchorDelta, clampHelper)=> {
    reference:if baseline = null then defaultPosition else baseline;
    safeCandidate:if candidate = null then reference else candidate;
    minY:reference[1] - maxVerticalAnchorDelta;
    maxY:reference[1] + maxVerticalAnchorDelta;
    clampedY:clampHelper(safeCandidate[1], minY, maxY);
    eval [safeCandidate[0], clampedY];
  };

  eval {
    clamp01:clamp01;
    normalizeSide:normalizeSide;
    normalizeDirectionValue:normalizeDirectionValue;
    lerpPoint:lerpPoint;
    computeArcPoint:computeArcPoint;
    resolveArcHeight:resolveArcHeight;
    distanceBetweenPoints:distanceBetweenPoints;
    clampAnchorVerticalDrift:clampAnchorVerticalDrift;
  };
}

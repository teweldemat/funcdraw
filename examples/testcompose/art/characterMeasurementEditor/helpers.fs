{
  clamp: (x, lo, hi) =>
  {
    eval if x < lo then lo else if x > hi then hi else x;
  };

  dot: (a, b) => a[0] * b[0] + a[1] * b[1];
  dist2: (a, b) => (a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]);

  segmentDist2: (p, a, b) =>
  {
    ab: [b[0] - a[0], b[1] - a[1]];
    ap: [p[0] - a[0], p[1] - a[1]];
    abLen2: ab[0] * ab[0] + ab[1] * ab[1];
    tRaw: if abLen2 == 0 then 0 else dot(ap, ab) / abLen2;
    ratio: clamp(tRaw, 0, 1);
    closest: [a[0] + ab[0] * ratio, a[1] + ab[1] * ratio];
    eval dist2(p, closest);
  };

  makeHitTester: (hitRadius) =>
  {
    hitRadius2: hitRadius * hitRadius;
    hitSegment: (point, a, b) => segmentDist2(point, a, b) <= hitRadius2;
    hitCircle: (point, center, radius) =>
      dist2(point, center) <= (radius + hitRadius) * (radius + hitRadius);
    eval { hitSegment; hitCircle; };
  };
}

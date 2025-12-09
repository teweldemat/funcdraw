{
  add:(a, b)=> {
    pa:a ?? [0,0];
    pb:b ?? [0,0];
    eval [pa[0] + pb[0], pa[1] + pb[1]];
  };

  subtract:(a, b)=> {
    pa:a ?? [0,0];
    pb:b ?? [0,0];
    eval [pa[0] - pb[0], pa[1] - pb[1]];
  };

  average:(a, b)=> {
    hasA:a != null;
    hasB:b != null;
    count:(if hasA then 1 else 0) + (if hasB then 1 else 0);
    sumX:(if hasA then a[0] else 0) + (if hasB then b[0] else 0);
    sumY:(if hasA then a[1] else 0) + (if hasB then b[1] else 0);
    eval if count = 0 then null else [sumX / count, sumY / count];
  };

  clampSymmetric:(value, limit)=> {
    maxValue:limit ?? 1;
    v:value ?? null;
    eval if v = null then 0 else if v > maxValue then maxValue else if v < -maxValue then -maxValue else v;
  };

  scaleToLength:(point, origin, length)=> {
    ox:(origin?!origin[0]) ?? 0;
    oy:(origin?!origin[1]) ?? 0;
    dx:((point?!point[0]) ?? 0) - ox;
    dy:((point?!point[1]) ?? 0) - oy;
    distance:math.sqrt(dx * dx + dy * dy);
    target:math.max(length ?? 0, 0);
    eval if distance < 0.000001 then [ox, oy - target] else {
      scale:if distance = 0 then 0 else target / distance;
      eval [ox + dx * scale, oy + dy * scale];
    };
  };

  eval {
    add:add;
    subtract:subtract;
    average:average;
    clampSymmetric:clampSymmetric;
    scaleToLength:scaleToLength;
  };
}

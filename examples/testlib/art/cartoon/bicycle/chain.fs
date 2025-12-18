(start, end, segments, displacement) =>
{
  dx: (end[0] - start[0]) / segments;
  dy: (end[1] - start[1]) / segments;
  length:
    math.Sqrt(
      (end[0] - start[0]) * (end[0] - start[0])
      + (end[1] - start[1]) * (end[1] - start[1]));

  unitsPerLength: if length == 0 then 0 else segments / length;
  dispUnits: displacement * unitsPerLength;
  segmentFraction: 0.2;

  wrap: (value) =>
  {
    q: math.Floor(value / segments);
    r: value - q * segments;
    eval if r < 0 then r + segments else r;
  };

  point: (value) =>
  [
    start[0] + dx * wrap(value),
    start[1] + dy * wrap(value)
  ];

  raw:
    Range(0, segments) map (k, idx) =>
    {
      a: k + dispUnits;
      b: a + segmentFraction;
      aw: wrap(a);
      bw: wrap(b);
      crossed: bw < aw;
      eval if crossed then null else
      {
        type: "line";
        from: point(a);
        to: point(b);
        stroke: "#fbbf24";
        width: 0.3;
      };
    };

  lines: raw filter (x) => x != null;
  eval lines;
}


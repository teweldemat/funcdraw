// wheel([0, 0], 10, 2, 0)
(center, outerRadius, innerRadius, angle) =>
{
  n: 12;
  lines:
    Range(0, n) map (k, idx) =>
    {
      theta: (angle ?? 0) + k * (2 * math.Pi / n);
      eval
      {
        type: "line";
        from:
        [
          center[0] + math.Sin(theta) * innerRadius,
          center[1] + math.Cos(theta) * innerRadius
        ];
        to:
        [
          center[0] + math.Sin(theta) * outerRadius,
          center[1] + math.Cos(theta) * outerRadius
        ];
        stroke: "#38bdf8";
        width: 0.3;
      };
    };

  eval
    lines
    + [{ type: "circle"; center; radius: outerRadius; stroke: "#334155"; width: 1; fill: "none"; }]
    + [{ type: "circle"; center; radius: innerRadius; stroke: "#38bdf8"; width: 1; fill: "none"; }];
}

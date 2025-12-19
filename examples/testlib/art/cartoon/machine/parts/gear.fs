// gear([0, 0], 4, 12, 0)
(center, radius, teeth, angleAdvance) =>
{
  n: teeth;
  toothSize: 0.7;
  teethLines:
    Range(0, n) reduce (acc, k) =>
      {
        angle: (k * 2 * math.Pi / n) + (angleAdvance ?? 0);
        halfStep: math.Pi / n;
        base1:
        [
          center[0] + math.Sin(angle - halfStep) * radius,
          center[1] + math.Cos(angle - halfStep) * radius
        ];
        base2:
        [
          center[0] + math.Sin(angle + halfStep) * radius,
          center[1] + math.Cos(angle + halfStep) * radius
        ];
        apex:
        [
          center[0] + math.Sin(angle) * (radius + toothSize),
          center[1] + math.Cos(angle) * (radius + toothSize)
        ];
        eval
          acc
          + [
            { type: "line"; from: base1; to: base2; stroke: "#38bdf8"; width: 0.3; },
            { type: "line"; from: base2; to: apex; stroke: "#38bdf8"; width: 0.3; },
            { type: "line"; from: apex; to: base1; stroke: "#38bdf8"; width: 0.3; }
          ];
      }
    ~ [];

  eval
    teethLines
    + [{ type: "circle"; center; radius; stroke: "#334155"; width: 1; fill: "#334155"; }];
}

(options) =>
{
  base: options.base ?? [0, 0];
  height: options.height ?? 24;

  trunkWidth: options.trunkWidth ?? height * 0.14;
  trunkHeight: options.trunkHeight ?? height * 0.38;
  trunkFill: options.trunkFill ?? "#92400e";
  trunkStroke: options.trunkStroke ?? "#451a03";
  trunkWidthStroke: options.trunkStrokeWidth ?? 0.25;

  leafFill: options.leafFill ?? "#16a34a";
  leafStroke: options.leafStroke ?? "#14532d";
  leafStrokeWidth: options.leafStrokeWidth ?? 0.22;

  trunkLeft: base[0] - trunkWidth / 2;
  trunkBottom: base[1];
  trunk:
  {
    type: "rect";
    name: "tree-trunk";
    position: [trunkLeft, trunkBottom];
    size: [trunkWidth, trunkHeight];
    fill: trunkFill;
    stroke: trunkStroke;
    width: trunkWidthStroke;
  };

  crownBottomY: trunkBottom + trunkHeight;
  crownHeight: height - trunkHeight;
  crownCenter: [base[0], crownBottomY + crownHeight * 0.55];
  crownR: crownHeight * 0.42;

  leaf: (center, r, idx) =>
  {
    type: "circle";
    name: "tree-leaf";
    index: idx;
    center;
    radius: r;
    fill: leafFill;
    stroke: leafStroke;
    width: leafStrokeWidth;
  };

  leaves:
  [
    leaf([crownCenter[0] - crownR * 0.55, crownCenter[1] - crownR * 0.05], crownR * 0.95, 0),
    leaf([crownCenter[0] + crownR * 0.6, crownCenter[1] - crownR * 0.08], crownR * 0.92, 1),
    leaf([crownCenter[0], crownCenter[1] + crownR * 0.15], crownR * 1.05, 2)
  ];

  eval [trunk] + leaves;
}


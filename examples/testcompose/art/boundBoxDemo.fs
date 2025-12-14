{
  view:
  {
    left: -400;
    bottom: -300;
    right: 400;
    top: 300;
  };

  labelStyle:
  {
    fontSize: 18;
    color: "#94a3b8";
  };

  sectionBox:
  {
    fill: "#0b122033";
    stroke: "#334155";
    width: 1;
  };

  bboxStyle:
  {
    fill: "#00000000";
    stroke: "#a855f7";
    width: 2;
  };

  // Example A: transformed group
  baseA:
  [
    { type: "rect"; position: [-40, -20]; size: [80, 40]; fill: "#0ea5e933"; stroke: "#0ea5e9"; width: 3; },
    { type: "circle"; center: [20, 10]; radius: 14; fill: "#22c55e33"; stroke: "#22c55e"; width: 2; },
    { type: "polygon"; points: [[-35, -15], [0, 30], [35, -15]]; fill: "#fbbf2433"; stroke: "#fbbf24"; width: 2; },
    { type: "line"; from: [-40, -20]; to: [40, 20]; stroke: "#e11d48"; width: 2; }
  ];

  originA: [-220, 120];
  movedA: fd.translate(baseA, originA[0], originA[1]);
  rotatedA: fd.rotate(movedA, originA, math.Pi / 5);
  scaledA: fd.scale(rotatedA, originA, 1.35, 0.75);
  bboxA: fd.boundingbox(scaledA);
  bboxRectA: { type: "rect"; position: [bboxA.left, bboxA.bottom]; size: [bboxA.width, bboxA.height]; fill: bboxStyle.fill; stroke: bboxStyle.stroke; width: bboxStyle.width; };

  frameA:
  {
    type: "rect";
    position: [-390, 10];
    size: [370, 280];
    fill: sectionBox.fill;
    stroke: sectionBox.stroke;
    width: sectionBox.width;
  };
  labelA:
  {
    type: "text";
    text: "A) transform + rotate + scale";
    position: [-380, 270];
    fontSize: labelStyle.fontSize;
    color: labelStyle.color;
  };

  // Example B: thick line bounding box (includes stroke width)
  lineB:
  {
    type: "line";
    from: [-330, -240];
    to: [-80, -110];
    stroke: "#38bdf8";
    width: 18;
  };
  bboxB: fd.boundingbox(lineB);
  bboxRectB: { type: "rect"; position: [bboxB.left, bboxB.bottom]; size: [bboxB.width, bboxB.height]; fill: bboxStyle.fill; stroke: bboxStyle.stroke; width: bboxStyle.width; };

  frameB:
  {
    type: "rect";
    position: [-390, -290];
    size: [370, 280];
    fill: sectionBox.fill;
    stroke: sectionBox.stroke;
    width: sectionBox.width;
  };
  labelB:
  {
    type: "text";
    text: "B) line (stroke width matters)";
    position: [-380, -30];
    fontSize: labelStyle.fontSize;
    color: labelStyle.color;
  };

  // Example C: text bounding box (with rotation)
  textC:
  {
    type: "text";
    text: "Bounding box\nfor text";
    position: [60, 190];
    fontSize: 44;
    color: "#e2e8f0";
  };
  rotatedTextC: fd.rotate(textC, [60, 190], -math.Pi / 10);
  bboxC: fd.boundingbox(rotatedTextC);
  bboxRectC: { type: "rect"; position: [bboxC.left, bboxC.bottom]; size: [bboxC.width, bboxC.height]; fill: bboxStyle.fill; stroke: bboxStyle.stroke; width: bboxStyle.width; };

  frameC:
  {
    type: "rect";
    position: [-10, 10];
    size: [410, 280];
    fill: sectionBox.fill;
    stroke: sectionBox.stroke;
    width: sectionBox.width;
  };
  labelC:
  {
    type: "text";
    text: "C) text + rotate";
    position: [0, 270];
    fontSize: labelStyle.fontSize;
    color: labelStyle.color;
  };

  eval
  {
    view;
    graphics:
      [frameA, frameB, frameC]
      + [labelA, labelB, labelC]
      + [scaledA, bboxRectA]
      + [lineB, bboxRectB]
      + [rotatedTextC, bboxRectC];
  };
}

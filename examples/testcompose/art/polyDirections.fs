{
  character: package("@funcdraw/testlib").cartoon.character;
  defaults: character.skeleton.defaults;
  scale: 8;
  scaleVec: (v) => [v[0] * scale, v[1] * scale];
  scaleLimb: (limb) =>
  {
    end: scaleVec(limb.end);
    upper: limb.upper * scale;
    lower: limb.lower * scale;
    sign: limb.sign;
  };

  palette:
  {
    body: "#f8fafc";
    limb: "#38bdf8";
  };
  backgroundColor: "#0a0f1a";

  baseAnchors:
  {
    front: [-26, 0];
    left: [-8, 0];
    back: [10, 0];
    right: [28, 0];
  };
  anchors:
  {
    front: scaleVec(baseAnchors.front);
    left: scaleVec(baseAnchors.left);
    back: scaleVec(baseAnchors.back);
    right: scaleVec(baseAnchors.right);
  };

  scaledMeasurements:
  {
    height: defaults.height * scale;
    leftHand: scaleLimb(defaults.leftHand);
    rightHand: scaleLimb(defaults.rightHand);
    leftLeg: scaleLimb(defaults.leftLeg);
    rightLeg: scaleLimb(defaults.rightLeg);
    neckLength: defaults.neckLength * scale;
    headRadius: defaults.headRadius * scale;
    bodyAngle: defaults.bodyAngle;
    neckAngle: defaults.neckAngle;
    handPhaseOffset: defaults.handPhaseOffset;
    shoulderWidth: defaults.shoulderWidth * scale;
    thighWidth: defaults.thighWidth * scale;
    direction: defaults.direction;
  };

  baseGeometry: character.skeleton.build([0, 0], scaledMeasurements);
  groundY: anchors.front[1] + baseGeometry.leftLeg.to[1];
  headCenterY: baseGeometry.neck.to[1] + baseGeometry.measurements.headRadius * math.Sin(baseGeometry.measurements.neckAngle);
  labelY: anchors.front[1] + headCenterY + baseGeometry.measurements.headRadius * 0.8;
  margin: 10 * scale;

  front: character.static(anchors.front, scaledMeasurements + { direction: "front"; }, palette, character.skins.poly);
  left: character.static(anchors.left, scaledMeasurements + { direction: "left"; }, palette, character.skins.poly);
  back: character.static(anchors.back, scaledMeasurements + { direction: "back"; }, palette, character.skins.poly);
  right: character.static(anchors.right, scaledMeasurements + { direction: "right"; }, palette, character.skins.poly);

  ground:
  {
    type: "line";
    from: [anchors.front[0] - margin, groundY];
    to: [anchors.right[0] + margin, groundY];
    stroke: palette.limb;
    width: 0.35;
  };

  view:
  {
    left: -400;
    bottom: -300;
    right: 400;
    top: 300;
  };

  background:
  {
    type: "rect";
    from: [view.left, view.bottom];
    to: [view.right, view.top];
    fill: backgroundColor;
  };

  labels:
  [
    { type: "text"; position: [anchors.front[0], labelY]; text: "FRONT"; color: palette.limb; fontSize: 2; align: "center"; },
    { type: "text"; position: [anchors.left[0], labelY]; text: "LEFT"; color: palette.limb; fontSize: 2; align: "center"; },
    { type: "text"; position: [anchors.back[0], labelY]; text: "BACK"; color: palette.limb; fontSize: 2; align: "center"; },
    { type: "text"; position: [anchors.right[0], labelY]; text: "RIGHT"; color: palette.limb; fontSize: 2; align: "center"; }
  ];

  eval
  {
    view;
    graphics: [background, ground]
      + labels
      + front
      + left
      + back
      + right;
  };
}

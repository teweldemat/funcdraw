(anchor, measurements, palette) =>
{
  defaults: defaultMeasurements;
  m:
  {
    height: measurements.height ?? defaults.height;
    leftHand: measurements.leftHand ?? defaults.leftHand;
    rightHand: measurements.rightHand ?? defaults.rightHand;
    leftLeg: measurements.leftLeg ?? defaults.leftLeg;
    rightLeg: measurements.rightLeg ?? defaults.rightLeg;
  };

  line:
  {
    type: "line";
    from: anchor;
    to: [anchor[0], anchor[1] + m.height];
    stroke: palette.body;
    width: 0.35;
  };

  leftHand:
  {
    type: "line";
    from: [anchor[0], anchor[1] + m.height];
    to: [anchor[0] + m.leftHand[0], anchor[1] + m.height + m.leftHand[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  rightHand:
  {
    type: "line";
    from: [anchor[0], anchor[1] + m.height];
    to: [anchor[0] + m.rightHand[0], anchor[1] + m.height + m.rightHand[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  leftLeg:
  {
    type: "line";
    from: anchor;
    to: [anchor[0] + m.leftLeg[0], anchor[1] + m.leftLeg[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  rightLeg:
  {
    type: "line";
    from: anchor;
    to: [anchor[0] + m.rightLeg[0], anchor[1] + m.rightLeg[1]];
    stroke: palette.limb;
    width: 0.25;
  };

  eval [line, leftHand, rightHand, leftLeg, rightLeg];
}

{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  baseBodyAngle: math.Pi / 2;
  baseNeckAngle: math.Pi / 2;
  baseLeftHand: [-7, -12];
  baseRightHand: [7, -12];
  baseLeftLeg: [-3, -14];
  baseRightLeg: [3, -14];
  bend: math.Sin(t) * 0.6;
  bodyAngle: baseBodyAngle + bend;
  character: package("@funcdraw/testlib").cartoon.character(
    [0, 0],
    {
      bodyAngle: bodyAngle;
      neckAngle: baseNeckAngle + bend * 0.5;
      leftHand: baseLeftHand;
      rightHand: baseRightHand;
      leftLeg: baseLeftLeg;
      rightLeg: baseRightLeg;
    },
    palette);

  eval
  {
    view:
    {
      left: -20;
      bottom: -22;
      right: 22;
      top: 26;
    };
    graphics: character;
  };
}

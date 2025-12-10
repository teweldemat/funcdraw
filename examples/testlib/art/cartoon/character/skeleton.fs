(anchor, measurements) =>
{
  defaults: defaultMeasurements;
  m: defaults + measurements;
  rotate: (vector, angle) =>
  {
    x: vector[0];
    y: vector[1];
    cosA: math.Cos(angle);
    sinA: math.Sin(angle);
    eval [x * cosA - y * sinA, x * sinA + y * cosA];
  };
  bodyDelta: [m.height * math.Cos(m.bodyAngle), m.height * math.Sin(m.bodyAngle)];
  neckDelta: [m.neckLength * math.Cos(m.neckAngle), m.neckLength * math.Sin(m.neckAngle)];

  bodyTo: [anchor[0] + bodyDelta[0], anchor[1] + bodyDelta[1]];
  leftHandOffset: m.leftHand;
  rightHandOffset: m.rightHand;
  leftLegOffset: m.leftLeg;
  rightLegOffset: m.rightLeg;
  leftHandTo: [bodyTo[0] + leftHandOffset[0], bodyTo[1] + leftHandOffset[1]];
  rightHandTo: [bodyTo[0] + rightHandOffset[0], bodyTo[1] + rightHandOffset[1]];
  leftLegTo: [anchor[0] + leftLegOffset[0], anchor[1] + leftLegOffset[1]];
  rightLegTo: [anchor[0] + rightLegOffset[0], anchor[1] + rightLegOffset[1]];
  neckTo: [bodyTo[0] + neckDelta[0], bodyTo[1] + neckDelta[1]];

  eval
  {
    anchor;
    measurements: m;
    body:
    {
      from: anchor;
      to: bodyTo;
    };
    neck:
    {
      from: bodyTo;
      to: neckTo;
    };
    leftHand:
    {
      from: bodyTo;
      to: leftHandTo;
    };
    rightHand:
    {
      from: bodyTo;
      to: rightHandTo;
    };
    leftLeg:
    {
      from: anchor;
      to: leftLegTo;
    };
    rightLeg:
    {
      from: anchor;
      to: rightLegTo;
    };
  };
}

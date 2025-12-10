(anchor, measurements, palette) =>
{
  geometry: skeleton.build(anchor, measurements);
  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];

  body:
  {
    type: "line";
    from: geometry.body.from;
    to: geometry.body.to;
    stroke: palette.body;
    width: 0.35;
  };

  neck:
  {
    type: "line";
    from: geometry.neck.from;
    to: geometry.neck.to;
    stroke: palette.body;
    width: 0.25;
  };

  head:
  {
    type: "circle";
    center: headCenter;
    radius: geometry.measurements.headRadius;
    stroke: palette.body;
    width: 0.25;
  };

  leftHand:
  {
    type: "line";
    from: geometry.leftHand.from;
    to: geometry.leftHand.to;
    stroke: palette.limb;
    width: 0.25;
  };

  rightHand:
  {
    type: "line";
    from: geometry.rightHand.from;
    to: geometry.rightHand.to;
    stroke: palette.limb;
    width: 0.25;
  };

  leftLeg:
  {
    type: "line";
    from: geometry.leftLeg.from;
    to: geometry.leftLeg.to;
    stroke: palette.limb;
    width: 0.25;
  };

  rightLeg:
  {
    type: "line";
    from: geometry.rightLeg.from;
    to: geometry.rightLeg.to;
    stroke: palette.limb;
    width: 0.25;
  };

  eval [body, neck, head, leftHand, rightHand, leftLeg, rightLeg];
}

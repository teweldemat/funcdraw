(geometry, palette) =>
{
  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];

  limbSegments: (limb, stroke, width) =>
  {
    upper:
    {
      type: "line";
      from: limb.from;
      to: limb.joint;
      stroke;
      width;
    };
    lower:
    {
      type: "line";
      from: limb.joint;
      to: limb.to;
      tag: limb.to;
      stroke;
      width;
    };

    eval [upper, lower];
  };

  body:
  {
    type: "line";
    name:'body'
    from: geometry.body.from;
    to: geometry.body.to;
    tag: geometry.anchor;
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

  limbWidth: 0.25;
  leftHandSegments: limbSegments(geometry.leftHand, palette.limb, limbWidth);
  rightHandSegments: limbSegments(geometry.rightHand, palette.limb, limbWidth);
  leftLegSegments: limbSegments(geometry.leftLeg, palette.limb, limbWidth);
  rightLegSegments: limbSegments(geometry.rightLeg, palette.limb, limbWidth);

  eval [body, neck, head]
    + leftHandSegments
    + rightHandSegments
    + leftLegSegments
    + rightLegSegments;
}

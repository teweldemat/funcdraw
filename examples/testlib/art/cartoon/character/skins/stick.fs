(geometry, palette) =>
{
  direction: geometry.direction;
  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];
  eyeRadius: geometry.measurements.headRadius * 0.2;
  eyeOffsetX: geometry.measurements.headRadius * 0.5;
  eyeOffsetY: geometry.measurements.headRadius * 0.15;
  eye: (offsetX) =>
  {
    type: "circle";
    center: [headCenter[0] + offsetX, headCenter[1] + eyeOffsetY];
    radius: eyeRadius;
    stroke: palette.body;
    width: 0.15;
  };
  backMark:
  {
    type: "line";
    from: [headCenter[0] - eyeOffsetX * 0.6, headCenter[1] + eyeOffsetY];
    to: [headCenter[0] + eyeOffsetX * 0.6, headCenter[1] + eyeOffsetY];
    stroke: palette.body;
    width: 0.15;
  };
  eyes: if direction == "front" then [eye(-eyeOffsetX), eye(eyeOffsetX)]
    else if direction == "left" then [eye(-eyeOffsetX * 0.6)]
    else if direction == "right" then [eye(eyeOffsetX * 0.6)]
    else if direction == "back" then [backMark]
    else error("expected direction left|right|front|back");

  shoulders:
  {
    type: "line";
    from: geometry.leftHandAttachment;
    to: geometry.rightHandAttachment;
    stroke: palette.body;
    width: 0.25;
  };

  thighs:
  {
    type: "line";
    from: geometry.leftLegAttachment;
    to: geometry.rightLegAttachment;
    stroke: palette.body;
    width: 0.25;
  };

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
  backHands: if direction == "left" then rightHandSegments
    else if direction == "right" then leftHandSegments
    else [];
  frontHands: if direction == "left" then leftHandSegments
    else if direction == "right" then rightHandSegments
    else leftHandSegments + rightHandSegments;
  leftLegSegments: limbSegments(geometry.leftLeg, palette.limb, limbWidth);
  rightLegSegments: limbSegments(geometry.rightLeg, palette.limb, limbWidth);
  backLegs: if direction == "left" then rightLegSegments
    else if direction == "right" then leftLegSegments
    else [];
  frontLegs: if direction == "left" then leftLegSegments
    else if direction == "right" then rightLegSegments
    else leftLegSegments + rightLegSegments;

  eval backHands
    + backLegs
    + [shoulders, thighs, body, neck, head]
    + frontHands
    + frontLegs
    + eyes;
}

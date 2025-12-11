(geometry, palette) =>
{
  direction: geometry.direction;
  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];
  headHeight: geometry.measurements.headRadius * 2.4;
  headWidthFront: geometry.measurements.headRadius * 2;
  headWidthSide: geometry.measurements.headRadius * 1.3;
  profileSign: if direction == "right" then 1 else -1;

  headPointsFront:
  [
    [headCenter[0] - headWidthFront * 0.5, headCenter[1] + headHeight * 0.1],
    [headCenter[0], headCenter[1] + headHeight * 0.6],
    [headCenter[0] + headWidthFront * 0.5, headCenter[1] + headHeight * 0.1],
    [headCenter[0] + headWidthFront * 0.4, headCenter[1] - headHeight * 0.6],
    [headCenter[0] - headWidthFront * 0.4, headCenter[1] - headHeight * 0.6]
  ];

  headPointsSide:
  [
    [headCenter[0] - headWidthSide * 0.6 * profileSign, headCenter[1] + headHeight * 0.3],
    [headCenter[0], headCenter[1] + headHeight * 0.55],
    [headCenter[0] + headWidthSide * 0.7 * profileSign, headCenter[1] + headHeight * 0.1],
    [headCenter[0] + headWidthSide * 0.5 * profileSign, headCenter[1] - headHeight * 0.55],
    [headCenter[0] - headWidthSide * 0.5 * profileSign, headCenter[1] - headHeight * 0.35]
  ];

  head:
  {
    type: "polygon";
    name: "head";
    points: if direction == "left" then headPointsSide else if direction == "right" then headPointsSide else headPointsFront;
    fill: palette.body;
    stroke: palette.body;
  };

  eyeSize: geometry.measurements.headRadius * 0.3;
  eyeDiamond: (center) =>
  {
    type: "polygon";
    points:
    [
      [center[0] - eyeSize * 0.6, center[1]],
      [center[0], center[1] + eyeSize * 0.6],
      [center[0] + eyeSize * 0.6, center[1]],
      [center[0], center[1] - eyeSize * 0.6]
    ];
    fill: palette.limb;
    stroke: palette.limb;
  };

  nose:
  {
    type: "polygon";
    points:
    [
      [headCenter[0], headCenter[1] + headHeight * 0.05],
      [headCenter[0] + geometry.measurements.headRadius * 0.25, headCenter[1] - headHeight * 0.05],
      [headCenter[0], headCenter[1] - headHeight * 0.18]
    ];
    fill: palette.body;
    stroke: palette.body;
  };

  eyes: if direction == "front" then
    [
      eyeDiamond([headCenter[0] - eyeSize * 0.8, headCenter[1] + headHeight * 0.1]),
      eyeDiamond([headCenter[0] + eyeSize * 0.8, headCenter[1] + headHeight * 0.1]),
      nose
    ]
  else if direction == "back" then
    [
      eyeDiamond([headCenter[0] - eyeSize * 0.8, headCenter[1] + headHeight * 0.1]),
      eyeDiamond([headCenter[0] + eyeSize * 0.8, headCenter[1] + headHeight * 0.1]),
      nose
    ]
  else
    [
      eyeDiamond([headCenter[0] + headWidthSide * 0.1 * profileSign, headCenter[1] + headHeight * 0.1])
    ];

  segmentPolygon: (from, to, width, color, name) =>
  {
    dx: to[0] - from[0];
    dy: to[1] - from[1];
    length: math.Sqrt(dx * dx + dy * dy);
    dir: [dx / length, dy / length];
    perp: [-dir[1], dir[0]];
    half: width * 0.5;
    offset: [perp[0] * half, perp[1] * half];

    eval
    {
      type: "polygon";
      name;
      points:
      [
        [from[0] - offset[0], from[1] - offset[1]],
        [from[0] + offset[0], from[1] + offset[1]],
        [to[0] + offset[0], to[1] + offset[1]],
        [to[0] - offset[0], to[1] - offset[1]]
      ];
      fill: color;
      stroke: color;
    };
  };

  body:
  {
    type: "polygon";
    name: "body";
    points: if direction == "left" then
    {
      dx: geometry.body.to[0] - geometry.body.from[0];
      dy: geometry.body.to[1] - geometry.body.from[1];
      length: math.Sqrt(dx * dx + dy * dy);
      dir: [dx / length, dy / length];
      perp: [-dir[1], dir[0]];
      halfWidth: math.Max(geometry.measurements.shoulderWidth, geometry.measurements.thighWidth) * 0.5;
      offset: [perp[0] * halfWidth, perp[1] * halfWidth];

      eval
      [
        [geometry.body.from[0] - offset[0], geometry.body.from[1] - offset[1]],
        [geometry.body.from[0] + offset[0], geometry.body.from[1] + offset[1]],
        [geometry.body.to[0] + offset[0], geometry.body.to[1] + offset[1]],
        [geometry.body.to[0] - offset[0], geometry.body.to[1] - offset[1]]
      ];
    }
    else if direction == "right" then
    {
      dx: geometry.body.to[0] - geometry.body.from[0];
      dy: geometry.body.to[1] - geometry.body.from[1];
      length: math.Sqrt(dx * dx + dy * dy);
      dir: [dx / length, dy / length];
      perp: [-dir[1], dir[0]];
      halfWidth: math.Max(geometry.measurements.shoulderWidth, geometry.measurements.thighWidth) * 0.5;
      offset: [perp[0] * halfWidth, perp[1] * halfWidth];

      eval
      [
        [geometry.body.from[0] - offset[0], geometry.body.from[1] - offset[1]],
        [geometry.body.from[0] + offset[0], geometry.body.from[1] + offset[1]],
        [geometry.body.to[0] + offset[0], geometry.body.to[1] + offset[1]],
        [geometry.body.to[0] - offset[0], geometry.body.to[1] - offset[1]]
      ];
    }
    else
      [geometry.leftHandAttachment, geometry.rightHandAttachment, geometry.rightLegAttachment, geometry.leftLegAttachment];
    fill: palette.body;
    stroke: palette.body;
  };

  neck: segmentPolygon(geometry.neck.from, geometry.neck.to, geometry.measurements.headRadius * 0.4, palette.body, "neck");

  limbWidth: 0.9;
  limbSegmentsPoly: (limb, name) =>
  {
    upper: segmentPolygon(limb.from, limb.joint, limbWidth, palette.limb, name + "-upper");
    lower: segmentPolygon(limb.joint, limb.to, limbWidth, palette.limb, name + "-lower");
    eval [upper, lower];
  };

  leftHandSegments: limbSegmentsPoly(geometry.leftHand, "leftHand");
  rightHandSegments: limbSegmentsPoly(geometry.rightHand, "rightHand");
  backHands: if direction == "left" then rightHandSegments
    else if direction == "right" then leftHandSegments
    else [];
  frontHands: if direction == "left" then leftHandSegments
    else if direction == "right" then rightHandSegments
    else leftHandSegments + rightHandSegments;
  leftLegSegments: limbSegmentsPoly(geometry.leftLeg, "leftLeg");
  rightLegSegments: limbSegmentsPoly(geometry.rightLeg, "rightLeg");
  backLegs: if direction == "left" then rightLegSegments
    else if direction == "right" then leftLegSegments
    else [];
  frontLegs: if direction == "left" then leftLegSegments
    else if direction == "right" then rightLegSegments
    else leftLegSegments + rightLegSegments;

  eval backHands
    + backLegs
    + [body, neck]
    + [head]
    + eyes
    + frontHands
    + frontLegs;
}

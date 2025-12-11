{
  defaults: defaultMeasurements;

  solveLimb: (attach, limb) =>
  {
    dx: limb.end[0];
    dy: limb.end[1];
    distance: math.Sqrt(dx * dx + dy * dy);
    direction: if distance == 0 then [1, 0] else [dx / distance, dy / distance];
    minReach: math.Abs(limb.upper - limb.lower);
    maxReach: limb.upper + limb.lower;
    clampedDistance: if distance > maxReach then maxReach else if distance < minReach then minReach else distance;
    perpendicular: [-direction[1], direction[0]];
    along: (limb.upper * limb.upper - limb.lower * limb.lower + clampedDistance * clampedDistance) / (2 * clampedDistance);
    height: math.Sqrt(math.Max(limb.upper * limb.upper - along * along, 0));
    targetDelta: [direction[0] * clampedDistance, direction[1] * clampedDistance];
    joint:
    [
      attach[0] + direction[0] * along + perpendicular[0] * height * limb.sign,
      attach[1] + direction[1] * along + perpendicular[1] * height * limb.sign
    ];
    to: [attach[0] + targetDelta[0], attach[1] + targetDelta[1]];

    eval { from: attach; joint: joint; to: to; };
  };

  build: (anchor, measurements) =>
  {
    base: defaults + measurements;
    merged:
    {
      leftHand: defaults.leftHand + base.leftHand;
      rightHand: defaults.rightHand + base.rightHand;
      leftLeg: defaults.leftLeg + base.leftLeg;
      rightLeg: defaults.rightLeg + base.rightLeg;
    };
    m: base + merged;
    bodyDelta: [m.height * math.Cos(m.bodyAngle), m.height * math.Sin(m.bodyAngle)];
    bodyLength: math.Sqrt(bodyDelta[0] * bodyDelta[0] + bodyDelta[1] * bodyDelta[1]);
    bodyDirection: [bodyDelta[0] / bodyLength, bodyDelta[1] / bodyLength];
    perpendicular: [-bodyDirection[1], bodyDirection[0]];
    spread: if m.direction == "front" then 1 else if m.direction == "back" then 1 else if m.direction == "left" then 0 else if m.direction == "right" then 0 else error("expected direction left|right|front|back");
    shoulderSpread: m.shoulderWidth * spread;
    thighSpread: m.thighWidth * spread;
    leftHandAttachment: [anchor[0] + bodyDelta[0] + perpendicular[0] * shoulderSpread, anchor[1] + bodyDelta[1] + perpendicular[1] * shoulderSpread];
    rightHandAttachment: [anchor[0] + bodyDelta[0] - perpendicular[0] * shoulderSpread, anchor[1] + bodyDelta[1] - perpendicular[1] * shoulderSpread];
    leftLegAttachment: [anchor[0] + perpendicular[0] * thighSpread, anchor[1] + perpendicular[1] * thighSpread];
    rightLegAttachment: [anchor[0] - perpendicular[0] * thighSpread, anchor[1] - perpendicular[1] * thighSpread];
    neckDelta: [m.neckLength * math.Cos(m.neckAngle), m.neckLength * math.Sin(m.neckAngle)];

    bodyTo: [anchor[0] + bodyDelta[0], anchor[1] + bodyDelta[1]];
    leftHandGeometry: solveLimb(leftHandAttachment, m.leftHand);
    rightHandGeometry: solveLimb(rightHandAttachment, m.rightHand);
    leftLegGeometry: solveLimb(leftLegAttachment, m.leftLeg);
    rightLegGeometry: solveLimb(rightLegAttachment, m.rightLeg);
    neckTo: [bodyTo[0] + neckDelta[0], bodyTo[1] + neckDelta[1]];

    eval
    {
      anchor;
      direction: m.direction;
      leftHandAttachment;
      rightHandAttachment;
      leftLegAttachment;
      rightLegAttachment;
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
        from: leftHandGeometry.from;
        joint: leftHandGeometry.joint;
        to: leftHandGeometry.to;
      };
      rightHand:
      {
        from: rightHandGeometry.from;
        joint: rightHandGeometry.joint;
        to: rightHandGeometry.to;
      };
      leftLeg:
      {
        from: leftLegGeometry.from;
        joint: leftLegGeometry.joint;
        to: leftLegGeometry.to;
      };
      rightLeg:
      {
        from: rightLegGeometry.from;
        joint: rightLegGeometry.joint;
        to: rightLegGeometry.to;
      };
    };
  };
}

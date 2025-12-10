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
    neckDelta: [m.neckLength * math.Cos(m.neckAngle), m.neckLength * math.Sin(m.neckAngle)];

    bodyTo: [anchor[0] + bodyDelta[0], anchor[1] + bodyDelta[1]];
    leftHandGeometry: solveLimb(bodyTo, m.leftHand);
    rightHandGeometry: solveLimb(bodyTo, m.rightHand);
    leftLegGeometry: solveLimb(anchor, m.leftLeg);
    rightLegGeometry: solveLimb(anchor, m.rightLeg);
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

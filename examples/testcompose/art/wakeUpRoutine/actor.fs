{
  character: package("@funcdraw/testlib").cartoon.character;
  skin:character.skins.poly;

  // Leg reach ratio (1 = straight legs, <1 = crouched).
  legCrouch01: 0.92;
  // Arm reach ratio (1 = straight arms, <1 = slight bend).
  armBend01: 0.97;

  characterMeasurements:
  {
    defaults: character.skeleton.defaults;
    s: 0.6;
    // Slight crouch helps the walk feel grounded (less "sliding").
    legReach01: legCrouch01;
    scaleVec: (v) => [v[0] * s, v[1] * s];
    scaleLimb: (limb) =>
    {
      end: scaleVec(limb.end);
      upper: limb.upper * s;
      lower: limb.lower * s;
      sign: limb.sign;
    };
    straightLimb: (limb, reach01) =>
    {
      scaled: scaleLimb(limb);
      total: scaled.upper + scaled.lower;
      eval scaled + { end: [0, -total * (reach01??1)]; };
    };
    eval
    {
      height: defaults.height * s;
      leftHand: straightLimb(defaults.leftHand, armBend01);
      rightHand: straightLimb(defaults.rightHand, armBend01);
      leftLeg: straightLimb(defaults.leftLeg, legReach01);
      rightLeg: straightLimb(defaults.rightLeg, legReach01);
      neckLength: defaults.neckLength * s;
      headRadius: defaults.headRadius * s;
      bodyAngle: defaults.bodyAngle;
      neckAngle: defaults.neckAngle;
      handPhaseOffset: defaults.handPhaseOffset;
      shoulderWidth: defaults.shoulderWidth * s * 1.45;
      thighWidth: defaults.thighWidth * s;
    };
  };

  ensureSlackIfStraight: (limb, reach01) =>
  {
    end: limb.end;
    dist: math.Sqrt(end[0] * end[0] + end[1] * end[1]);
    total: limb.upper + limb.lower;
    eps: 0.000001;

    // If the limb is (almost) perfectly straight, "restore" a bit of extra reach so it bends.
    // This is especially important after `zoomWalk(..., progress=1, ...)`, which normalizes
    // limb lengths to match `end`, removing the intended slack/crouch.
    eval
      if math.Abs(dist - total) < eps then
        limb + { upper: limb.upper / reach01; lower: limb.lower / reach01; }
      else limb;
  };

  crouchProfile: (profile) =>
    profile + {
      leftLeg: ensureSlackIfStraight(profile.leftLeg, legCrouch01);
      rightLeg: ensureSlackIfStraight(profile.rightLeg, legCrouch01);
      leftHand: ensureSlackIfStraight(profile.leftHand, armBend01);
      rightHand: ensureSlackIfStraight(profile.rightHand, armBend01);
    };

  frontPose:
  {
    direction: "front";
    leftLeg: { sign: -1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: 1; };
  };

  backPose:
  {
    direction: "back";
    leftLeg: { sign: -1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: 1; };
  };

  rightPose:
  {
    direction: "right";
    leftLeg: { sign: 1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: -1; };
  };

  charPalette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  doorCharacterAnchor: (layout) =>
  {
    legY: characterMeasurements.leftLeg.end[1];
    doorCenterX: layout.doorPos[0] + layout.doorSize[0] / 2;
    eval [doorCenterX, layout.doorPos[1] - legY];
  };
}

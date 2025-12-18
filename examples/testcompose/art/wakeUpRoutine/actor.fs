{
  character: package("@funcdraw/testlib").cartoon.character;
  skin:character.skins.poly;

  // Target reach ratios for walking (1 = straight, <1 = more bend/slack).
  legCrouch01: 0.96;
  sideLegCrouch01: 0.99;
  // Arm reach ratio (1 = straight arms, <1 = slight bend).
  armBend01: 0.97;

  characterMeasurements:
  {
    defaults: character.skeleton.defaults;
    s: 0.6;
    // Standing should be fully straight; walking slack is applied via `crouchProfile(...)`.
    legReach01: 1;
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

  ensureReach01: (limb, reach01) =>
  {
    end: limb.end;
    dist: math.Sqrt(end[0] * end[0] + end[1] * end[1]);
    total: limb.upper + limb.lower;
    eps: 0.0000001;
    r: reach01 ?? 1;
    minTotal: if r <= 0 then total else dist / r;
    scale: if total <= eps then 1 else minTotal / total;
    eval if scale > 1 + eps then limb + { upper: limb.upper * scale; lower: limb.lower * scale; } else limb;
  };

  crouchProfile: (profile) =>
  {
    legReach:
      if profile.direction == "left" or profile.direction == "right" then sideLegCrouch01
      else legCrouch01;
    eval
      profile
      + {
        leftLeg: ensureReach01(profile.leftLeg, legReach);
        rightLeg: ensureReach01(profile.rightLeg, legReach);
        leftHand: ensureReach01(profile.leftHand, armBend01);
        rightHand: ensureReach01(profile.rightHand, armBend01);
      };
  };

  straighten: (limb) => limb + { end: [0, -(limb.upper + limb.lower)]; };

  standProfile: (profile) =>
    profile + {
      leftLeg: straighten(profile.leftLeg);
      rightLeg: straighten(profile.rightLeg);
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

  leftPose:
  {
    direction: "left";
    leftLeg: { sign: -1; };
    rightLeg: { sign: -1; };
    leftHand: { sign: 1; };
    rightHand: { sign: 1; };
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

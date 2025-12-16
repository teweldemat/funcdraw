{
  character: package("@funcdraw/testlib").cartoon.character;
  skin:character.skins.poly;
  characterMeasurements:
  {
    defaults: character.skeleton.defaults;
    s: 0.6;
    scaleVec: (v) => [v[0] * s, v[1] * s];
    scaleLimb: (limb) =>
    {
      end: scaleVec(limb.end);
      upper: limb.upper * s;
      lower: limb.lower * s;
      sign: limb.sign;
    };
    straightLimb: (limb) =>
    {
      scaled: scaleLimb(limb);
      total: scaled.upper + scaled.lower;
      eval scaled + { end: [0, -total]; };
    };
    eval
    {
      height: defaults.height * s;
      leftHand: straightLimb(defaults.leftHand);
      rightHand: straightLimb(defaults.rightHand);
      leftLeg: straightLimb(defaults.leftLeg);
      rightLeg: straightLimb(defaults.rightLeg);
      neckLength: defaults.neckLength * s;
      headRadius: defaults.headRadius * s;
      bodyAngle: defaults.bodyAngle;
      neckAngle: defaults.neckAngle;
      handPhaseOffset: defaults.handPhaseOffset;
      shoulderWidth: defaults.shoulderWidth * s * 1.45;
      thighWidth: defaults.thighWidth * s;
    };
  };

  frontPose:
  {
    direction: "front";
    leftLeg: { sign: -1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
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

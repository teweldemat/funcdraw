{
  character: package("@funcdraw/testlib").cartoon.character;
  skin: character.skins.poly;

  // Match wakeUpRoutine actor measurements.
  legCrouch01: 0.95;
  armBend01: 0.97;

  characterMeasurements:
  {
    defaults: character.skeleton.defaults;
    s: 0.6;
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

  rightPose:
  {
    direction: "right";
    leftLeg: { sign: 1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: -1; };
  };

  palette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  base: characterMeasurements + rightPose;

  strideRaw: 6;
  steps: 3;
  walkStepsPerSecond: 3;
  slowMo: 6;

  legTotal: (base.leftLeg.upper + base.leftLeg.lower + base.rightLeg.upper + base.rightLeg.lower) / 2;
  defaultLegTotal:
    (character.skeleton.defaults.leftLeg.upper
    + character.skeleton.defaults.leftLeg.lower
    + character.skeleton.defaults.rightLeg.upper
    + character.skeleton.defaults.rightLeg.lower) / 2;
  legScale: legTotal / defaultLegTotal;
  stride: math.Abs(strideRaw) * legScale;
  distance: stride * steps;

  duration: (steps / walkStepsPerSecond) * slowMo;
  progress:
    if t <= 0 then 0
    else if t >= duration then 1
    else t / duration;

  groundY: 0;
  anchor0: [0, -base.leftLeg.end[1] + groundY];

  current: character.profileWalk(anchor0, base, distance, strideRaw, progress);
  geometry: character.skeleton.build(current.anchor, current);

  viewHeight: 60;
  ratio: canvas.size.width / canvas.size.height;
  viewWidth: viewHeight * ratio;
  centerX: current.anchor[0];
  left: centerX - viewWidth / 2;
  bottom: -5;
  view: { left; bottom; right: left + viewWidth; top: bottom + viewHeight; };

  ground:
  {
    type: "line";
    from: [view.left, groundY];
    to: [view.right, groundY];
    stroke: "#334155";
    width: 0.4;
  };

  marker: (point, fill) =>
  {
    type: "circle";
    center: point;
    radius: 0.75;
    fill;
    stroke: "#0f172a";
    width: 0.25;
  };

  plannedPath:
  {
    type: "line";
    from: anchor0;
    to: [anchor0[0] + distance, anchor0[1]];
    stroke: fd.color.alpha("#38bdf8", 0.35);
    width: 0.3;
  };

  label:
  {
    type: "text";
    text: "profileWalkDebug: anchor + foot markers";
    position: [view.left + 2, view.top - 4];
    fontSize: 2.1;
    color: "#94a3b8";
  };

  characterGraphic: character.static(current.anchor, current, palette, skin);

  eval
  {
    view;
    graphics:
      [
        ground,
        plannedPath,
        characterGraphic,
        marker(current.anchor, "#f472b6"),
        marker(geometry.leftLeg.to, "#22c55e"),
        marker(geometry.rightLeg.to, "#ef4444"),
        label
      ];
  };
}


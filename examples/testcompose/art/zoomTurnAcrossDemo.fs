{
  character: package("@funcdraw/testlib").cartoon.character;

  palette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  defaults: character.skeleton.defaults;

  farScale: 0.35;
  scaleVec: (v, s) => [v[0] * s, v[1] * s];
  scaleLimb: (limb, s) =>
  {
    end: scaleVec(limb.end, s);
    upper: limb.upper * s;
    lower: limb.lower * s;
    sign: limb.sign;
  };

  farMeasurements:
  {
    height: defaults.height * farScale;
    leftHand: scaleLimb(defaults.leftHand, farScale);
    rightHand: scaleLimb(defaults.rightHand, farScale);
    leftLeg: scaleLimb(defaults.leftLeg, farScale);
    rightLeg: scaleLimb(defaults.rightLeg, farScale);
    neckLength: defaults.neckLength * farScale;
    headRadius: defaults.headRadius * farScale;
    bodyAngle: defaults.bodyAngle;
    neckAngle: defaults.neckAngle;
    handPhaseOffset: defaults.handPhaseOffset;
    shoulderWidth: defaults.shoulderWidth * farScale;
    thighWidth: defaults.thighWidth * farScale;
    direction: "front";
  };

  ease01: (p) => (1 - math.Cos(p * math.Pi)) / 2;

  zoomDuration: 4;
  turnDuration: 2;
  acrossDuration: 6;
  cycleDuration: zoomDuration + turnDuration + acrossDuration;
  localT: t - math.Floor(t / cycleDuration) * cycleDuration;

  stage:
    if localT < zoomDuration then "zoom"
    else if localT < zoomDuration + turnDuration then "turn"
    else "across";

  zoomProgress: if localT < zoomDuration then ease01(localT / zoomDuration) else 1;
  turnProgress: if localT < zoomDuration then 0
    else if localT < zoomDuration + turnDuration then ease01((localT - zoomDuration) / turnDuration)
    else 1;
  acrossT: localT - zoomDuration - turnDuration;
  acrossProgress:
    if acrossT < 0 then 0
    else if acrossT > acrossDuration then 1
    else acrossT / acrossDuration;

  startAnchor: [-18, 22];
  verticalDistance: -30;
  zoomStride: 6;
  zoomFactor: 0.04;

  acrossDistance: 44;
  acrossStride: 6;

  frontBase:
  {
    direction: "front";
    leftLeg: { sign: -1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: 1; };
  };

  rightBase:
  {
    direction: "right";
    leftLeg: { sign: 1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: -1; };
  };

  zoomBase: farMeasurements + frontBase;
  zoomProfile: character.zoomWalk(startAnchor, zoomBase, verticalDistance, zoomStride, zoomProgress, zoomFactor);
  zoomEndProfile: character.zoomWalk(startAnchor, zoomBase, verticalDistance, zoomStride, 1, zoomFactor);

  turnDirection: if turnProgress < 0.5 then "front" else "right";
  turnBase: if turnDirection == "front" then frontBase else rightBase;
  turnProfile: zoomEndProfile + turnBase;

  acrossBase: zoomEndProfile + rightBase;
  acrossProfile: character.profileWalk(zoomEndProfile.anchor, acrossBase, acrossDistance, acrossStride, acrossProgress);

  profile:
    if stage == "zoom" then zoomProfile
    else if stage == "turn" then turnProfile
    else acrossProfile;

  actor: character.static(profile.anchor, profile, palette);

  footMinY: (p) => math.Min(p.anchor[1] + p.leftLeg.end[1], p.anchor[1] + p.rightLeg.end[1]);
  headMaxY: (p) => p.anchor[1] + p.height + p.neckLength + p.headRadius;

  zoomStartProfile: character.zoomWalk(startAnchor, zoomBase, verticalDistance, zoomStride, 0, zoomFactor);
  acrossMidProfile: character.profileWalk(zoomEndProfile.anchor, acrossBase, acrossDistance, acrossStride, 0.5);
  acrossEndProfile: character.profileWalk(zoomEndProfile.anchor, acrossBase, acrossDistance, acrossStride, 1);

  minFootY: math.Min(footMinY(zoomStartProfile), math.Min(footMinY(zoomEndProfile), math.Min(footMinY(acrossMidProfile), footMinY(acrossEndProfile))));
  maxHeadY: math.Max(headMaxY(zoomStartProfile), math.Max(headMaxY(zoomEndProfile), math.Max(headMaxY(acrossMidProfile), headMaxY(acrossEndProfile))));

  marginX: acrossEndProfile.leftHand.upper + acrossEndProfile.leftHand.lower + 6;
  marginY: acrossEndProfile.height + 12;
  endAnchorAcross: [zoomEndProfile.anchor[0] + acrossDistance, zoomEndProfile.anchor[1]];

  view:
  {
    left: startAnchor[0] - marginX;
    right: endAnchorAcross[0] + marginX;
    bottom: minFootY - marginY;
    top: maxHeadY + marginY * 0.6;
  };

  background:
  {
    type: "rect";
    from: [view.left, view.bottom];
    to: [view.right, view.top];
    fill: "#020617";
    stroke: "none";
    width: 0;
  };

  approachPath:
  {
    type: "line";
    from: startAnchor;
    to: zoomEndProfile.anchor;
    stroke: "#1e293b";
    width: 0.25;
  };

  acrossPath:
  {
    type: "line";
    from: zoomEndProfile.anchor;
    to: endAnchorAcross;
    stroke: "#1e293b";
    width: 0.25;
  };

  pivot:
  {
    type: "circle";
    center: zoomEndProfile.anchor;
    radius: 0.6;
    fill: "#0f172a";
    stroke: "#94a3b8";
    width: 0.15;
  };

  stageLabel:
  {
    type: "text";
    text: join(
      [
        "stage=", stage,
        "  direction=", profile.direction,
        "  zoom=", format(zoomProgress, "0.00"),
        "  turn=", format(turnProgress, "0.00"),
        "  across=", format(acrossProgress, "0.00")
      ],
      ""
    );
    position: [view.left + 2, view.top - 4];
    fontSize: 2.2;
    color: "#94a3b8";
  };

  eval
  {
    view;
    graphics: [background, approachPath, acrossPath, pivot, stageLabel] + actor;
  };
}

{
  character: package("@funcdraw/testlib").cartoon.character;

  palette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  defaults: character.skeleton.defaults;

  horizontalDistance: 24;
  strideLength: 6;
  startAnchor: [-12, 0];

  cycleDuration: 6;
  localT: t - math.Floor(t / cycleDuration) * cycleDuration;
  angle: localT / cycleDuration * 2 * math.Pi;
  progress: (1 - math.Cos(angle)) / 2;

  walkBase:
  {
    direction: "right";
    leftLeg: { sign: 1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: -1; };
  };

  profile: character.profileWalk(startAnchor, walkBase, horizontalDistance, strideLength, progress);
  actor: character.static(profile.anchor, profile, palette,character.skins.poly);

  groundY: startAnchor[1] + defaults.leftLeg.end[1];
  endAnchor: [startAnchor[0] + horizontalDistance, startAnchor[1]];

  view:
  {
    left: startAnchor[0] - 14;
    bottom: groundY - 8;
    right: startAnchor[0] + horizontalDistance + 14;
    top: 26;
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

  ground:
  {
    type: "line";
    from: [view.left, groundY];
    to: [view.right, groundY];
    stroke: "#334155";
    width: 0.25;
  };

  anchorPath:
  {
    type: "line";
    from: [startAnchor[0], startAnchor[1]];
    to: [endAnchor[0], endAnchor[1]];
    stroke: "#1e293b";
    width: 0.25;
  };

  strideCount: math.Floor(horizontalDistance / strideLength) + 1;
  strideMarks:
    Range(0, strideCount) map (k, idx) =>
    {
      x: startAnchor[0] + strideLength * k;
      eval
      {
        type: "circle";
        center: [x, groundY];
        radius: 0.35;
        stroke: "#475569";
        width: 0.15;
      };
    };

  cursor:
  {
    type: "line";
    from: [profile.anchor[0], startAnchor[1]];
    to: [profile.anchor[0], groundY];
    stroke: "#94a3b8";
    width: 0.15;
  };

  label:
  {
    type: "text";
    text: join(["profileWalk progress=", format(progress, "0.00")], "");
    position: [view.left + 2, view.top - 4];
    fontSize: 2.2;
    color: "#94a3b8";
  };

  eval
  {
    view;
    graphics: [background, ground, anchorPath, strideMarks, cursor, actor, label];
  };
}

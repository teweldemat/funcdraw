{
  character: package("@funcdraw/testlib").cartoon.character;

  palette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  verticalDistance: -24;
  strideLength: 6;
  zoomFactor: 0.05;
  startAnchor: [0, 12];

  cycleDuration: 6;
  localT: t - math.Floor(t / cycleDuration) * cycleDuration;
  angle: localT / cycleDuration * 2 * math.Pi;
  progress: (1 - math.Cos(angle)) / 2;

  walkBase:
  {
    direction: "front";
    leftLeg: { sign: -1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: 1; };
  };

  startProfile: character.zoomWalk(startAnchor, walkBase, verticalDistance, strideLength, 0, zoomFactor);
  endProfile: character.zoomWalk(startAnchor, walkBase, verticalDistance, strideLength, 1, zoomFactor);

  startFootMinY: math.Min(startProfile.anchor[1] + startProfile.leftLeg.end[1], startProfile.anchor[1] + startProfile.rightLeg.end[1]);
  endFootMinY: math.Min(endProfile.anchor[1] + endProfile.leftLeg.end[1], endProfile.anchor[1] + endProfile.rightLeg.end[1]);
  minFootY: math.Min(startFootMinY, endFootMinY);

  startHeadMaxY: startProfile.anchor[1] + startProfile.height + startProfile.neckLength + startProfile.headRadius;
  endHeadMaxY: endProfile.anchor[1] + endProfile.height + endProfile.neckLength + endProfile.headRadius;
  maxHeadY: math.Max(startHeadMaxY, endHeadMaxY);

  profile: character.zoomWalk(startAnchor, walkBase, verticalDistance, strideLength, progress, zoomFactor);
  geometry: character.skeleton.build(profile.anchor, profile);
  actor: character.static(profile.anchor, profile, palette);

  sign: math.Sign(verticalDistance);
  distanceAbs: math.Abs(verticalDistance);
  strideAbs: math.Abs(strideLength);
  strideCount: math.Floor(distanceAbs / strideAbs) + 1;

  endAnchor: [startAnchor[0], startAnchor[1] + verticalDistance];
  minAnchorY: math.Min(startAnchor[1], endAnchor[1]);
  maxAnchorY: math.Max(startAnchor[1], endAnchor[1]);

  view:
  {
    left: -22;
    bottom: math.Min(minAnchorY, minFootY) - 20;
    right: 22;
    top: math.Max(maxAnchorY, maxHeadY) + 12;
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

  anchorPath:
  {
    type: "line";
    from: startAnchor;
    to: endAnchor;
    stroke: "#1e293b";
    width: 0.25;
  };

  strideMarks:
    Range(0, strideCount) map (k, idx) =>
    {
      y: startAnchor[1] + strideAbs * sign * k;
      eval
      {
        type: "circle";
        center: [startAnchor[0], y];
        radius: 0.35;
        stroke: "#475569";
        width: 0.15;
      };
    };

  cursor:
  {
    type: "line";
    from: [view.left, profile.anchor[1]];
    to: [view.right, profile.anchor[1]];
    stroke: "#94a3b8";
    width: 0.15;
  };

  anchorMarker:
  {
    type: "circle";
    center: profile.anchor;
    radius: 0.35;
    fill: "#94a3b8";
    stroke: "none";
    width: 0;
  };

  feetMarkers:
  [
    { type: "circle"; center: geometry.leftLeg.to; radius: 0.55; fill: "#f472b6"; stroke: "#0f172a"; width: 0.15; },
    { type: "circle"; center: geometry.rightLeg.to; radius: 0.55; fill: "#f472b6"; stroke: "#0f172a"; width: 0.15; }
  ];

  label:
  {
    type: "text";
    text: join(["zoomWalk progress=", format(progress, "0.00")], "");
    position: [view.left + 2, view.top - 4];
    fontSize: 2.2;
    color: "#94a3b8";
  };

  eval
  {
    view;
    graphics: [background, anchorPath, strideMarks, cursor, anchorMarker, label] + feetMarkers + actor;
  };
}

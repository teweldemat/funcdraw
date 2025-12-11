{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  stepDuration: 1.5;
  dotSpacing: 6;
  dotStartX: -6;
  dotY: -14;
  dotAtIndex: (idx) => [dotStartX + dotSpacing * idx, dotY];

  startAnchor: [-3, 0];
  startLeftIndex: 0;
  startRightIndex: 1;
  initialLeftWorld: dotAtIndex(startLeftIndex);
  initialRightWorld: dotAtIndex(startRightIndex);

  resolveSteps: (count, anchorIn, leftWorld, rightWorld, movingLeft, leftIndex, rightIndex) =>
  {
    eval
    (
      if count <= 0 then
        { anchor: anchorIn; left: leftWorld; right: rightWorld; movingLeft; leftIndex; rightIndex; }
      else
      {
        targetIndex: (if movingLeft then rightIndex else leftIndex) + 1;
        target: dotAtIndex(targetIndex);
        moving: if movingLeft then "left" else "right";
        base:
        {
          leftLeg: { end: [leftWorld[0] - anchorIn[0], leftWorld[1] - anchorIn[1]]; };
          rightLeg: { end: [rightWorld[0] - anchorIn[0], rightWorld[1] - anchorIn[1]]; };
        };
        profile: package("@funcdraw/testlib").cartoon.character.singleStepProfile(anchorIn, base, moving, target, 1);
        nextAnchor: profile.anchor;
        nextLeft: [nextAnchor[0] + profile.leftLeg.end[0], nextAnchor[1] + profile.leftLeg.end[1]];
        nextRight: [nextAnchor[0] + profile.rightLeg.end[0], nextAnchor[1] + profile.rightLeg.end[1]];
        nextLeftIndex: if movingLeft then targetIndex else leftIndex;
        nextRightIndex: if movingLeft then rightIndex else targetIndex;

        eval resolveSteps(count - 1, nextAnchor, nextLeft, nextRight, if movingLeft then false else true, nextLeftIndex, nextRightIndex);
      }
    );
  };

  stepIndex: math.Floor(t / stepDuration);
  stepProgressRaw: t / stepDuration - stepIndex;
  stepProgress: (1 - math.Cos(stepProgressRaw * math.Pi)) / 2;

  completedState: resolveSteps(stepIndex, startAnchor, initialLeftWorld, initialRightWorld, true, startLeftIndex, startRightIndex);

  currentTargetIndex: (if completedState.movingLeft then completedState.rightIndex else completedState.leftIndex) + 1;
  currentTarget: dotAtIndex(currentTargetIndex);
  movingNow: if completedState.movingLeft then "left" else "right";
  currentBase:
  {
    handPhaseOffset: stepIndex;
    leftLeg: {sign:1, end: [completedState.left[0] - completedState.anchor[0], completedState.left[1] - completedState.anchor[1]]; };
    rightLeg: {sign:1, end: [completedState.right[0] - completedState.anchor[0], completedState.right[1] - completedState.anchor[1]]; };
    leftHand:{sign:-1},
    rightHand:{sign:-1},
  };
  profile: package("@funcdraw/testlib").cartoon.character.singleStepProfile(completedState.anchor, currentBase, movingNow, currentTarget, stepProgress);
  
  

  character: package("@funcdraw/testlib").cartoon.character.static(profile.anchor, profile, palette);

  markerStartIndex: math.Min(completedState.leftIndex, completedState.rightIndex) - 1;
  dotMarkers:
  [
    { type: "circle"; center: dotAtIndex(markerStartIndex); radius: 0.5; stroke: "#fbbf24"; width: 0.2; },
    { type: "circle"; center: dotAtIndex(markerStartIndex + 1); radius: 0.5; stroke: "#fbbf24"; width: 0.2; },
    { type: "circle"; center: dotAtIndex(markerStartIndex + 2); radius: 0.5; stroke: "#fbbf24"; width: 0.2; },
    { type: "circle"; center: dotAtIndex(markerStartIndex + 3); radius: 0.5; stroke: "#fbbf24"; width: 0.2; },
    { type: "circle"; center: dotAtIndex(markerStartIndex + 4); radius: 0.5; stroke: "#fbbf24"; width: 0.2; },
    { type: "circle"; center: dotAtIndex(markerStartIndex + 5); radius: 0.5; stroke: "#fbbf24"; width: 0.2; }
  ];

  eval
  {
    view:
    {
      left: profile.anchor[0] - 20;
      bottom: -22;
      right: profile.anchor[0] + 20;
      top: 26;
    };
    graphics: character + dotMarkers;
  };
}

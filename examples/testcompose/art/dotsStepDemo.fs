{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31";
  };

  stepDuration: 0.5;
  dotSpacing: 6;
  dotStartX: -6;
  dotY: -14;
  dotAtIndex: (idx) => [dotStartX + dotSpacing * idx, dotY];

  startAnchor: [-3, 0];
  startLeftIndex: 0;
  startRightIndex: 1;

  stepIndex: math.Floor(t / stepDuration);
  stepProgressRaw: t / stepDuration - stepIndex;
  stepProgress: (1 - math.Cos(stepProgressRaw * math.Pi)) / 2;

  isEvenStep: math.Floor(stepIndex / 2) * 2 == stepIndex;
  leftIndex: if isEvenStep then startLeftIndex + stepIndex else startLeftIndex + stepIndex + 1;
  rightIndex: if isEvenStep then startRightIndex + stepIndex else startRightIndex + stepIndex - 1;
  anchor: [startAnchor[0] + dotSpacing * stepIndex, startAnchor[1]];
  leftWorld: dotAtIndex(leftIndex);
  rightWorld: dotAtIndex(rightIndex);
  completedState: { anchor; left: leftWorld; right: rightWorld; movingLeft: isEvenStep; leftIndex; rightIndex; };

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
    direction:'right',
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

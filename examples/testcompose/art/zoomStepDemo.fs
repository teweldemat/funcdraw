{
  character: package("@funcdraw/testlib").cartoon.character;

  palette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  stepDuration: 0.7;
  zoomFactor: 0.05;

  baseFootY: -14;
  strideY: 1.2;
  span: 6;
  cycle: span * 2;

  yAtIndex: (idx) =>
  {
    phase: idx - (idx div cycle) * cycle;
    tri: if phase <= span then phase else cycle - phase;
    eval baseFootY + tri * strideY;
  };

  loopSteps: 18;
  loopDuration: stepDuration * loopSteps;
  localT: t - math.Floor(t / loopDuration) * loopDuration;

  stepIndex: math.Floor(localT / stepDuration);
  stepProgressRaw: localT / stepDuration - stepIndex;
  stepProgress: (1 - math.Cos(stepProgressRaw * math.Pi)) / 2;

  startLeftIndex: 0;
  startRightIndex: 1;

  defaults: character.skeleton.defaults;
  startAnchor: [0, 0];
  startLeftY: yAtIndex(startLeftIndex);
  startRightY: yAtIndex(startRightIndex);
  startProfile:
    defaults + {
      direction: "front";
      leftLeg: defaults.leftLeg + { end: [0, startLeftY - startAnchor[1]]; };
      rightLeg: defaults.rightLeg + { end: [0, startRightY - startAnchor[1]]; };
      leftHand: defaults.leftHand + { end: [0, -(defaults.leftHand.upper + defaults.leftHand.lower)]; };
      rightHand: defaults.rightHand + { end: [0, -(defaults.rightHand.upper + defaults.rightHand.lower)]; };
    };
  seed:
  {
    anchor: startAnchor;
    leftIndex: startLeftIndex;
    rightIndex: startRightIndex;
    profile: startProfile;
  };
  stepOnce: (state, k) =>
  {
    isEven: (k div 2) * 2 == k;
    moving: if isEven then "left" else "right";
    targetIdx: (if moving == "left" then state.rightIndex else state.leftIndex) + 1;
    nextProfile: character.singleStepZoom(state.anchor, state.profile, moving, yAtIndex(targetIdx), 1, zoomFactor);
    nextLeft: if moving == "left" then targetIdx else state.leftIndex;
    nextRight: if moving == "right" then targetIdx else state.rightIndex;
    eval
    {
      anchor: nextProfile.anchor;
      leftIndex: nextLeft;
      rightIndex: nextRight;
      profile: nextProfile;
    };
  };
  completed:
    Range(0, stepIndex) reduce (state, k) =>
      stepOnce(state, k)
    ~ seed;

  isEvenStep: math.Floor(stepIndex / 2) * 2 == stepIndex;
  movingNow: if isEvenStep then "left" else "right";
  targetIndex: (if movingNow == "left" then completed.rightIndex else completed.leftIndex) + 1;
  targetY: yAtIndex(targetIndex);

  profile: character.singleStepZoom(completed.anchor, completed.profile, movingNow, targetY, stepProgress, zoomFactor);
  geometry: character.skeleton.build(profile.anchor, profile);
  actor: character.static(profile.anchor, profile, palette);

  view:
  {
    left: -20;
    bottom: -26;
    right: 20;
    top: 34;
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

  targetLine:
  {
    type: "line";
    from: [view.left, targetY];
    to: [view.right, targetY];
    stroke: "#334155";
    width: 0.1;
  };

  feetMarkers:
  [
    { type: "circle"; center: geometry.leftLeg.to; radius: 0.55; fill: "#f472b6"; stroke: "#0f172a"; width: 0.15; },
    { type: "circle"; center: geometry.rightLeg.to; radius: 0.55; fill: "#f472b6"; stroke: "#0f172a"; width: 0.15; }
  ];

  eval
  {
    view;
    graphics: [background, targetLine] + feetMarkers + actor;
  };
}

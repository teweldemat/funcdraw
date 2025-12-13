(state) =>
{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  character: package("@funcdraw/testlib").cartoon.character;

  skinName: if state == null then "stick" else state.skin;
  skin: if skinName == "stick" then character.skins.stick else if skinName == "poly" then character.skins.poly else error("expected skin stick|poly");

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
  profile: character.singleStepProfile(completedState.anchor, currentBase, movingNow, currentTarget, stepProgress);

  view:
  {
    left: profile.anchor[0] - 20;
    bottom: -22;
    right: profile.anchor[0] + 20;
    top: 26;
  };

  characterGraphics: character.static(profile.anchor, profile, palette, skin);

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

  button: package("@funcdraw/testlib").ui.button;
  stickButtonState: if state == null then null else state.stickButton;
  polyButtonState: if state == null then null else state.polyButton;

  buttonSize: [9, 4];
  buttonGap: 0.75;
  buttonMargin: 1.25;
  buttonX: view.right - buttonMargin - buttonSize[0];
  stickButtonY: view.top - buttonMargin - buttonSize[1];
  polyButtonY: stickButtonY - buttonSize[1] - buttonGap;

  stickButton:
    button(
      { position: [buttonX, stickButtonY]; size: buttonSize; label: "stick"; },
      stickButtonState);
  polyButton:
    button(
      { position: [buttonX, polyButtonY]; size: buttonSize; label: "poly"; },
      polyButtonState);

  selectionOutline:
  {
    type: "rect";
    position: if skinName == "stick" then [buttonX, stickButtonY] else [buttonX, polyButtonY];
    size: buttonSize;
    fill: "#00000000";
    stroke: "#fbbf24";
    width: 0.8;
  };

  stepper: (event) =>
  {
    stickStep: stickButton.step(event);
    polyStep: polyButton.step(event);

    stickClicked: stickStep != null and Len(stickStep.events) > 0;
    polyClicked: polyStep != null and Len(polyStep.events) > 0;
    nextSkinName: if stickClicked then "stick" else if polyClicked then "poly" else skinName;

    eval
    if stickStep == null and polyStep == null and nextSkinName == skinName then null else
    {
      state:
      {
        skin: nextSkinName;
        stickButton: if stickStep == null then stickButtonState else stickStep.state;
        polyButton: if polyStep == null then polyButtonState else polyStep.state;
      };
      events:
        (if stickStep == null then [] else stickStep.events)
        + (if polyStep == null then [] else polyStep.events);
    };
  };

  eval
  {
    view;
    graphics: characterGraphics
      + dotMarkers
      + stickButton.graphics
      + polyButton.graphics
      + [selectionOutline];
    step: stepper;
  };
}

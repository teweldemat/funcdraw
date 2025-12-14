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

  directionName:
    if state == null then "right"
    else if state.direction == null then "right"
    else state.direction;

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
    direction: directionName,
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
  upButtonState: if state == null then null else state.upButton;
  downButtonState: if state == null then null else state.downButton;
  leftButtonState: if state == null then null else state.leftButton;
  rightButtonState: if state == null then null else state.rightButton;

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

  arrowStroke: "#e2e8f0";
  arrowWidth: 0.14;
  rightArrow:
  [
    { type: "line"; from: [-0.55, 0]; to: [0.55, 0]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [0.55, 0]; to: [0.2, 0.25]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [0.55, 0]; to: [0.2, -0.25]; stroke: arrowStroke; width: arrowWidth; }
  ];
  leftArrow:
  [
    { type: "line"; from: [0.55, 0]; to: [-0.55, 0]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [-0.55, 0]; to: [-0.2, 0.25]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [-0.55, 0]; to: [-0.2, -0.25]; stroke: arrowStroke; width: arrowWidth; }
  ];
  upArrow:
  [
    { type: "line"; from: [0, -0.55]; to: [0, 0.55]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [0, 0.55]; to: [0.25, 0.2]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [0, 0.55]; to: [-0.25, 0.2]; stroke: arrowStroke; width: arrowWidth; }
  ];
  downArrow:
  [
    { type: "line"; from: [0, 0.55]; to: [0, -0.55]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [0, -0.55]; to: [0.25, -0.2]; stroke: arrowStroke; width: arrowWidth; },
    { type: "line"; from: [0, -0.55]; to: [-0.25, -0.2]; stroke: arrowStroke; width: arrowWidth; }
  ];

  dirButtonSize: [4, 4];
  dirGap: 0.4;
  dirPadWidth: dirButtonSize[0] * 3 + dirGap * 2;
  dirPadRight: view.right - buttonMargin;
  dirPadLeft: dirPadRight - dirPadWidth;
  dirPadTop: polyButtonY - buttonGap;

  dirRowTopY: dirPadTop - dirButtonSize[1];
  dirRowMidY: dirRowTopY - dirButtonSize[1] - dirGap;
  dirRowBotY: dirRowMidY - dirButtonSize[1] - dirGap;

  dirColLeftX: dirPadLeft;
  dirColMidX: dirPadLeft + dirButtonSize[0] + dirGap;
  dirColRightX: dirPadLeft + (dirButtonSize[0] + dirGap) * 2;

  upButton:
    button(
      { position: [dirColMidX, dirRowTopY]; size: dirButtonSize; graphics: upArrow; },
      upButtonState);
  leftButton:
    button(
      { position: [dirColLeftX, dirRowMidY]; size: dirButtonSize; graphics: leftArrow; },
      leftButtonState);
  rightButton:
    button(
      { position: [dirColRightX, dirRowMidY]; size: dirButtonSize; graphics: rightArrow; },
      rightButtonState);
  downButton:
    button(
      { position: [dirColMidX, dirRowBotY]; size: dirButtonSize; graphics: downArrow; },
      downButtonState);

  selectionOutline:
  {
    type: "rect";
    position: if skinName == "stick" then [buttonX, stickButtonY] else [buttonX, polyButtonY];
    size: buttonSize;
    fill: "#00000000";
    stroke: "#fbbf24";
    width: 0.8;
  };

  directionOutline:
  {
    type: "rect";
    position:
      if directionName == "left" then [dirColLeftX, dirRowMidY]
      else if directionName == "right" then [dirColRightX, dirRowMidY]
      else if directionName == "front" then [dirColMidX, dirRowBotY]
      else if directionName == "back" then [dirColMidX, dirRowTopY]
      else error("expected direction left|right|front|back");
    size: dirButtonSize;
    fill: "#00000000";
    stroke: "#fbbf24";
    width: 0.8;
  };

  directionLabel:
  {
    type: "text";
    text: "direction: " + directionName;
    position: [dirPadLeft, dirPadTop + 1.1];
    fontSize: 1.2;
    color: "#94a3b8";
  };

  stepper: (event) =>
  {
    stickStep: stickButton.step(event);
    polyStep: polyButton.step(event);
    upStep: upButton.step(event);
    downStep: downButton.step(event);
    leftStep: leftButton.step(event);
    rightStep: rightButton.step(event);

    stickClicked: stickStep != null and Len(stickStep.events) > 0;
    polyClicked: polyStep != null and Len(polyStep.events) > 0;
    nextSkinName: if stickClicked then "stick" else if polyClicked then "poly" else skinName;

    upClicked: upStep != null and Len(upStep.events) > 0;
    downClicked: downStep != null and Len(downStep.events) > 0;
    leftClicked: leftStep != null and Len(leftStep.events) > 0;
    rightClicked: rightStep != null and Len(rightStep.events) > 0;
    nextDirectionName:
      if leftClicked then "left"
      else if rightClicked then "right"
      else if upClicked then "back"
      else if downClicked then "front"
      else directionName;

    eval
    if stickStep == null and polyStep == null and upStep == null and downStep == null and leftStep == null and rightStep == null and nextSkinName == skinName and nextDirectionName == directionName then null else
    {
      state:
      {
        skin: nextSkinName;
        direction: nextDirectionName;
        stickButton: if stickStep == null then stickButtonState else stickStep.state;
        polyButton: if polyStep == null then polyButtonState else polyStep.state;
        upButton: if upStep == null then upButtonState else upStep.state;
        downButton: if downStep == null then downButtonState else downStep.state;
        leftButton: if leftStep == null then leftButtonState else leftStep.state;
        rightButton: if rightStep == null then rightButtonState else rightStep.state;
      };
      events:
        (if stickStep == null then [] else stickStep.events)
        + (if polyStep == null then [] else polyStep.events)
        + (if upStep == null then [] else upStep.events)
        + (if downStep == null then [] else downStep.events)
        + (if leftStep == null then [] else leftStep.events)
        + (if rightStep == null then [] else rightStep.events);
    };
  };

  eval
  {
    view;
    graphics: characterGraphics
      + dotMarkers
      + stickButton.graphics
      + polyButton.graphics
      + upButton.graphics
      + downButton.graphics
      + leftButton.graphics
      + rightButton.graphics
      + [selectionOutline, directionOutline, directionLabel];
    step: stepper;
  };
}

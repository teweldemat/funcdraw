(localT) =>
{
  view: common.resolveView();
  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  startAnchor: actor.doorCharacterAnchor(layout);

  toRoadDistance: common.sidewalkTopY - common.yardTopY;
  stride: 6;
  zoomFactor: 0.018;
  walkwayDx: 12;

  walkDownProgress: if localT < common.walkToRoadDuration then common.ease01(localT / common.walkToRoadDuration) else 1;
  toRoadBase: actor.characterMeasurements + actor.frontPose;
  toRoadProfileBase: actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, walkDownProgress, zoomFactor);
  toRoadProfile: toRoadProfileBase + { anchor: [startAnchor[0] + walkwayDx * walkDownProgress, toRoadProfileBase.anchor[1]]; };
  toRoadEndBase: actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, 1, zoomFactor);
  toRoadEnd: toRoadEndBase + { anchor: [startAnchor[0] + walkwayDx, toRoadEndBase.anchor[1]]; };

  rightPose:
  {
    direction: "right";
    leftLeg: { sign: 1; };
    rightLeg: { sign: 1; };
    leftHand: { sign: -1; };
    rightHand: { sign: -1; };
  };

  turnT: localT - common.walkToRoadDuration;
  turnProgress:
    if turnT < 0 then 0
    else if turnT > common.turnDuration then 1
    else common.ease01(turnT / common.turnDuration);
  turnDirection: if turnProgress < 0.5 then "front" else "right";
  turned:
    if turnDirection == "front" then toRoadEnd + actor.frontPose
    else toRoadEnd + rightPose;

  acrossT: localT - common.walkToRoadDuration - common.turnDuration;
  acrossProgress:
    if acrossT < 0 then 0
    else if acrossT > common.acrossDuration then 1
    else common.ease01(acrossT / common.acrossDuration);
  acrossDistance: view.right - (toRoadEnd.anchor[0] - 2) + 30;
  acrossBase: toRoadEnd + rightPose;
  acrossProfile: actor.character.profileWalk(toRoadEnd.anchor, acrossBase, acrossDistance, stride, acrossProgress);

  stage:
    if localT < common.walkToRoadDuration then "toRoad"
    else if localT < common.walkToRoadDuration + common.turnDuration then "turn"
    else if localT < common.walkToRoadDuration + common.turnDuration + common.acrossDuration then "across"
    else "end";

  current:
    if stage == "toRoad" then toRoadProfile
    else if stage == "turn" then turned
    else acrossProfile;

  characterGraphic: actor.character.static(current.anchor, current, actor.charPalette,actor.skin);

  doorOpen: if stage == "toRoad" then 1 else if stage == "turn" then 0.85 else 0.4;
  houseGraphics:
    common.house.types.cottage(
      {
        anchor: common.houseAnchor;
        width: common.houseWidth;
        stories: common.houseStories;
        doorOpen: doorOpen;
        lightColor: "#0f172a";
      });

  endT: localT - common.walkToRoadDuration - common.turnDuration - common.acrossDuration;
  showEnd: endT > 0;
  endAlpha:
    if endT < 0 then 0
    else if endT > common.endDuration then 1
    else common.ease01(endT / common.endDuration);
  endFill: fd.color.alpha("#0f172a", endAlpha);
  endCenterX: (view.left + view.right) / 2;
  endLabel:
    {
      type: "text";
      text: "THE END";
      position: [endCenterX, 25];
      align: "center";
      fontSize: 18;
      color: endFill;
    };

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [houseGraphics, characterGraphic] + (if showEnd then [endLabel] else []);
  };
}

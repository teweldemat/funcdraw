(localT) =>
{
  transport: package("@funcdraw/testlib").cartoon.transport;

  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  startAnchor: actor.doorCharacterAnchor(layout);

  toRoadDistance: common.sidewalkTopY - common.yardTopY;
  stride: 6;
  zoomFactor: 0.018;
  walkwayDx: 12;

  walkDownProgress: if localT < common.walkToRoadDuration then localT / common.walkToRoadDuration else 1;
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

  toStopT: localT - common.walkToRoadDuration - common.turnDuration;
  toStopProgress:
    if toStopT < 0 then 0
    else if toStopT > common.walkToStopDuration then 1
    else toStopT / common.walkToStopDuration;
  toStopDistance: common.busStopX - toRoadEnd.anchor[0];
  toStopBase: toRoadEnd + rightPose;
  toStopProfile: actor.character.profileWalk(toRoadEnd.anchor, toStopBase, toStopDistance, stride, toStopProgress);

  stage:
    if localT < common.walkToRoadDuration then "toRoad"
    else if localT < common.walkToRoadDuration + common.turnDuration then "turn"
    else "toStop";

  current:
    if stage == "toRoad" then toRoadProfile
    else if stage == "turn" then turned
    else toStopProfile;

  viewCenterX: current.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: transport.busStopSign(common.busStopSign);

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

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [houseGraphics, stopSign, characterGraphic];
  };
}

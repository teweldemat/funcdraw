(localT) =>
{
  stage:
    if localT < common.walkToRoadDuration then "toRoad"
    else if localT < common.walkToRoadDuration + common.turnDuration then "turn"
    else if localT < common.walkToRoadDuration + common.turnDuration + common.walkToCrossingDuration then "scene3a_walk_to_crossing"
    else if localT < common.walkToRoadDuration + common.turnDuration + common.walkToCrossingDuration + common.crossZebraDuration then "scene3b_cross_zebra"
    else "scene3c_walk_to_stop_far_side";

  stageT:
    if stage == "toRoad" then localT
    else if stage == "turn" then localT - common.walkToRoadDuration
    else if stage == "scene3a_walk_to_crossing" then localT - common.walkToRoadDuration - common.turnDuration
    else if stage == "scene3b_cross_zebra" then localT - common.walkToRoadDuration - common.turnDuration - common.walkToCrossingDuration
    else localT - common.walkToRoadDuration - common.turnDuration - common.walkToCrossingDuration - common.crossZebraDuration;

  eval if stage == "toRoad" then
  {
    transport: package("@funcdraw/testlib").cartoon.transport;

    layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
    startAnchor: actor.doorCharacterAnchor(layout);

    toRoadDistance: common.sidewalkTopY - common.yardTopY;
    stride: common.walkStride;
    zoomFactor: 0.018;

    walkDownProgress: stageT / common.walkToRoadDuration;
    toRoadBase: actor.characterMeasurements + actor.frontPose;
    toRoadProfileBase: actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, walkDownProgress, zoomFactor);
    current: toRoadProfileBase + { anchor: [startAnchor[0], toRoadProfileBase.anchor[1]]; };

    viewCenterX: current.anchor[0];
    view: common.resolveViewAt(viewCenterX);
    stopSign: transport.busStopSign(common.busStopSign);
    characterGraphic: actor.character.static(current.anchor, current, actor.charPalette,actor.skin);
    houseGraphics:
      common.house.types.cottage(
        {
          anchor: common.houseAnchor;
          width: common.houseWidth;
          stories: common.houseStories;
          doorOpen: 1;
          lightColor: "#0f172a";
        });
    eval { view; graphics: backdrop(view, 1, t) + [houseGraphics, stopSign, characterGraphic]; };
  }
  else if stage == "turn" then
  {
    transport: package("@funcdraw/testlib").cartoon.transport;

    layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
    startAnchor: actor.doorCharacterAnchor(layout);

    toRoadDistance: common.sidewalkTopY - common.yardTopY;
    stride: common.walkStride;
    zoomFactor: 0.018;
    walkwayDx: common.walkwayDx;

    toRoadBase: actor.characterMeasurements + actor.frontPose;
    toRoadEndBase: actor.crouchProfile(actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, 1, zoomFactor));

    turnProgress:
      if stageT > common.turnDuration then 1
      else common.ease01(stageT / common.turnDuration);
    turnAnchorX: startAnchor[0] + walkwayDx * turnProgress;
    turnBase: toRoadEndBase + { anchor: [turnAnchorX, toRoadEndBase.anchor[1]]; };
    turnDirection: if turnProgress < 0.5 then "front" else "right";
    current:
      if turnDirection == "front" then turnBase + actor.frontPose
      else turnBase + actor.rightPose;

    viewCenterX: current.anchor[0];
    view: common.resolveViewAt(viewCenterX);
    stopSign: transport.busStopSign(common.busStopSign);
    characterGraphic: actor.character.static(current.anchor, current, actor.charPalette,actor.skin);
    houseGraphics:
      common.house.types.cottage(
        {
          anchor: common.houseAnchor;
          width: common.houseWidth;
          stories: common.houseStories;
          doorOpen: 0.85;
          lightColor: "#0f172a";
        });
    eval { view; graphics: backdrop(view, 1, t) + [houseGraphics, stopSign, characterGraphic]; };
  }
  else if stage == "scene3a_walk_to_crossing" then scene3a_walk_to_crossing(stageT)
  else if stage == "scene3b_cross_zebra" then scene3b_cross_zebra(stageT)
  else scene3c_walk_to_stop_far_side(stageT);
}

(localT) =>
{
  transport: package("@funcdraw/testlib").cartoon.transport;

  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  startAnchor: actor.doorCharacterAnchor(layout);

  toRoadDistance: common.sidewalkTopY - common.yardTopY;
  stride: common.walkStride;
  zoomFactor: 0.018;
  walkwayDx: common.walkwayDx;

  toRoadBase: actor.characterMeasurements + actor.frontPose;
  toRoadEndBase: actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, 1, zoomFactor);
  toRoadEnd: toRoadEndBase + { anchor: [startAnchor[0] + walkwayDx, toRoadEndBase.anchor[1]]; };

  targetX: common.zebraCrossing.centerX;
  toCrossDistance: targetX - toRoadEnd.anchor[0];
  base: toRoadEnd + actor.rightPose;
  atCross: actor.character.profileWalk(toRoadEnd.anchor, base, toCrossDistance, stride, 1);
  crossBase: atCross + actor.backPose;

  crossDistance: common.roadBottomY - common.sidewalkTopY;
  crossZoomFactor: 0.0008;
  crossedBack: actor.character.zoomWalk(atCross.anchor, crossBase, crossDistance, stride, 1, crossZoomFactor);
  crossed: crossedBack + actor.rightPose;

  walkProgress:
    if localT < common.walkToStopFarSideDuration then localT / common.walkToStopFarSideDuration
    else 1;

  toStopDistance: common.busStopX - crossed.anchor[0];
  current: actor.character.profileWalk(crossed.anchor, crossed, toStopDistance, stride, walkProgress);

  viewCenterX: current.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: transport.busStopSign(common.busStopSign);

  characterGraphic: actor.character.static(current.anchor, current, actor.charPalette, actor.skin);

  houseGraphics:
    common.house.types.cottage(
      {
        anchor: common.houseAnchor;
        width: common.houseWidth;
        stories: common.houseStories;
        doorOpen: 0.1;
        lightColor: "#0f172a";
      });

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [houseGraphics, stopSign, characterGraphic];
  };
}

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
  toRoadEndBase: actor.crouchProfile(actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, 1, zoomFactor));
  toRoadEnd: toRoadEndBase + { anchor: [startAnchor[0] + walkwayDx, toRoadEndBase.anchor[1]]; };

  targetX: common.zebraCrossing.centerX;
  toCrossDistance: targetX - toRoadEnd.anchor[0];
  base: toRoadEnd + actor.rightPose;
  atCross: actor.character.profileWalk(toRoadEnd.anchor, base, toCrossDistance, stride, 1);
  crossBase: atCross + actor.backPose;

  crossProgress:
    if localT < common.crossZebraDuration then localT / common.crossZebraDuration
    else 1;

  crossDistance: common.roadBottomY - common.sidewalkTopY;
  crossZoomFactor: 0.0008;
  crossed: actor.crouchProfile(actor.character.zoomWalk(atCross.anchor, crossBase, crossDistance, stride, crossProgress, crossZoomFactor));

  viewCenterX: crossed.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: transport.busStopSign(common.busStopSign);

  characterGraphic: actor.character.static(crossed.anchor, crossed, actor.charPalette, actor.skin);

  houseGraphics:
    common.house.types.cottage(
      {
        anchor: common.houseAnchor;
        width: common.houseWidth;
        stories: common.houseStories;
        doorOpen: 0.2;
        lightColor: "#0f172a";
      });

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [houseGraphics, stopSign, characterGraphic];
  };
}

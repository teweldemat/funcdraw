(localT) =>
{
  transport: package("@funcdraw/testlib").cartoon.transport;
  city: package("@funcdraw/testlib").cartoon.city;

  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  startAnchor: actor.doorCharacterAnchor(layout);

  toRoadDistance: common.sidewalkTopY - common.yardTopY;
  stride: common.walkStride;
  zoomFactor: 0.018;
  walkwayDx: common.walkwayDx;

  toRoadBase: actor.characterMeasurements + actor.frontPose;
  toRoadEndBase: actor.crouchProfile(actor.character.zoomWalk(startAnchor, toRoadBase, toRoadDistance, stride, 1, zoomFactor));
  toRoadEnd: toRoadEndBase + { anchor: [startAnchor[0] + walkwayDx, toRoadEndBase.anchor[1]]; };

  walkProgress:
    if localT < common.walkToCrossingDuration then localT / common.walkToCrossingDuration
    else 1;

  targetX: common.zebraCrossing.centerX;
  toCrossDistance: targetX - toRoadEnd.anchor[0];
  base: toRoadEnd + actor.rightPose;
  current: actor.crouchProfile(actor.character.profileWalk(toRoadEnd.anchor, base, toCrossDistance, stride, walkProgress));

  viewCenterX: current.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: city.busStopSign(common.busStopSign);

  characterGraphic: actor.character.static(current.anchor, current, actor.charPalette, actor.skin);

  houseGraphics:
    common.house.types.cottage(
      {
        anchor: common.houseAnchor;
        width: common.houseWidth;
        stories: common.houseStories;
        doorOpen: 0.4;
        lightColor: "#0f172a";
      });

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [houseGraphics, stopSign, characterGraphic];
  };
}

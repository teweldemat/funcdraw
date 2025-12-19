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

  crossingX: common.zebraCrossing.centerX;
  toCrossDistance: crossingX - toRoadEnd.anchor[0];
  toCrossBase: toRoadEnd + actor.rightPose;
  atCross: actor.character.profileWalk(toRoadEnd.anchor, toCrossBase, toCrossDistance, stride, 1);
  crossBase: atCross + actor.backPose;

  crossDistance: common.roadBottomY - common.sidewalkTopY;
  crossZoomFactor: 0.0008;
  crossedBack: actor.crouchProfile(actor.character.zoomWalk(atCross.anchor, crossBase, crossDistance, stride, 1, crossZoomFactor));
  crossed: crossedBack + actor.rightPose;

  toStopDistance: common.busStopX - crossed.anchor[0];
  stopped: actor.character.profileWalk(crossed.anchor, crossed, toStopDistance, stride, 1);

  straighten: (limb) => limb + { end: [0, -(limb.upper + limb.lower)]; };
  standing:
    stopped + {
      leftLeg: straighten(stopped.leftLeg);
      rightLeg: straighten(stopped.rightLeg);
      leftHand: straighten(stopped.leftHand);
      rightHand: straighten(stopped.rightHand);
    };

  posedStanding:
    if localT < common.waitForBusDuration + common.busArriveDuration then standing + actor.backPose
    else standing;

  viewCenterX: standing.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: city.busStopSign(common.busStopSign);

  arriveT: localT - common.waitForBusDuration;
  arriveProgress:
    if arriveT < 0 then 0
    else if arriveT > common.busArriveDuration then 1
    else common.ease01(arriveT / common.busArriveDuration);

  doorT: arriveT - common.busArriveDuration;
  doorOpen:
    if doorT < 0 then 0
    else if doorT > common.busDoorOpenDuration then 1
    else common.ease01(doorT / common.busDoorOpenDuration);

  busW: common.bus.size[0];
  busDoorCenterOffset: busW * 0.15;
  stopDoorCenterX: standing.anchor[0] + 4;
  busStopX: stopDoorCenterX - busDoorCenterOffset;

  busH: common.bus.size[1];
  wheelRadius: busH * 0.18;
  busY: common.roadBottomY + wheelRadius * 1.4;
  busStartX: view.left - busW - 30;
  busX: busStartX + (busStopX - busStartX) * arriveProgress;
  busAnchor: [busX, busY];

  busGraphics:
    transport.bus(
      {
        anchor: busAnchor;
        size: common.bus.size;
        doorOpen: doorOpen;
        fill: common.bus.fill;
        stroke: common.bus.stroke;
        width: common.bus.width;
      });

  characterGraphic: actor.character.static(posedStanding.anchor, posedStanding, actor.charPalette, actor.skin);

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + busGraphics + characterGraphic + stopSign;
  };
}

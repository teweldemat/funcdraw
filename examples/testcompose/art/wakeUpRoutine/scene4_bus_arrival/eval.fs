(localT) =>
{
  transport: package("@funcdraw/testlib").cartoon.transport;

  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  startAnchor: actor.doorCharacterAnchor(layout);

  toRoadDistance: common.sidewalkTopY - common.yardTopY;
  stride: 6;
  zoomFactor: 0.018;
  walkwayDx: 12;

  toRoadBase: actor.characterMeasurements + actor.frontPose;
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

  toStopDistance: common.busStopX - toRoadEnd.anchor[0];
  stopBase: toRoadEnd + rightPose;
  stopped: actor.character.profileWalk(toRoadEnd.anchor, stopBase, toStopDistance, stride, 1);

  straighten: (limb) => limb + { end: [0, -(limb.upper + limb.lower)]; };
  standing:
    stopped + {
      leftLeg: straighten(stopped.leftLeg);
      rightLeg: straighten(stopped.rightLeg);
      leftHand: straighten(stopped.leftHand);
      rightHand: straighten(stopped.rightHand);
    };

  viewCenterX: standing.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: transport.busStopSign(common.busStopSign);

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

  busY: common.roadBottomY + 4.5;
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

  characterGraphic: actor.character.static(standing.anchor, standing, actor.charPalette, actor.skin);

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [stopSign, busGraphics, characterGraphic];
  };
}


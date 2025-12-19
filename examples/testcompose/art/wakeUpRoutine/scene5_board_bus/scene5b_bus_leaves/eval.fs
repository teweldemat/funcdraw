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

  viewCenterX: standing.anchor[0] + common.followLookAhead;
  view: common.resolveViewAt(viewCenterX);

  stopSign: city.busStopSign(common.busStopSign);

  busW: common.bus.size[0];
  busH: common.bus.size[1];
  wheelRadius: busH * 0.18;

  busDoorCenterOffset: busW * 0.15;
  stopDoorCenterX: standing.anchor[0] + 4;
  busStopX: stopDoorCenterX - busDoorCenterOffset;
  busY: common.roadBottomY + wheelRadius * 1.4;

  departDuration: common.walkToSeatDuration + common.settleDuration;
  departProgress:
    if localT > departDuration then 1
    else common.ease01(localT / departDuration);

  busEndX: view.right + busW + 40;
  busX: busStopX + (busEndX - busStopX) * departProgress;
  busAnchor: [busX, busY];

  doorCloseDuration: common.settleDuration;
  doorOpen:
    if localT > doorCloseDuration then 0
    else 1 - common.ease01(localT / doorCloseDuration);

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

  windows: busGraphics filter (g) => g.name == "bus-window";
  glass: busGraphics filter (g) => g.name == "bus-window-glass";
  busRest: busGraphics filter (g) => g.name != "bus-window-glass";

  windowCenter: (w) => [w.position[0] + w.size[0] / 2, w.position[1] + w.size[1] / 2];
  startWindow: First(windows, (w) => w.index == 0);
  seatWindow: First(windows, (w) => w.index == 4);
  startHead: windowCenter(startWindow);
  seatHead: windowCenter(seatWindow);

  insideProgress:
    if localT > common.walkToSeatDuration then 1
    else localT / common.walkToSeatDuration;

  headPos:
  [
    startHead[0] + (seatHead[0] - startHead[0]) * insideProgress,
    startHead[1] + (seatHead[1] - startHead[1]) * insideProgress
  ];

  head:
  {
    type: "circle";
    name: "rider-head";
    center: headPos;
    radius: stopped.headRadius;
    fill: actor.charPalette.body;
    stroke: "none";
    width: 0;
    opacity: 1;
  };

  eval
  {
    view;
    graphics:
      backdrop(view, 1, t)
      + busRest
      + [head]
      + glass
      + stopSign
      + [];
  };
}

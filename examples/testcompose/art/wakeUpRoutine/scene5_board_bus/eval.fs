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

  busW: common.bus.size[0];
  busDoorCenterOffset: busW * 0.15;
  stopDoorCenterX: standing.anchor[0] + 4;
  busStopX: stopDoorCenterX - busDoorCenterOffset;
  busY: common.roadBottomY + 4.5;
  busAnchor: [busStopX, busY];

  busGraphics:
    transport.bus(
      {
        anchor: busAnchor;
        size: common.bus.size;
        doorOpen: 1;
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

  enterProgress:
    if localT < common.enterBusDuration then localT / common.enterBusDuration
    else 1;
  insideT: localT - common.enterBusDuration;
  insideProgress:
    if insideT < 0 then 0
    else if insideT > common.walkToSeatDuration then 1
    else insideT / common.walkToSeatDuration;

  settleT: insideT - common.walkToSeatDuration;
  settled: settleT > common.settleDuration;
  endT: settleT - common.settleDuration;
  endAlpha:
    if endT < 0 then 0
    else if endT > common.endDuration then 1
    else common.ease01(endT / common.endDuration);

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
    opacity: if localT < common.enterBusDuration then 0 else 1;
  };

  enterDistance: 8;
  entering: actor.character.profileWalk(standing.anchor, standing, enterDistance, 4, enterProgress);
  showOutside: localT < common.enterBusDuration;
  outsideGraphic: actor.character.static(entering.anchor, entering, actor.charPalette, actor.skin);

  endCenterX: (view.left + view.right) / 2;
  endLabel:
  {
    type: "text";
    text: "THE END";
    position: [endCenterX, 30];
    align: "center";
    fontSize: 24;
    color: fd.color.alpha("#0f172a", endAlpha);
  };

  eval
  {
    view;
    graphics:
      backdrop(view, 1, t)
      + [stopSign]
      + (if showOutside then [busRest, outsideGraphic] else [busRest])
      + [head]
      + [glass]
      + (if settled then [endLabel] else []);
  };
}

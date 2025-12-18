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

  stopSignFront: transport.busStopSign(common.busStopSign);
  stopSign:
    stopSignFront map (g) =>
      if g.name == "bus-stop-label" then g + { opacity: 0; }
      else if g.name == "bus-stop-sign" then g + { fill: "#cbd5e1"; }
      else g;

  busW: common.bus.size[0];
  busH: common.bus.size[1];
  wheelRadius: busH * 0.18;

  busDoorCenterOffset: busW * 0.15;
  stopDoorCenterX: standing.anchor[0] + 4;
  busStopX: stopDoorCenterX - busDoorCenterOffset;
  busY: common.roadBottomY + wheelRadius * 1.4;
  busAnchor: [busStopX, busY];

  baseView: common.resolveViewAt(busAnchor[0] + busDoorCenterOffset);
  zoom: 0.46;
  baseW: baseView.right - baseView.left;
  baseH: baseView.top - baseView.bottom;
  zoomW: baseW * zoom;
  zoomH: baseH * zoom;
  viewCenterX: busAnchor[0] + busDoorCenterOffset - zoomW * 0.05;
  viewCenterY: busAnchor[1] + busH * 0.48;
  view:
  {
    left: viewCenterX - zoomW / 2;
    right: viewCenterX + zoomW / 2;
    bottom: viewCenterY - zoomH / 2;
    top: viewCenterY + zoomH / 2;
  };

  enterProgress:
    if localT < common.enterBusDuration then localT / common.enterBusDuration
    else 1;

  doorOpen: common.ease01(enterProgress);

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
  startHead: windowCenter(startWindow);

  doorWidth: busW * 0.14;
  doorHeight: busH * 0.72;
  doorPos: [busAnchor[0] + busW * 0.08, busAnchor[1] + busH * 0.02];
  interiorPos: [doorPos[0] + doorWidth * 0.12, doorPos[1] + doorHeight * 0.06];
  interiorH: doorHeight * 0.9;
  floorH: interiorH * 0.22;
  stepH: floorH * 0.34;

  groundFootY: standing.anchor[1] + standing.leftLeg.end[1];
  insideFootY: interiorPos[1] + floorH * 0.52 + stepH;
  enterVerticalDistance: insideFootY - groundFootY;

  enterInsideX: interiorPos[0] + doorWidth * 0.72;
  enterX: standing.anchor[0] + (enterInsideX - standing.anchor[0]) * enterProgress;

  enterStride: common.walkStride;
  enterZoomFactor: 0.03;
  enterBase: standing + actor.backPose;
  enterZoomWalk: actor.character.zoomWalk(standing.anchor, enterBase, enterVerticalDistance, enterStride, enterProgress, enterZoomFactor);
  entering: enterZoomWalk + { anchor: [enterX, enterZoomWalk.anchor[1]]; };
  outsideGraphic0: actor.character.static(entering.anchor, entering, actor.charPalette, actor.skin);

  outsideOpacity:
    if enterProgress < 0.55 then 1
    else 1 - common.ease01((enterProgress - 0.55) / 0.45);

  outsideGraphic: outsideGraphic0 map (g) => g + { opacity: outsideOpacity; };

  headAlpha:
    if enterProgress < 0.55 then 0
    else common.ease01((enterProgress - 0.55) / 0.45);

  head:
  {
    type: "circle";
    name: "rider-head";
    center: startHead;
    radius: stopped.headRadius;
    fill: actor.charPalette.body;
    stroke: "none";
    width: 0;
    opacity: headAlpha;
  };

  eval
  {
    view;
    graphics:
      backdrop(view, 1, t)
      + busRest
      + [head]
      + glass
      + outsideGraphic
      + stopSign;
  };
}

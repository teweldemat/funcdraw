(localT) =>
{
  transport: package("@funcdraw/testlib").cartoon.transport;
  landscape: package("@funcdraw/testlib").cartoon.landscape;
  house: package("@funcdraw/testlib").cartoon.house;
  bicycle: package("@funcdraw/testlib").cartoon.bicycle;
  character: package("@funcdraw/testlib").cartoon.character;

  clamp01: (p) => if p < 0 then 0 else if p > 1 then 1 else p;
  lerp: (a, b, p) => a + (b - a) * p;

  stopCenterX: 360;

  busW: common.bus.size[0];
  busH: common.bus.size[1];
  wheelRadiusBus: busH * 0.18;
  busY: common.roadBottomY + wheelRadiusBus * 1.4;
  busDoorCenterOffset: busW * 0.15;
  stopDoorCenterX: stopCenterX + 30;
  busStopX: stopDoorCenterX - busDoorCenterOffset;

  arriveDuration: 4;
  doorOpenDuration: 1.2;
  exitDuration: 2.2;
  departDuration: 3;
  walkToBikeDuration: 3.2;
  mountDuration: 0.6;
  rideDuration: 5.8;

  arriveT: localT;
  doorT: localT - arriveDuration;
  exitT: localT - arriveDuration - doorOpenDuration;
  departT: localT - arriveDuration - doorOpenDuration - exitDuration;
  walkT: localT - arriveDuration - doorOpenDuration - exitDuration - departDuration;
  mountT: localT - arriveDuration - doorOpenDuration - exitDuration - departDuration - walkToBikeDuration;
  rideT:
    localT
    - arriveDuration
    - doorOpenDuration
    - exitDuration
    - departDuration
    - walkToBikeDuration
    - mountDuration;

  // View: fixed at the new stop until riding begins, then follow the bike a bit.
  rideProgress: clamp01(rideT / rideDuration);
  rideDistance: 220;
  bikeRearX0: stopCenterX - 70;
  bikeRearX: bikeRearX0 - rideDistance * rideProgress;
  viewCenterX:
    if rideT < 0 then stopCenterX
    else bikeRearX - 40;
  view: common.resolveViewAt(viewCenterX);

  // More houses + trees at the new location.
  house1:
    house.types.townhouse(
      {
        anchor: [stopCenterX - 60, common.yardTopY];
        width: 52;
        stories: 3;
        doorOpen: 0;
        lightColor: "#0f172a";
      });
  house2:
    house.types.cottage(
      {
        anchor: [stopCenterX + 55, common.yardTopY];
        width: 46;
        stories: 2;
        doorOpen: 0;
        lightColor: "#0f172a";
      });
  house3:
    house.types.cottage(
      {
        anchor: [stopCenterX - 165, common.yardTopY];
        width: 44;
        stories: 1;
        doorOpen: 0;
        lightColor: "#0f172a";
      });

  tree: (x, h, idx) =>
    (landscape.tree({ base: [x, common.yardTopY]; height: h; }) map (g) => g + { zIndex: -10; treeIndex: idx; });
  trees:
    tree(stopCenterX - 100, 28, 0)
    + tree(stopCenterX - 25, 34, 1)
    + tree(stopCenterX + 5, 26, 2)
    + tree(stopCenterX + 95, 30, 3)
    + tree(stopCenterX - 210, 32, 4);

  // Bus stop sign (new location).
  stopSignCfg: common.busStopSign + { base: [stopCenterX + 80, common.roadBottomY]; };
  stopSign: transport.busStopSign(stopSignCfg);

  // Bus motion + door.
  arriveProgress: common.ease01(clamp01(arriveT / arriveDuration));
  doorProgress: common.ease01(clamp01(doorT / doorOpenDuration));
  exitProgress: common.ease01(clamp01(exitT / exitDuration));
  departProgress: common.ease01(clamp01(departT / departDuration));

  busStartX: view.left - busW - 40;
  busEndX: view.right + busW + 60;
  busX0: busStartX + (busStopX - busStartX) * arriveProgress;
  busX:
    if departT < 0 then busX0
    else busStopX + (busEndX - busStopX) * departProgress;
  busAnchor: [busX, busY];

  doorOpen:
    if doorT < 0 then 0
    else if exitT < 0 then doorProgress
    else if departT < 0 then 1
    else 1 - departProgress;

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
  seatWindow: First(windows, (w) => w.index == 2);
  insideHead: windowCenter(seatWindow);

  headAlpha:
    if exitT < 0 then 1
    else if departT < 0 then 1 - exitProgress
    else 0;

  head:
  {
    type: "circle";
    name: "rider-head";
    center: insideHead;
    radius: actor.characterMeasurements.headRadius;
    fill: actor.charPalette.body;
    stroke: "none";
    width: 0;
    opacity: headAlpha;
  };

  // Character exits the bus.
  doorWidth: busW * 0.14;
  doorHeight: busH * 0.72;
  doorPos: [busAnchor[0] + busW * 0.08, busAnchor[1] + busH * 0.02];
  interiorPos: [doorPos[0] + doorWidth * 0.12, doorPos[1] + doorHeight * 0.06];
  interiorH: doorHeight * 0.9;
  floorH: interiorH * 0.22;
  stepH: floorH * 0.34;
  insideFootY: interiorPos[1] + floorH * 0.52 + stepH;

  outsideAnchor:
  {
    legY: actor.characterMeasurements.leftLeg.end[1];
    groundFootY: common.roadBottomY;
    pelvisY: groundFootY - legY;
    eval [stopDoorCenterX - 6, pelvisY];
  };

  insideX: interiorPos[0] + doorWidth * 0.72;
  reverseExit: 1 - exitProgress;
  exitX: outsideAnchor[0] + (insideX - outsideAnchor[0]) * reverseExit;

  verticalToInside:
  {
    groundFootY: common.roadBottomY;
    eval insideFootY - groundFootY;
  };

  exitStride: common.walkStride;
  exitZoomFactor: 0.03;
  exitBase: actor.characterMeasurements + actor.leftPose;
  exitZoomWalk: actor.crouchProfile(actor.character.zoomWalk(outsideAnchor, exitBase, verticalToInside, exitStride, reverseExit, exitZoomFactor));
  exiting: exitZoomWalk + { anchor: [exitX, exitZoomWalk.anchor[1]]; };

  bodyExitAlpha:
    if exitT < 0 then 0
    else if departT < 0 then exitProgress
    else 1;

  exitingGraphic0: actor.character.static(exiting.anchor, exiting, actor.charPalette, actor.skin);
  exitingGraphic: exitingGraphic0 map (g) => g + { opacity: bodyExitAlpha; };

  // Bicycle waiting on the sidewalk (proportioned to the character).
  bikeWheelRadius: actor.characterMeasurements.height * 0.9;
  bikeRearCenter: [bikeRearX0, common.roadBottomY + bikeWheelRadius];
  bikeAngleIdle: 0;
  bikeStatic: bicycle(bikeRearCenter, bikeWheelRadius, bikeAngleIdle, "#9ca3af", "#6b7280", "left");

  // Walk to the bicycle after the bus departs.
  walkProgress: common.ease01(clamp01(walkT / walkToBikeDuration));
  walkTargetX: bikeStatic.attachments.seat[0] + 10;
  walkDistance: walkTargetX - outsideAnchor[0];
  walkerBase: actor.characterMeasurements + actor.leftPose;
  walker:
    if walkT < 0 then walkerBase + { anchor: outsideAnchor; }
    else actor.crouchProfile(actor.character.profileWalk(outsideAnchor, walkerBase, walkDistance, common.walkStride, walkProgress));

  walkerGraphic0: actor.character.static(walker.anchor, walker, actor.charPalette, actor.skin);

  // Mount transition: cross-fade from walker to rider-on-bike.
  mountProgress: common.ease01(clamp01(mountT / mountDuration));

  bikeRideAngle: rideDistance * rideProgress / bikeWheelRadius;
  bikeRideRearCenter: [bikeRearX, common.roadBottomY + bikeWheelRadius];
  bikeMoving: bicycle(bikeRideRearCenter, bikeWheelRadius, bikeRideAngle, "#9ca3af", "#6b7280", "left");

  // Rider pose attached to pedals + handlebar.
  dist: (a, b) =>
  {
    dx: b[0] - a[0];
    dy: b[1] - a[1];
    eval math.Sqrt(dx * dx + dy * dy);
  };
  bendSignTo: (origin, target, desiredDir) =>
  {
    dx: target[0] - origin[0];
    dy: target[1] - origin[1];
    len: math.Sqrt(dx * dx + dy * dy);
    eval if len <= 0 then 1 else
    {
      dir: [dx / len, dy / len];
      perp: [-dir[1], dir[0]];
      dot: perp[0] * desiredDir[0] + perp[1] * desiredDir[1];
      eval if dot >= 0 then 1 else -1;
    };
  };

  makeRider: (bikeForRide, includeBike) =>
  {
    dirMul: bikeForRide.dirMul ?? -1;
    bodyAngleBase: 1.2;
    neckAngleBase: 1.3;
    bodyAngle: if dirMul >= 0 then bodyAngleBase else math.Pi - bodyAngleBase;
    neckAngle: if dirMul >= 0 then neckAngleBase else math.Pi - neckAngleBase;

    anchor:
      [
        bikeForRide.attachments.seat[0] + 0.8,
        bikeForRide.attachments.seat[1] - 0.3
      ];

    baseMeasurements:
      actor.characterMeasurements
      + actor.leftPose
      + {
        bodyAngle;
        neckAngle;
        shoulderWidth: actor.characterMeasurements.shoulderWidth * 1.15;
        thighWidth: actor.characterMeasurements.thighWidth * 1.1;
      };

    baseGeometry: character.skeleton.build(anchor, baseMeasurements);

    pedalNear: bikeForRide.attachments.pedals.near;
    pedalFar: bikeForRide.attachments.pedals.far;
    leftPedalTarget: pedalNear;
    rightPedalTarget: pedalFar;
    barA: bikeForRide.attachments.handlebar.from;
    barB: bikeForRide.attachments.handlebar.to;

    kneeBendDir: [dirMul, 0];
    elbowBendDir: [-0.2 * dirMul, -1];

    leftLegEnd:
      [leftPedalTarget[0] - baseGeometry.leftLegAttachment[0], leftPedalTarget[1] - baseGeometry.leftLegAttachment[1]];
    rightLegEnd:
      [rightPedalTarget[0] - baseGeometry.rightLegAttachment[0], rightPedalTarget[1] - baseGeometry.rightLegAttachment[1]];
    leftHandEnd: [barA[0] - baseGeometry.leftHandAttachment[0], barA[1] - baseGeometry.leftHandAttachment[1]];
    rightHandEnd: [barB[0] - baseGeometry.rightHandAttachment[0], barB[1] - baseGeometry.rightHandAttachment[1]];

    leftKneeSign: bendSignTo(baseGeometry.leftLegAttachment, leftPedalTarget, kneeBendDir);
    rightKneeSign: bendSignTo(baseGeometry.rightLegAttachment, rightPedalTarget, kneeBendDir);
    leftElbowSign: bendSignTo(baseGeometry.leftHandAttachment, barA, elbowBendDir);
    rightElbowSign: bendSignTo(baseGeometry.rightHandAttachment, barB, elbowBendDir);

    legSlackMul: 1.05;
    armSlackMul: 1.03;
    legMaxDist:
      math.Max(
        dist(baseGeometry.leftLegAttachment, bikeForRide.frontGearCenter) + bikeForRide.pedalOrbitRadius,
        dist(baseGeometry.rightLegAttachment, bikeForRide.frontGearCenter) + bikeForRide.pedalOrbitRadius
      ) * legSlackMul;
    armMaxDist:
      math.Max(
        dist(baseGeometry.leftHandAttachment, barA),
        dist(baseGeometry.rightHandAttachment, barB)
      ) * armSlackMul;

    legUpper: legMaxDist / 2;
    legLower: legMaxDist / 2;
    armUpper: armMaxDist / 2;
    armLower: armMaxDist / 2;

    riderMeasurements:
      baseMeasurements
      + {
        leftLeg:
          baseMeasurements.leftLeg
          + { upper: legUpper; lower: legLower; end: leftLegEnd; sign: leftKneeSign; };
        rightLeg:
          baseMeasurements.rightLeg
          + { upper: legUpper; lower: legLower; end: rightLegEnd; sign: rightKneeSign; };
        leftHand:
          baseMeasurements.leftHand
          + { upper: armUpper; lower: armLower; end: leftHandEnd; sign: leftElbowSign; };
        rightHand:
          baseMeasurements.rightHand
          + { upper: armUpper; lower: armLower; end: rightHandEnd; sign: rightElbowSign; };
      };

    selectedSkin:
      if includeBike then
        character.skins.polyRider(
          {
            back: bikeForRide.layers.back;
            between: bikeForRide.layers.between;
            front: bikeForRide.layers.front;
          })
      else actor.skin;

    eval character.static(anchor, riderMeasurements, actor.charPalette, selectedSkin);
  };

  // Avoid drawing a second "standing" character while the bus arrives: only show walker after walking starts.
  walkerShown:
    if walkT < 0 then []
    else if rideT >= 0 then []
    else if mountT < 0 then walkerGraphic0
    else walkerGraphic0 map (g) => g + { opacity: 1 - mountProgress; };

  riderMountShown:
    if mountT < 0 or rideT >= 0 then []
    else (makeRider(bikeStatic, false) map (g) => g + { opacity: mountProgress; });

  riderRideShown:
    if rideT < 0 then []
    else makeRider(bikeMoving, true);

  // Keep the bike visible during mount; swap to the moving bike once riding starts.
  bikeOnGround:
    if rideT < 0 then bikeStatic.graphics
    else [];

  eval
  {
    view;
    graphics:
      backdrop(view, 1, t)
      + trees
      + house1
      + house2
      + house3
      + busRest
      + [head]
      + glass
      + stopSign
      + (if walkT < 0 then exitingGraphic else [])
      + bikeOnGround
      + walkerShown
      + riderMountShown
      + riderRideShown;
  };
}

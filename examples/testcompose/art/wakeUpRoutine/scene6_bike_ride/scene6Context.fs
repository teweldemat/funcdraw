(localT) =>
{
  transport: package("@funcdraw/testlib").cartoon.transport;
  city: package("@funcdraw/testlib").cartoon.city;
  landscape: package("@funcdraw/testlib").cartoon.landscape;
  house: package("@funcdraw/testlib").cartoon.house;
  bicycle: transport.bicycle;
  character: package("@funcdraw/testlib").cartoon.character;

  s: scene6Segments;

  clamp01: (p) => if p < 0 then 0 else if p > 1 then 1 else p;
  lerp: (a, b, p) => a + (b - a) * p;
  lerpVec: (a, b, p) => [lerp(a[0], b[0], p), lerp(a[1], b[1], p)];

  stopCenterX: 360;

  busW: common.bus.size[0];
  busH: common.bus.size[1];
  wheelRadiusBus: busH * 0.18;
  busY: common.roadBottomY + wheelRadiusBus * 1.4;
  busDoorCenterOffset: busW * 0.15;
  stopDoorCenterX: stopCenterX + 30;
  busStopX: stopDoorCenterX - busDoorCenterOffset;

  arriveT: localT;
  doorT: localT - s.doorOffset;
  exitT: localT - s.exitOffset;
  departT: localT - s.departOffset;
  walkT: localT - s.walkOffset;
  mountT: localT - s.mountOffset;
  rideT: localT - s.rideOffset;

  // View: fixed at the new stop until riding begins, then follow the bike a bit.
  rideProgress: clamp01(rideT / s.rideDuration);
  rideDistance: 220;
  bikeRearX0: stopCenterX - 70;
  viewCenterX:
    if rideT < 0 then stopCenterX
    else bikeRearCenterRide[0] - 40;
  view: common.resolveViewAt(viewCenterX);
  cameraDeltaX: viewCenterX - stopCenterX;

  translateGraphicX: (dx, g) =>
    if g.type == "rect" then g + { position: [g.position[0] + dx, g.position[1]]; }
    else if g.type == "circle" then g + { center: [g.center[0] + dx, g.center[1]]; }
    else if g.type == "line" then g + { from: [g.from[0] + dx, g.from[1]]; to: [g.to[0] + dx, g.to[1]]; }
    else if g.type == "polygon" then g + { points: g.points map (p) => [p[0] + dx, p[1]]; }
    else if g.type == "text" then g + { position: [g.position[0] + dx, g.position[1]]; }
    else g;

  rotatePointAroundCS: (pivot, c, s, p) =>
  {
    dx: p[0] - pivot[0];
    dy: p[1] - pivot[1];
    eval [pivot[0] + dx * c - dy * s, pivot[1] + dx * s + dy * c];
  };

  rotateGraphicAroundCS: (pivot, c, s, g) =>
    if g.type == "circle" then g + { center: rotatePointAroundCS(pivot, c, s, g.center); }
    else if g.type == "line" then g + { from: rotatePointAroundCS(pivot, c, s, g.from); to: rotatePointAroundCS(pivot, c, s, g.to); }
    else if g.type == "polygon" then g + { points: g.points map (p) => rotatePointAroundCS(pivot, c, s, p); }
    else if g.type == "rect" then
      g
      + {
        type: "polygon";
        points:
          [
            rotatePointAroundCS(pivot, c, s, g.position),
            rotatePointAroundCS(pivot, c, s, [g.position[0] + g.size[0], g.position[1]]),
            rotatePointAroundCS(pivot, c, s, [g.position[0] + g.size[0], g.position[1] + g.size[1]]),
            rotatePointAroundCS(pivot, c, s, [g.position[0], g.position[1] + g.size[1]]),
          ];
      }
    else if g.type == "text" then g + { position: rotatePointAroundCS(pivot, c, s, g.position); }
    else g;

  rotateLayerAroundCS: (pivot, c, s, layer) => layer map (g) => rotateGraphicAroundCS(pivot, c, s, g);

  parallaxLayer: (depth, layer) =>
  {
    // depth=0 => fixed in world (foreground), depth=1 => locked to view (infinite background).
    dx: cameraDeltaX * depth;
    eval layer map (g) => translateGraphicX(dx, g);
  };

  // More houses + trees at the new location.
  house1:
    house.types.townhouse(
      {
        // Keep the tallest building away from the side road intersection.
        anchor: [stopCenterX + 110, common.yardTopY];
        width: 52;
        stories: 3;
        doorOpen: 0;
        lightColor: "#0f172a";
      });
  house2:
    house.types.cottage(
      {
        anchor: [stopCenterX + 65, common.yardTopY];
        width: 46;
        stories: 2;
        doorOpen: 0;
        lightColor: "#0f172a";
      });
  house3:
    house.types.cottage(
      {
        // Keep the smaller house away from the side-road entrance.
        anchor: [stopCenterX - 235, common.yardTopY];
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

  // Depth/parallax: the bike, bus, and main road are "depth 0".
  // Background elements drift with parallax by depth.
  sideRoadHorizonY: -8;
  depthFromY: (y) => clamp01((y - common.roadTopY) / (sideRoadHorizonY - common.roadTopY));
  // Houses are placed farther than trees (more drift + drawn behind).
  treesDepth: depthFromY(common.yardTopY + 1);
  housesDepth: depthFromY(common.yardTopY + 7);

  treesLayer: parallaxLayer(treesDepth, trees);
  housesLayer:
    parallaxLayer(housesDepth, house1)
    + parallaxLayer(housesDepth, house2)
    + parallaxLayer(housesDepth, house3);

  sideRoadTurnX: stopCenterX - 200;
  // Used for the bike's fixed (depth=0) ride path.
  sideRoadBikeVanishX0: sideRoadTurnX - 90;
  // Used for the road graphic: when the view is centered on `sideRoadTurnX`, the road becomes vertical.
  // This is achieved by keeping the vanishing point at the screen center (world x = `viewCenterX`).
  sideRoadRenderVanishX0: stopCenterX;

  // A side road branching toward the horizon with rough perspective.
  // Base stays attached to the main road (depth 0), while the far end drifts
  // with background parallax so it aligns with houses/trees.
  sideRoad:
  {
    turnX: sideRoadTurnX;
    baseY: common.roadTopY;
    roadH: common.roadTopY - common.roadBottomY;
    baseW: roadH * 0.95;
    horizonY: sideRoadHorizonY;
    topW: baseW * 0.16;
    // Far end is shifted by the same parallax depth as houses/trees.
    vanishX0: sideRoadRenderVanishX0;
    vanishDepth: depthFromY(horizonY);
    vanishX: vanishX0 + cameraDeltaX * vanishDepth;

    patch:
    {
      type: "polygon";
      name: "side-road-intersection";
      // Extend the side-road edges down into the main road with the same perspective.
      points:
      {
        bottomY: common.roadBottomY;
        // p is negative (extrapolation) since bottomY is closer than baseY.
        p: (bottomY - baseY) / (horizonY - baseY);
        leftTop: [turnX - baseW / 2, baseY];
        rightTop: [turnX + baseW / 2, baseY];
        leftBottom: [leftTop[0] + (vanishX - leftTop[0]) * p, bottomY];
        rightBottom: [rightTop[0] + (vanishX - rightTop[0]) * p, bottomY];
        eval [leftBottom, rightBottom, rightTop, leftTop];
      };
      fill: "#475569";
      stroke: "none";
      width: 0;
    };

    roadPoly:
    {
      type: "polygon";
      name: "side-road";
      points:
      [
        [turnX - baseW / 2, baseY],
        [turnX + baseW / 2, baseY],
        [vanishX + topW / 2, horizonY],
        [vanishX - topW / 2, horizonY],
      ];
      fill: "#475569";
      stroke: "#0f172a";
      width: 0.25;
    };

    dxWorld: vanishX0 - turnX;
    dy: horizonY - baseY;
    dx: vanishX - turnX;
    len: math.Sqrt(dx * dx + dy * dy);
    dir: if len <= 0 then [0, 1] else [dx / len, dy / len];
    lerp: (a, b, p) => a + (b - a) * p;
    dashCount: 9;
    dashes:
      Range(0, dashCount) map (k, idx) =>
      {
        p: (k + 0.6) / (dashCount + 1);
        // Interpolate parallax along the road: no drift at the base, full drift at the horizon.
        y: baseY + dy * p;
        driftX: cameraDeltaX * depthFromY(y);
        center: [turnX + dxWorld * p + driftX, y];
        dashLen: lerp(3.2, 0.55, p);
        from: [center[0] - dir[0] * dashLen / 2, center[1] - dir[1] * dashLen / 2];
        to: [center[0] + dir[0] * dashLen / 2, center[1] + dir[1] * dashLen / 2];
        eval { type: "line"; name: "side-road-dash"; from; to; stroke: fd.color.alpha("#e2e8f0", 0.75); width: lerp(1.05, 0.2, p); };
      };

    eval
      if turnX < view.left - 40 or turnX > view.right + 40 then []
      else [patch, roadPoly] + dashes;
  };

  // Add a subtle perspective shear to the zebra crossing so it responds to view changes.
  roadDepth01: (y) => clamp01((y - common.roadBottomY) / (common.roadTopY - common.roadBottomY));
  zebraCenterX: common.zebraCrossing.centerX;
  zebraDeltaX: viewCenterX - zebraCenterX;
  zebraPerspectiveStrength: 0.35;

  warpZebraStripe:
    (g) =>
      if g.type != "rect" then g
      else
      {
        x0: g.position[0];
        y0: g.position[1];
        x1: g.position[0] + g.size[0];
        y1: g.position[1] + g.size[1];

        warpX: (x, y) => x + zebraDeltaX * roadDepth01(y) * zebraPerspectiveStrength;
        eval
          g
          + {
            type: "polygon";
            points:
              [
                [warpX(x0, y0), y0],
                [warpX(x1, y0), y0],
                [warpX(x1, y1), y1],
                [warpX(x0, y1), y1],
              ];
          };
      };

  backdrop0: backdrop(view, 1, t);
  zebraStripes: backdrop0 filter (g) => g.name == "zebra";
  backdropNoZebra: backdrop0 filter (g) => g.name != "zebra";
  zebraLayer: zebraStripes map warpZebraStripe;

  envLayer:
    // Draw farther houses behind nearer trees.
    backdropNoZebra
    + zebraLayer
    + sideRoad
    + housesLayer
    + treesLayer;

  // Bus stop sign (new location).
  stopSignCfg: common.busStopSign + { base: [stopCenterX + 80, common.roadBottomY]; };
  stopSign: city.busStopSign(stopSignCfg);

  // Bus motion + door.
  arriveProgress: common.ease01(clamp01(arriveT / s.arriveDuration));
  doorProgress: common.ease01(clamp01(doorT / s.doorOpenDuration));
  exitProgress: common.ease01(clamp01(exitT / s.exitDuration));
  departProgress: common.ease01(clamp01(departT / s.departDuration));

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

  busLayer: busRest + [head] + glass;

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
  exitLayer: if walkT < 0 then exitingGraphic else [];

  // Bicycle waiting on the sidewalk (proportioned to the character).
  bikeWheelRadius: actor.characterMeasurements.height * 0.55;
  bikeRearCenter: [bikeRearX0, common.roadBottomY + bikeWheelRadius];
  bikeAngleIdle: 0;
  bikeStatic: bicycle(bikeRearCenter, bikeWheelRadius, bikeAngleIdle, "#9ca3af", "#6b7280", "left");

  // Walk to the bicycle after the bus departs.
  walkProgress: common.ease01(clamp01(walkT / s.walkToBikeDuration));
  walkTargetX: bikeStatic.attachments.seat[0] + 10;
  walkDistance: walkTargetX - outsideAnchor[0];
  walkerBase: actor.characterMeasurements + actor.leftPose;
  walker:
    if walkT < 0 then walkerBase + { anchor: outsideAnchor; }
    else actor.crouchProfile(actor.character.profileWalk(outsideAnchor, walkerBase, walkDistance, common.walkStride, walkProgress));

  walkerGraphic0: actor.character.static(walker.anchor, walker, actor.charPalette, actor.skin);

  // Mount transition: cross-fade from walker to rider-on-bike.
  mountProgress: common.ease01(clamp01(mountT / s.mountDuration));

  bikeRideAngle: rideDistance * rideProgress / bikeWheelRadius;
  bikeRideY: common.roadBottomY + bikeWheelRadius;
  bikeRearX: bikeRearX0 - rideDistance * rideProgress;
  bikeMainRearCenter: [bikeRearX, bikeRideY];

  preTurnDistance: bikeRearX0 - sideRoadTurnX;
  turnMid: clamp01(preTurnDistance / rideDistance);
  turnWindow: 0.18;
  turnStart: turnMid - turnWindow / 2;
  turnBlend: common.ease01(clamp01((rideProgress - turnStart) / turnWindow));

  sideDxWorld: sideRoadBikeVanishX0 - sideRoadTurnX;
  sideDyWorld: sideRoadHorizonY - common.roadTopY;
  sideLen0: math.Sqrt(sideDxWorld * sideDxWorld + sideDyWorld * sideDyWorld);
  sideDir0: if sideLen0 <= 0 then [-1, 0] else [sideDxWorld / sideLen0, sideDyWorld / sideLen0];

  sideProgress: clamp01((rideProgress - turnStart) / (1 - turnStart));
  // Don't ride all the way to the horizon (it would "float" into the sky in this 2D setup).
  sidePMax: 0.55;
  sideP: sidePMax * sideProgress;

  // Bike is depth=0, so its world path does not include parallax drift.
  sideSurfaceY: common.roadTopY + sideDyWorld * sideP;
  bikeSideRearCenter: [sideRoadTurnX + sideDxWorld * sideP, sideSurfaceY + bikeWheelRadius];

  bikeRearCenterRide: lerpVec(bikeMainRearCenter, bikeSideRearCenter, turnBlend);
  // Align rotation with the side road's current screen direction (includes parallax drift),
  // while keeping the bike path itself at depth=0.
  sidePAhead: if sideP + 0.01 > sidePMax then sidePMax else sideP + 0.01;
  sideSurfaceY2: common.roadTopY + sideDyWorld * sidePAhead;
  sideDriftX2: cameraDeltaX * depthFromY(sideSurfaceY2);
  sideDriftX1: cameraDeltaX * depthFromY(sideSurfaceY);
  sideCenterX1: sideRoadTurnX + sideDxWorld * sideP + sideDriftX1;
  sideCenterX2: sideRoadTurnX + sideDxWorld * sidePAhead + sideDriftX2;
  sideDxNow: sideCenterX2 - sideCenterX1;
  sideDyNow: sideSurfaceY2 - sideSurfaceY;
  sideLenNow: math.Sqrt(sideDxNow * sideDxNow + sideDyNow * sideDyNow);
  sideDir: if sideLenNow <= 0 then sideDir0 else [sideDxNow / sideLenNow, sideDyNow / sideLenNow];
  cTarget: -sideDir[0];
  sTarget: -sideDir[1];
  cRaw: lerp(1, cTarget, turnBlend);
  sRaw: lerp(0, sTarget, turnBlend);
  csLen: math.Sqrt(cRaw * cRaw + sRaw * sRaw);
  cTurn: if csLen <= 0 then 1 else cRaw / csLen;
  sTurn: if csLen <= 0 then 0 else sRaw / csLen;

  bikeMoving: bicycle(bikeRearCenterRide, bikeWheelRadius, bikeRideAngle, "#9ca3af", "#6b7280", "left");

  // Rider pose attached to pedals + handlebar.
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

    riderMeasurements:
      baseMeasurements
      + {
        leftLeg:
          baseMeasurements.leftLeg
          + { end: leftLegEnd; sign: leftKneeSign; };
        rightLeg:
          baseMeasurements.rightLeg
          + { end: rightLegEnd; sign: rightKneeSign; };
        leftHand:
          baseMeasurements.leftHand
          + { end: leftHandEnd; sign: leftElbowSign; };
        rightHand:
          baseMeasurements.rightHand
          + { end: rightHandEnd; sign: rightElbowSign; };
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
    else rotateLayerAroundCS(bikeRearCenterRide, cTurn, sTurn, makeRider(bikeMoving, true));

  // Keep the bike visible during mount; swap to the moving bike once riding starts.
  bikeOnGround:
    if rideT < 0 then bikeStatic.graphics
    else [];

  eval
  {
    view;
    envLayer;
    stopSign;
    busLayer;
    exitLayer;
    bikeOnGround;
    walkerShown;
    riderMountShown;
    riderRideShown;
  };
}

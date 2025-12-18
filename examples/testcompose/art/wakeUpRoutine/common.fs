{
  house: package("@funcdraw/testlib").cartoon.house;

  baseView:
  {
    left: -110;
    bottom: -78;
    right: 110;
    top: 90;
  };

  baseCenterX: (baseView.left + baseView.right) / 2;

  resolveViewAt: (centerX) =>
  {
    baseW: baseView.right - baseView.left;
    baseH: baseView.top - baseView.bottom;
    ratio: canvas.size.width / canvas.size.height;
    desiredW: baseH * ratio;
    width: if desiredW >= baseW then desiredW else baseW;
    height: if desiredW >= baseW then baseH else baseW / ratio;
    left: centerX - width / 2;
    bottom: baseView.bottom;
    eval { left; bottom; right: left + width; top: bottom + height; };
  };

  resolveView: () => resolveViewAt(baseCenterX);

  roadBottomY: -60;
  roadTopY: -44;
  sidewalkTopY: -38;
  yardTopY: -20;

  houseAnchor: [-55, yardTopY];
  houseWidth: 60;
  houseStories: 2;

  followLookAhead: 40;
  busStopX: 85;

  zebraCrossing:
  {
    centerX: 22;
    width: 26;
    stripeWidth: 2.3;
    gap: 1.7;
    inset: 1.2;
    fill: fd.color.alpha("#e2e8f0", 0.9);
  };

  walkStride: 6;
  walkStepsPerSecond: 3;
  walkwayDx: 0;

  walkDuration: (distance) => math.Abs(distance) / (walkStride * walkStepsPerSecond);

  scene1Duration: 8;
  scene2Duration: 4;

  walkToRoadDuration: walkDuration(sidewalkTopY - yardTopY);
  turnDuration: 1.5;
  walkToCrossingDuration: walkDuration(zebraCrossing.centerX - (houseAnchor[0] + walkwayDx));
  crossZebraDuration: walkDuration(roadBottomY - sidewalkTopY);
  walkToStopFarSideDuration: walkDuration(busStopX - zebraCrossing.centerX);
  walkToStopDuration: walkToCrossingDuration + crossZebraDuration + walkToStopFarSideDuration;
  scene3Duration: walkToRoadDuration + turnDuration + walkToStopDuration;

  waitForBusDuration: 2.5;
  busArriveDuration: 4.5;
  busDoorOpenDuration: 1.4;
  scene4Duration: waitForBusDuration + busArriveDuration + busDoorOpenDuration;

  enterBusDuration: 1.2;
  walkToSeatDuration: 3.5;
  settleDuration: 1;
  scene5aDuration: enterBusDuration;
  scene5bDuration: walkToSeatDuration + settleDuration;
  scene5Duration: scene5aDuration + scene5bDuration;

  scene6Duration: 20;

  totalDuration: scene1Duration + scene2Duration + scene3Duration + scene4Duration + scene5Duration + scene6Duration;

  ease01: (p) => (1 - math.Cos(p * math.Pi)) / 2;

  busStopSign:
  {
    base: [busStopX - 14, roadBottomY];
    poleHeight: 18;
    signSize: [12, 8];
    fill: "#e2e8f0";
    stroke: "#0f172a";
    width: 0.35;
    textColor: "#0f172a";
  };

  bus:
  {
    size: [92, 30];
    fill: "#f97316";
    stroke: "#0f172a";
    width: 0.35;
  };

  houseLayout: (anchor, width, stories) =>
  {
    storyHeight: width * 0.55;
    doorWidth: width * 0.22;
    doorHeight: storyHeight * 0.72;
    doorPos: [anchor[0] - doorWidth / 2, anchor[1]];
    doorSize: [doorWidth, doorHeight];

    windowW: width * 0.16;
    windowH: storyHeight * 0.22;
    windowY: (i) => anchor[1] + storyHeight * i + storyHeight * 0.52;
    leftX: anchor[0] - width * 0.27 - windowW / 2;
    rightX: anchor[0] + width * 0.27 - windowW / 2;

    windows:
      Range(0, stories) reduce (acc, i) =>
        acc
        + [
          { position: [leftX, windowY(i)]; size: [windowW, windowH]; },
          { position: [rightX, windowY(i)]; size: [windowW, windowH]; }
        ]
      ~ [];

    eval { storyHeight; doorPos; doorSize; windows; };
  };

}

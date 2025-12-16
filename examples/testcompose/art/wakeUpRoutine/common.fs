{
  house: package("@funcdraw/testlib").cartoon.house;

  baseView:
  {
    left: -110;
    bottom: -60;
    right: 110;
    top: 90;
  };

  resolveView: () =>
  {
    baseW: baseView.right - baseView.left;
    baseH: baseView.top - baseView.bottom;
    ratio: canvas.size.width / canvas.size.height;
    desiredW: baseH * ratio;
    width: if desiredW >= baseW then desiredW else baseW;
    height: if desiredW >= baseW then baseH else baseW / ratio;
    centerX: (baseView.left + baseView.right) / 2;
    left: centerX - width / 2;
    bottom: baseView.bottom;
    eval { left; bottom; right: left + width; top: bottom + height; };
  };

  roadBottomY: -60;
  roadTopY: -44;
  sidewalkTopY: -38;
  yardTopY: -20;

  houseAnchor: [-55, yardTopY];
  houseWidth: 60;
  houseStories: 2;

  scene1Duration: 8;
  scene2Duration: 4;

  walkToRoadDuration: 4;
  turnDuration: 1.5;
  acrossDuration: 7;
  endDuration: 2;
  scene3Duration: walkToRoadDuration + turnDuration + acrossDuration + endDuration;

  cycleDuration: scene1Duration + scene2Duration + scene3Duration;

  ease01: (p) => (1 - math.Cos(p * math.Pi)) / 2;

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

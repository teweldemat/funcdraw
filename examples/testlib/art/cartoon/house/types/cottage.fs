(options) =>
{
  anchor: options.anchor;
  houseWidth: options.width;
  stories: options.stories;
  doorOpen: options.doorOpen;
  lightColor: options.lightColor;

  eval if stories < 1 then error("expected stories >= 1") else
  {
    storyHeight: houseWidth * 0.55;
    bodyHeight: storyHeight * stories;
    wallLeft: anchor[0] - houseWidth / 2;
    wallBottom: anchor[1];
    wallTop: wallBottom + bodyHeight;

    roofHeight: storyHeight * 0.7;
    roofOverhang: houseWidth * 0.06;

    wall:
    {
      type: "rect";
      name: "wall";
      position: [wallLeft, wallBottom];
      size: [houseWidth, bodyHeight];
      fill: common.wallFill;
      stroke: common.wallStroke;
      width: common.wallWidth;
    };

    roof:
    {
      type: "polygon";
      name: "roof";
      points:
      [
        [wallLeft - roofOverhang, wallTop],
        [wallLeft + houseWidth + roofOverhang, wallTop],
        [anchor[0], wallTop + roofHeight]
      ];
      fill: common.roofFill;
      stroke: common.roofStroke;
      width: common.roofWidth;
    };

    doorWidth: houseWidth * 0.22;
    doorHeight: storyHeight * 0.72;
    doorPos: [anchor[0] - doorWidth / 2, wallBottom];
    door: common.makeDoor(doorPos, [doorWidth, doorHeight], doorOpen, lightColor);

    windowW: houseWidth * 0.16;
    windowH: storyHeight * 0.22;
    windowY: (i) => wallBottom + storyHeight * i + storyHeight * 0.52;
    leftX: anchor[0] - houseWidth * 0.27 - windowW / 2;
    rightX: anchor[0] + houseWidth * 0.27 - windowW / 2;

    windows:
      Range(0, stories) reduce (acc, i) =>
        acc
        + common.makeWindow([leftX, windowY(i)], [windowW, windowH], lightColor)
        + common.makeWindow([rightX, windowY(i)], [windowW, windowH], lightColor)
      ~ [];

    base:
    {
      type: "line";
      name: "base";
      from: [wallLeft - roofOverhang, wallBottom];
      to: [wallLeft + houseWidth + roofOverhang, wallBottom];
      stroke: common.wallStroke;
      width: common.wallWidth;
    };

    eval [wall, roof] + windows + door + [base];
  };
}

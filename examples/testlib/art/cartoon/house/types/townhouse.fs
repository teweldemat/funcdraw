(options) =>
{
  anchor: options.anchor;
  houseWidth: options.width;
  stories: options.stories;
  doorOpen: options.doorOpen;
  lightColor: options.lightColor;

  eval if stories < 1 then error("expected stories >= 1") else
  {
    storyHeight: houseWidth * 0.6;
    bodyHeight: storyHeight * stories;
    wallLeft: anchor[0] - houseWidth / 2;
    wallBottom: anchor[1];
    wallTop: wallBottom + bodyHeight;

    wall:
    {
      type: "rect";
      name: "wall";
      position: [wallLeft, wallBottom];
      size: [houseWidth, bodyHeight];
      fill: "#fde68a";
      stroke: common.wallStroke;
      width: common.wallWidth;
    };

    parapetHeight: storyHeight * 0.12;
    parapet:
    {
      type: "rect";
      name: "roof";
      position: [wallLeft, wallTop - parapetHeight];
      size: [houseWidth, parapetHeight];
      fill: "#f97316";
      stroke: "#7c2d12";
      width: common.roofWidth;
    };

    doorWidth: houseWidth * 0.2;
    doorHeight: storyHeight * 0.75;
    doorPos: [anchor[0] - doorWidth / 2, wallBottom];
    door: common.makeDoor(doorPos, [doorWidth, doorHeight], doorOpen, lightColor);

    windowW: houseWidth * 0.14;
    windowH: storyHeight * 0.22;
    windowY: (i) => wallBottom + storyHeight * i + storyHeight * 0.52;
    x1: anchor[0] - houseWidth * 0.28 - windowW / 2;
    x2: anchor[0] - windowW / 2;
    x3: anchor[0] + houseWidth * 0.28 - windowW / 2;

    windows:
      Range(0, stories) reduce (acc, i) =>
        acc
        + common.makeWindow([x1, windowY(i)], [windowW, windowH], lightColor)
        + common.makeWindow([x2, windowY(i)], [windowW, windowH], lightColor)
        + common.makeWindow([x3, windowY(i)], [windowW, windowH], lightColor)
      ~ [];

    trim:
      Range(1, stories) map (i) =>
      {
        y: wallBottom + storyHeight * i;
        eval { type: "line"; name: "trim"; from: [wallLeft, y]; to: [wallLeft + houseWidth, y]; stroke: "#0f172a"; width: 0.2; };
      };

    eval [wall] + trim + [parapet] + windows + door;
  };
}

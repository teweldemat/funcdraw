(options) =>
{
  anchor: options.anchor;
  houseWidth: options.width;
  doorOpen: options.doorOpen;
  lightColor: options.lightColor;

  radius: houseWidth / 2;
  center: [anchor[0], anchor[1] + radius];

  dome:
  {
    type: "circle";
    name: "dome";
    center;
    radius;
    fill: "#e0f2fe";
    stroke: "#0f172a";
    width: 0.35;
  };

  doorWidth: houseWidth * 0.25;
  doorHeight: radius * 0.55;
  doorPos: [anchor[0] - doorWidth / 2, anchor[1]];
  door: common.makeDoor(doorPos, [doorWidth, doorHeight], doorOpen, lightColor);

  windowCenter: [anchor[0], anchor[1] + radius * 1.1];
  window:
  [
    { type: "circle"; name: "window"; center: windowCenter; radius: houseWidth * 0.06; fill: lightColor; stroke: common.windowStroke; width: common.windowWidth; }
  ];

  eval [dome] + window + door;
}

(localT) =>
{
  view: common.resolveViewAt(common.houseAnchor[0]);
  progress: common.ease01(localT / common.scene1Duration);
  darknessAlpha: 0.95 * (1 - progress);

  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  roofHeight: layout.storyHeight * 0.7;
  roofOverhang: common.houseWidth * 0.06;
  wallLeft: common.houseAnchor[0] - common.houseWidth / 2 - roofOverhang;
  wallRight: common.houseAnchor[0] + common.houseWidth / 2 + roofOverhang;
  wallTop: common.houseAnchor[1] + layout.storyHeight * common.houseStories;
  roofTop: wallTop + roofHeight;
  shadeFill: fd.color.alpha("#020617", darknessAlpha);

  windowLights: (layout, lightColor) =>
  {
    pad: layout.storyHeight * 0.03;
    glowFill: fd.color.alpha(lightColor, 0.25);
    eval
      layout.windows reduce (acc, w) =>
        acc
        + [
          { type: "rect"; name: "window-glow"; position: [w.position[0] - pad, w.position[1] - pad]; size: [w.size[0] + 2 * pad, w.size[1] + 2 * pad]; fill: glowFill; stroke: "none"; width: 0; },
          { type: "rect"; name: "window-light"; position: w.position; size: w.size; fill: lightColor; stroke: "none"; width: 0; }
        ]
      ~ [];
  };

  wallShade:
  {
    type: "rect";
    name: "wall-shade";
    position: [common.houseAnchor[0] - common.houseWidth / 2, common.houseAnchor[1]];
    size: [common.houseWidth, wallTop - common.houseAnchor[1]];
    fill: shadeFill;
    stroke: "none";
    width: 0;
  };

  roofShade:
  {
    type: "polygon";
    name: "roof-shade";
    points:
    [
      [wallLeft, wallTop],
      [wallRight, wallTop],
      [common.houseAnchor[0], roofTop]
    ];
    fill: shadeFill;
    stroke: "none";
    width: 0;
  };

  houseGraphics:
    common.house.types.cottage(
      {
        anchor: common.houseAnchor;
        width: common.houseWidth;
        stories: common.houseStories;
        doorOpen: 0;
        lightColor: "#0f172a";
      });

  lights: windowLights(layout, "#fef9c3");

  eval
  {
    view;
    graphics: backdrop(view, progress, t) + [houseGraphics, wallShade, roofShade, lights];
  };
}

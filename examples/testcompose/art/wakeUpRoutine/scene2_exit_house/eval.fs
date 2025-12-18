(localT) =>
{
  doorOpen: common.ease01(localT / common.scene2Duration);
  layout: common.houseLayout(common.houseAnchor, common.houseWidth, common.houseStories);
  anchor: actor.doorCharacterAnchor(layout);
  view: common.resolveViewAt(anchor[0]);

  standing: actor.standProfile(actor.characterMeasurements + actor.frontPose);
  characterGraphic: actor.character.static(anchor, standing, actor.charPalette,actor.skin);

  houseGraphics:
    common.house.types.cottage(
      {
        anchor: common.houseAnchor;
        width: common.houseWidth;
        stories: common.houseStories;
        doorOpen: doorOpen;
        lightColor: "#0f172a";
      });

  opening: houseGraphics filter (g) => g.name == "door-opening";
  doorParts: houseGraphics filter (g) => g.name == "door" or g.name == "door-knob";
  base: houseGraphics filter (g) => g.name == "base";
  rest: houseGraphics filter (g) => g.name != "door-opening" and g.name != "door" and g.name != "door-knob" and g.name != "base";

  eval
  {
    view;
    graphics: backdrop(view, 1, t) + [rest, opening, characterGraphic, doorParts, base];
  };
}

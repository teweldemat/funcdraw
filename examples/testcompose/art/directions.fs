{
  character: package("@funcdraw/testlib").cartoon.character;
  palette:
  {
    body: "#e5e7eb";
    limb: "#fbbf24";
  };
  backgroundColor: "#0b1120";

  anchors:
  {
    front: [-24, 0];
    left: [-8, 0];
    back: [8, 0];
    right: [24, 0];
  };

  groundY: character.skeleton.build(anchors.front, {}).leftLeg.to[1];

  front: character.static(anchors.front, { direction: "front"; }, palette);
  left: character.static(anchors.left, { direction: "left"; }, palette);
  back: character.static(anchors.back, { direction: "back"; }, palette);
  right: character.static(anchors.right, { direction: "right"; }, palette);

  ground:
  {
    type: "line";
    from: [anchors.front[0] - 8, groundY];
    to: [anchors.right[0] + 8, groundY];
    stroke: palette.limb;
    width: 0.35;
  };

  view:
  {
    left: anchors.front[0] - 18;
    bottom: groundY - 14;
    right: anchors.right[0] + 18;
    top: groundY + 44;
  };

  background:
  {
    type: "rect";
    from: [view.left, view.bottom];
    to: [view.right, view.top];
    fill: backgroundColor;
  };

  eval
  {
    view;
    graphics: [background, ground]
      + front
      + left
      + back
      + right;
  };
}

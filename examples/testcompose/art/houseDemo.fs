{
  house: package("@funcdraw/testlib").cartoon.house;

  view:
  {
    left: -70;
    bottom: -35;
    right: 70;
    top: 45;
  };

  cottage:
    house.types.cottage(
      {
        anchor: [-38, -25];
        width: 34;
        stories: 2;
        doorOpen: 0.1;
        lightColor: "#fef9c3";
      });

  townhouse:
    house.types.townhouse(
      {
        anchor: [5, -25];
        width: 28;
        stories: 3;
        doorOpen: 0.6;
        lightColor: "#a7f3d0";
      });

  igloo:
    house.types.igloo(
      {
        anchor: [42, -25];
        width: 26;
        stories: 999;
        doorOpen: 0.8;
        lightColor: "#93c5fd";
      });

  ground:
  {
    type: "line";
    from: [view.left, -25];
    to: [view.right, -25];
    stroke: "#334155";
    width: 1;
  };

  labelStyle:
  {
    fontSize: 2.2;
    color: "#94a3b8";
  };

  labels:
  [
    { type: "text"; text: "cottage"; position: [-53, -31]; fontSize: labelStyle.fontSize; color: labelStyle.color; },
    { type: "text"; text: "townhouse"; position: [-6, -31]; fontSize: labelStyle.fontSize; color: labelStyle.color; },
    { type: "text"; text: "igloo"; position: [35, -31]; fontSize: labelStyle.fontSize; color: labelStyle.color; }
  ];

  eval
  {
    view;
    graphics: [ground, cottage, townhouse, igloo, labels];
  };
}

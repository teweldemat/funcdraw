{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  leftPhase: (math.Sin(t) + 1) / 2;
  rightPhase: (math.Sin(5*t + math.Pi) + 1) / 2;
  leftFoot: [-3 + 2 * math.Cos(t), -14 + 6 * leftPhase];
  rightFoot: [3 + 2 * math.Cos(t + math.Pi), -14 + 6 * rightPhase];

  character: package("@funcdraw/testlib").cartoon.character.static(
    [0, 0],
    {
      leftLeg: { end: leftFoot; };
      rightLeg: { end: rightFoot; };
    },
    palette);

  eval
  {
    view:
    {
      left: -20;
      bottom: -22;
      right: 22;
      top: 26;
    };
    graphics: character;
  };
}

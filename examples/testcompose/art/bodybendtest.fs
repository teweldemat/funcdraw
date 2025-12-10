{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  bodyAngle: math.Pi / 2 + math.Sin(t) * 0.6;

  character: package("@funcdraw/testlib").cartoon.character([0, 0], { bodyAngle: bodyAngle; }, palette);

  eval
  {
    valueHooks: { t: t };
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

{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  character: package("@funcdraw/testlib").cartoon.character([0, 0], {}, palette);

  eval
  {
    view:
    {
      left: -12;
      bottom: -16;
      right: 15;
      top: 22;
    };
    graphics: character;
  };
}

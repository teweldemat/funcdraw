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
      left: -5;
      bottom: -5;
      right: 15;
      top: 20;
    };
    graphics: character;
  };
}

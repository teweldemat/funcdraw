{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };


  character: package("@funcdraw/testlib").cartoon.character([0, 0], {  }, palette);

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

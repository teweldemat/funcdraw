(stageT) =>
{
  ctx: scene6Context(stageT);
  eval
  {
    view: ctx.view;
    graphics:
      ctx.envLayer
      + ctx.busLayer
      + ctx.stopSign
      + ctx.bikeOnGround;
  };
}

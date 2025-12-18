(stageT) =>
{
  ctx: scene6Context(scene6Segments.doorOffset + stageT);
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

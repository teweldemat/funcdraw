(stageT) =>
{
  ctx: scene6Context(scene6Segments.walkOffset + stageT);
  eval
  {
    view: ctx.view;
    graphics:
      ctx.envLayer
      + ctx.stopSign
      + ctx.bikeOnGround
      + ctx.walkerShown;
  };
}

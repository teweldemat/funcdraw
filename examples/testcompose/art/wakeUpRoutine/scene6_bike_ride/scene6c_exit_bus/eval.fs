(stageT) =>
{
  ctx: scene6Context(scene6Segments.exitOffset + stageT);
  eval
  {
    view: ctx.view;
    graphics:
      ctx.envLayer
      + ctx.busLayer
      + ctx.stopSign
      + ctx.exitLayer
      + ctx.bikeOnGround;
  };
}

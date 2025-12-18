(stageT) =>
{
  ctx: scene6Context(scene6Segments.mountOffset + stageT);
  eval
  {
    view: ctx.view;
    graphics:
      ctx.envLayer
      + ctx.stopSign
      + ctx.bikeOnGround
      + ctx.walkerShown
      + ctx.riderMountShown;
  };
}

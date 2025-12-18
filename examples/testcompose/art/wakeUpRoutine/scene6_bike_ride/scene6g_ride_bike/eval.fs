(stageT) =>
{
  ctx: scene6Context(scene6Segments.rideOffset + stageT);
  eval
  {
    view: ctx.view;
    graphics:
      ctx.envLayer
      + ctx.stopSign
      + ctx.riderRideShown;
  };
}

{
  palette: modules.palette;
  canvas:{
    type:'rect';
    data:{
      position:[-38,-26];
      size:[76,52];
      fill: palette.background;
      stroke: palette.accent;
      width:0.6;
    };
  };
  grid:{
    type:'rect';
    data:{
      position:[-36,-24];
      size:[72,48];
      fill:'transparent';
      stroke: palette.muted;
      width:0.3;
    };
  };
  eval [canvas, grid, collections.hero, collections.metrics];
}

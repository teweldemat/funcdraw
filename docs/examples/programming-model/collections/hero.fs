{
  palette: modules.palette;
  heroPanel:{
    type:'rect';
    position:[-34,6];
    size:[68,30];
    fill: palette.surface;
    stroke: palette.accent;
    width:0.6;
  };
  heroStripe:{
    type:'rect';
    position:[-34,18];
    size:[68,4];
    fill: palette.accent;
  };
  heroBadge:{
    type:'rect';
    position:[-30,10];
    size:[32,6];
    fill: palette.highlight;
  };
  eval [heroPanel, heroStripe, heroBadge];
}

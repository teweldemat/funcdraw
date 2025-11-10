{
  palette: modules.palette;
  cardOne:{
    type:'rect';
    data:{
      position:[-26,-16];
      size:[18,12];
      fill: palette.surface;
      stroke: palette.muted;
      width:0.4;
    };
  };
  cardTwo:{
    type:'rect';
    data:{
      position:[-5,-16];
      size:[18,12];
      fill: palette.surface;
      stroke: palette.muted;
      width:0.4;
    };
  };
  cardThree:{
    type:'rect';
    data:{
      position:[16,-16];
      size:[18,12];
      fill: palette.surface;
      stroke: palette.muted;
      width:0.4;
    };
  };
  accent:{
    type:'rect';
    data:{
      position:[-26,-6];
      size:[60,2.5];
      fill: palette.accent;
    };
  };
  return [cardOne, cardTwo, cardThree, accent];
}

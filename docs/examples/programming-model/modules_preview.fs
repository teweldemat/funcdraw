{
  palette: modules.palette;
  swatchOne:{
    type:'rect';
    data:{ position:[-36,-6]; size:[12,12]; fill: palette.background; stroke:'#0f172a'; width:0.4; };
  };
  swatchTwo:{
    type:'rect';
    data:{ position:[-18,-6]; size:[12,12]; fill: palette.surface; stroke:'#0f172a'; width:0.4; };
  };
  swatchThree:{
    type:'rect';
    data:{ position:[0,-6]; size:[12,12]; fill: palette.accent; stroke:'#0f172a'; width:0.4; };
  };
  swatchFour:{
    type:'rect';
    data:{ position:[18,-6]; size:[12,12]; fill: palette.muted; stroke:'#0f172a'; width:0.4; };
  };
  swatchFive:{
    type:'rect';
    data:{ position:[36,-6]; size:[12,12]; fill: palette.highlight; stroke:'#0f172a'; width:0.4; };
  };
  return [swatchOne, swatchTwo, swatchThree, swatchFour, swatchFive];
}

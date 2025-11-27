(center, sideLength, style)=>
{
  centerPoint:center ?? [0,0];
  side:sideLength ?? 10;
  options:style ?? {};
  halfSide:side / 2;
  fillColor:options.fill ?? "#38bdf8";
  strokeColor:options.stroke ?? "#0f172a";
  strokeWidth:options.width ?? 0.5;
  centerX:centerPoint[0] ?? 0;
  centerY:centerPoint[1] ?? 0;
  eval
  {
    type:"rect";
    position:[centerX - halfSide, centerY - halfSide];
    size:[side, side];
    fill:fillColor;
    stroke:strokeColor;
    width:strokeWidth;
  }
}

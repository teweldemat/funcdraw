{
  defaultSize:{ width:40; height:30 };
  canvasData:canvas ?? { size:defaultSize };
  canvasWidthValue:canvasData.size.width ?? defaultSize.width;
  canvasHeightValue:canvasData.size.height ?? defaultSize.height;
  canvasWidth:if canvasWidthValue > 0 then canvasWidthValue else defaultSize.width;
  canvasHeight:if canvasHeightValue > 0 then canvasHeightValue else defaultSize.height;

  view:{
    left:0;
    bottom:0;
    right:canvasWidth;
    top:canvasHeight;
  };

  centerX:canvasWidth / 2;
  centerY:canvasHeight / 2;

  graphics:[verticalLine, horizontalLine, sizeLabel];

  verticalLine:{
    type:"line";
    from:[centerX, 0];
    to:[centerX, canvasHeight];
    stroke:"#38bdf8";
    width:1.5;
  };

  horizontalLine:{
    type:"line";
    from:[0, centerY];
    to:[canvasWidth, centerY];
    stroke:"#38bdf8";
    width:1.5;
  };

  sizeLabel:{
    type:"text";
    text:canvasWidth + " × " + canvasHeight;
    position:[2, canvasHeight - 100];
    fontSize:40;
    color:"#ffffff";
  };
}

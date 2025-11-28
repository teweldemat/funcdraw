{
  view:{
    left:-50;
    bottom:-10;
    right:50;
    top:40;
  };

  squareLib:package("@funcdraw/testlib");
  squareFn:squareLib.square;
  background:{
    type:"rect";
    position:[0,0];
    size:[40,40];
    fill:"#020617";
    stroke:"#0f172a";
    width:0.25;
  };

  aquaStyle:{
    fill:"#38bdf8";
    stroke:"#0f172a";
    width:0.5;
  };

  magentaStyle:{
    fill:"#f472b6";
    stroke:"#881337";
    width:0.6;
  };

  goldStyle:{
    fill:"#facc15";
    stroke:"#ca8a04";
    width:0.75;
  };

  limeStyle:{
    fill:"#4ade80";
    stroke:"#166534";
    width:0.5;
  };

  graphics:[
    background+man.graphics,
    squareLib.square([10,30], 8, aquaStyle),
    squareLib.square([30,30], 8, magentaStyle),
    squareFn([10,10], 12, goldStyle),
    squareFn([30,10], 6, limeStyle)
  ];
}

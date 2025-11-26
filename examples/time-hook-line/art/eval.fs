{
  view:{
    left:-12;
    bottom:-12;
    right:12;
    top:12;
  };

  graphics:[
    {
      type:"line";
      from:[0,0];
      to:[math.cos(angle) * length, math.sin(angle) * length];
      stroke:"#38bdf8";
      width:0.75;
    }
  ];

  angle:(t ?? 0);
  length:10;
}

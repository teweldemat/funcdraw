{
  stickman:package("@funcdraw/testlib").cartoon.stickman;
  createStickman:stickman.static;

  lineupX:constants.directions.lineup;
  lineupDirections:["left", "front", "back", "right"];
  groundY:0;

  defaultPose:createStickman({});
  anchorY:defaultPose.skeleton.position[1];

  createHero:(x, direction)=> createStickman({
    position:[x, anchorY];
    measurements:{
      torso:{ direction:direction; height:22; width:12 };
      head:{ direction:direction; verticalExtent:9 };
      legs:{
        left:{ effectorCoordinate:[-4, -22] };
        right:{ effectorCoordinate:[4, -22] };
      };
      hands:{
        left:{ effectorCoordinate:[-7.8, 4.7] };
        right:{ effectorCoordinate:[7.8, 4.7] };
      };
    };
  });

  heroes:[
    createHero(lineupX[0], lineupDirections[0]),
    createHero(lineupX[1], lineupDirections[1]),
    createHero(lineupX[2], lineupDirections[2]),
    createHero(lineupX[3], lineupDirections[3])
  ];

  labels:[
    { type:"text"; text:lineupDirections[0]; position:[lineupX[0], constants.directions.labelY]; fill:"#0f172a"; fontSize:12; align:"center" },
    { type:"text"; text:lineupDirections[1]; position:[lineupX[1], constants.directions.labelY]; fill:"#0f172a"; fontSize:12; align:"center" },
    { type:"text"; text:lineupDirections[2]; position:[lineupX[2], constants.directions.labelY]; fill:"#0f172a"; fontSize:12; align:"center" },
    { type:"text"; text:lineupDirections[3]; position:[lineupX[3], constants.directions.labelY]; fill:"#0f172a"; fontSize:12; align:"center" }
  ];

  baseline:{
    type:"line";
    from:[-constants.directions.baselineHalfSpan, groundY];
    to:[constants.directions.baselineHalfSpan, groundY];
    stroke:"#94a3b8";
    width:0.5;
  };

  heroGraphics:heroes[0].graphics + heroes[1].graphics + heroes[2].graphics + heroes[3].graphics;

  eval {
    view:{ left:-40; bottom:-30; right:70; top:90 };
    graphics:[baseline, heroGraphics, labels];
  };
}

{
  view:{ left:-400; bottom:-300; right:400; top:300 };
  zoomedInView:{ left:-50; bottom:-20; right:50; top:50 };
  fontSize:12;

  shared:{
    anchor:[0, 20];
    torso:{ height:22; width:12 };
    head:{ verticalExtent:9 };
    hands:{ left:[-7.8, 4.7]; right:[7.8, 4.7] };
    legs:{
      sideWalkOffsets:{ left:[0, -20]; right:[-8, -20] };
      profileOffsets:{ left:[-4, -18.6]; right:[4, -18.6] };
      lengths:{ upper:12.4; lower:11.6 };
    };
  };

  sideWalk:{ steps:10; stride:8 };

  profileTester:{
    view:{ left:-80; bottom:-20; right:80; top:110 };
    anchor:[0, 18.6];
    strideLength:12;
    stepCount:10;
    speed:0.02;
    legLengths:{ upper:12.4; lower:11.6 };
    leftOffset:[-4, -18.6];
    rightOffset:[4, -18.6];
    movingTarget:[12, 0];
  };

  profileSingle:{
    anchorY:18.8;
    fixedPoint:[-32, 0];
    movingStartPoint:[-48, 0];
    movingTargetPoint:[-16, 0];
  };

  profileWalking:{
    anchorBaseY:18.6;
    legLengths:{ upper:12.4; lower:11.6 };
    strideLength:12;
    midStepLift:1.8;
    anchorBob:0.9;
  };

  zoomAlternating:{
    anchorBase:[0, 20];
    fixedBase:-24;
    stepRise:2.4;
    movingSpan:6;
    zoomPerStep:0.8;
  };

  zoomDemo:{
    anchorBase:[0, 20];
    fixedLegDepth:-20;
    movingLegDepth:-18;
    movingFootTargetOffset:-20;
    zoom:1.65;
  };

  directions:{
    lineup:[-48, -16, 16, 48];
    labelY:-6;
    baselineHalfSpan:56;
  };

  legExercise:{
    heroPosition:[0, 20];
    footLiftScale:6.4;
    footLateralBase:4;
    handAttachmentY:44;
    handOffsetX:6;
    handLengths:{ upper:8; lower:6 };
    groundHalfSpan:380;
  };
}

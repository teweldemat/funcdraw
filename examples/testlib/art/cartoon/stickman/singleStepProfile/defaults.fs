{
  defaults:{
    position:[0,10];
    leftOffset:[-2,-11];
    rightOffset:[2,-11];
    handForward:3.9;
    handDrop:2.35;
  };

  defaultHandOffsets:{
    left:[-defaults.handForward, defaults.handDrop];
    right:[defaults.handForward, defaults.handDrop];
  };

  minReachRatio:0.9;
  maxVerticalAnchorDelta:1.2;

  eval {
    defaults:defaults;
    defaultHandOffsets:defaultHandOffsets;
    minReachRatio:minReachRatio;
    maxVerticalAnchorDelta:maxVerticalAnchorDelta;
  };
}

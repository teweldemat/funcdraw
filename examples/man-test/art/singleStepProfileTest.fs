{
  consts:constants;
  stickman:package("@funcdraw/testlib").cartoon.stickman;
  stepper:stickman.singleStepProfile;

  anchor:consts.shared.anchor;
  view:consts.zoomedInView;
  stride:8;

  leftOffset:consts.shared.legs.profileOffsets.left;
  rightOffset:consts.shared.legs.profileOffsets.right;
  targetFoot:[
    anchor[0] + rightOffset[0] + stride,
    anchor[1] + rightOffset[1]
  ];

  progressRaw:if t = null then 0 else t;
  progress:math.max(0, math.min(1, progressRaw));

  baseMeasurements:{
    torso:{ direction:"right"; width:consts.shared.torso.width; height:consts.shared.torso.height };
    head:{ direction:"right"; verticalExtent:consts.shared.head.verticalExtent };
    hands:{
      left:{ effectorCoordinate:consts.shared.hands.left; upperLength:consts.legExercise.handLengths.upper; lowerLength:consts.legExercise.handLengths.lower };
      right:{ effectorCoordinate:consts.shared.hands.right; upperLength:consts.legExercise.handLengths.upper; lowerLength:consts.legExercise.handLengths.lower };
    };
    legs:{
      left:{ effectorCoordinate:leftOffset; upperLength:consts.shared.legs.lengths.upper; lowerLength:consts.shared.legs.lengths.lower; positiveBend:true };
      right:{ effectorCoordinate:rightOffset; upperLength:consts.shared.legs.lengths.upper; lowerLength:consts.shared.legs.lengths.lower; positiveBend:true };
    };
  };

  step:stepper({
    position:anchor;
    movingSide:"left";
    movingFeetTargetPoint:targetFoot;
    progress:progress;
    measurements:baseMeasurements;
    handSwing:{ enabled:false };
  });

  
  hero:stickman.static({
    position:step.position;
    measurements:step.measurements;
  });

  eval {
    view:view;
    graphics:hero.graphics;
  };
}

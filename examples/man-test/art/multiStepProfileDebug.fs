{
  cartoon:package("@funcdraw/testlib").cartoon;
  stickman:cartoon.stickman;
  walkBuilder:stickman.multiStepProfile;

  strideLength:constants.profileTester.strideLength;
  anchorBase:constants.profileTester.anchor;
  legLengths:constants.profileTester.legLengths;
  leftOffset:constants.profileTester.leftOffset;
  rightOffset:constants.profileTester.rightOffset;
  stepCount:constants.profileTester.stepCount;
  walkSpeed:constants.profileTester.speed;
  walkPhase:walkSpeed * t;
  walkProgress:walkPhase - math.floor(walkPhase);
  totalDisplacement:strideLength * stepCount;

  baseMeasurements:{
    torso:{ direction:"right"; height:20; width:2 };
    head:{ direction:"right"; verticalExtent:5 };
    hands:{
      left:{ effectorCoordinate:constants.shared.hands.left,upperLength:8,lowerLength:8 };
      right:{ effectorCoordinate:constants.shared.hands.right,upperLength:8,lowerLength:8  };
    };
    legs:{
      left:{ upperLength:legLengths.upper; lowerLength:legLengths.lower; effectorCoordinate:leftOffset };
      right:{ upperLength:legLengths.upper; lowerLength:legLengths.lower; effectorCoordinate:rightOffset };
    };
  };

  walkPose:walkBuilder({
    initialPosition:anchorBase;
    initialMeasurements:baseMeasurements;
    displacement:totalDisplacement;
    progress:walkProgress;
    direction:"right";
    strideLength:strideLength;
    handSwing:{ enabled:true; mode:"mirror"; amplitude:14; lift:1.8; forwardOffset:0 };
    debug:true;
  });

  anchorText:format("anchor=" + walkPose.position);
  leftHandText:format("LHand=" + walkPose.measurements.hands.left.effectorCoordinate);
  rightHandText:format("RHand=" + walkPose.measurements.hands.right.effectorCoordinate);
  leftFootText:format("LFoot=" + walkPose.measurements.legs.left.effectorCoordinate);
  rightFootText:format("RFoot=" + walkPose.measurements.legs.right.effectorCoordinate);
  debugStep:if walkPose.debug = null then null else walkPose.debug.debugStep;
  debugText:if debugStep = null then "no debug" else format(debugStep);
  historyText:if walkPose.debug = null or walkPose.debug.history = null then "no history" else format(walkPose.debug.history);
  sampleText:"sample step omitted";

  eval {
    view:constants.zoomedInView;
    graphics:[
      { type:"text"; text:anchorText; position:[-48,46]; size:3; fill:"#0f172a" };
      { type:"text"; text:leftHandText; position:[-48,42]; size:3; fill:"#0f172a" };
      { type:"text"; text:rightHandText; position:[-48,38]; size:3; fill:"#0f172a" };
      { type:"text"; text:leftFootText; position:[-48,34]; size:3; fill:"#0f172a" };
      { type:"text"; text:rightFootText; position:[-48,30]; size:3; fill:"#0f172a" };
      { type:"text"; text:debugText; position:[-48,24]; size:2.7; fill:"#0f172a" };
      { type:"text"; text:historyText; position:[-48,18]; size:2.4; fill:"#0f172a" };
      { type:"text"; text:sampleText; position:[-48,12]; size:2.4; fill:"#0f172a" };
    ];
  };

  applyLegOffsets:(measurements, anchor, leftFoot, rightFoot, defaults)=> {
    base:{} + (measurements ?? {});
    legs:{} + (base.legs ?? {});
    anchorPoint:if anchor = null then [0,0] else anchor;
    leftPoint:if leftFoot = null then [anchorPoint[0] + defaults.left[0], anchorPoint[1] + defaults.left[1]] else leftFoot;
    rightPoint:if rightFoot = null then [anchorPoint[0] + defaults.right[0], anchorPoint[1] + defaults.right[1]] else rightFoot;
    leftOffset:[leftPoint[0] - anchorPoint[0], leftPoint[1] - anchorPoint[1]];
    rightOffset:[rightPoint[0] - anchorPoint[0], rightPoint[1] - anchorPoint[1]];
    eval base + {
      legs:{
        left:(legs.left ?? {}) + { effectorCoordinate:leftOffset };
        right:(legs.right ?? {}) + { effectorCoordinate:rightOffset };
      }
    };
  };
}

{
  cartoon:package("@funcdraw/testlib").cartoon;
  stickman:cartoon.stickman;
  staticBuilder:stickman.static;
  walkBuilder:multiStepProfileUsingSingleStep;

  view:constants.zoomedInView;

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

  poseAnchor:walkPose.position;
  poseMeasurements:walkPose.measurements;
  posed:staticBuilder({
    position:[poseAnchor[0] + 0, poseAnchor[1] + 0];
    measurements:{} + poseMeasurements;
  });
  debugText:null;

  leftFoot:addPoints(poseAnchor, poseMeasurements.legs.left.effectorCoordinate);
  rightFoot:addPoints(poseAnchor, poseMeasurements.legs.right.effectorCoordinate);
  targetAnchor:[anchorBase[0] + totalDisplacement, anchorBase[1]];
  debugStep:if walkPose.debug = null then null else walkPose.debug.debugStep;
  debugHistory:if walkPose.debug = null then null else walkPose.debug.history;
  debugGraphics:if debugStep = null then [] else [
    {
      type:"text";
      position:[view.left + 2, view.top - 4];
      text:format(["step", debugStep.stepIndex, "p", debugStep.stepProgress]);
      fontSize:3.2;
      color:"#0f172a";
    },
    {
      type:"text";
      position:[view.left + 2, view.top - 8];
      text:format(["anchor", debugStep.anchorAfter]);
      fontSize:3.2;
      color:"#0f172a";
    },
    {
      type:"text";
      position:[view.left + 2, view.top - 12];
      text:format(["start", debugStep.movingStart, "target", debugStep.movingTarget]);
      fontSize:3.2;
      color:"#0f172a";
    },
    {
      type:"text";
      position:[view.left + 2, view.top - 16];
      text:format(["anchorBefore", debugStep.anchorBefore, "stride", debugStep.strideMagnitude]);
      fontSize:3.2;
      color:"#0f172a";
    },
    {
      type:"text";
      position:[view.left + 2, view.top - 20];
      text:format(debugHistory);
      fontSize:3.2;
      color:"#0f172a";
    }
  ];

  eval {
    view:view;
    graphics:createGroundLine(view.left, view.right) +
      posed.graphics +
      createMarker(leftFoot, "#10b981") +
      createMarker(rightFoot, "#94a3b8") +
      createMarker(targetAnchor, "#facc15") +
      createMarker(poseAnchor, "#fb923c") +
      debugGraphics;
    debug:walkPose;
  };

  createGroundLine:(minX, maxX)=> [
    { type:"line"; from:[minX, 0]; to:[maxX, 0]; stroke:"#94a3b8"; width:0.5 }
  ];

  createMarker:(point, fill)=> [
    {
      type:"circle";
      center:point;
      radius:0.6;
      fill:fill;
      stroke:"#0f172a";
      width:0.18;
    }
  ];

  addPoints:(a, b)=> [a[0] + b[0], a[1] + b[1]];
}

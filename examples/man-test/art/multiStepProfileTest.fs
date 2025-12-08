{
  cartoon:package("@funcdraw/testlib").cartoon;
  stickman:cartoon.stickman;
  walkBuilder:stickman.multiStepProfile;
  staticBuilder:stickman.static;

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
  posed:staticBuilder({ position:poseAnchor; measurements:poseMeasurements });
  debugText:null;

  leftFoot:addPoints(poseAnchor, poseMeasurements.legs.left.effectorCoordinate);
  rightFoot:addPoints(poseAnchor, poseMeasurements.legs.right.effectorCoordinate);
  targetAnchor:[anchorBase[0] + totalDisplacement, anchorBase[1]];

  eval {
    view:view;
    graphics:createGroundLine(view.left, view.right) +
      posed.graphics +
      createMarker(leftFoot, "#10b981") +
      createMarker(rightFoot, "#94a3b8") +
      createMarker(targetAnchor, "#facc15") +
      createMarker(poseAnchor, "#fb923c");
  };

  createGroundLine:(minX, maxX)=> [
    { type:"line"; from:[minX, 0]; to:[maxX, 0]; stroke:"#94a3b8"; width:0.5 }
  ];

  createMarker:(point, fill, radius)=> {
    markerFill:if fill = null then "#0f172a" else fill;
    markerRadius:if radius = null then 0.6 else radius;
    eval [
      {
        type:"circle";
        center:point;
        radius:markerRadius;
        fill:markerFill;
        stroke:"#0f172a";
        width:if markerRadius * 0.3 > 0.16 then markerRadius * 0.3 else 0.16;
      }
    ];
  };
  addPoints:(a, b)=> [a[0] + b[0], a[1] + b[1]];
  clamp01:(value)=> {
    eval if value != value then 0 else if value < 0 then 0 else if value > 1 then 1 else value;
  };

}

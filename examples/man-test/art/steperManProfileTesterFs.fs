{
  
  cartoonLibrary:package("@funcdraw/testlib").cartoon;
  stickmanModule:cartoonLibrary.stickman;
  steperBuilder:stickmanModule.steperManProfile;
  staticBuilder:stickmanModule.static;

  view:constants.zoomedInView;

  
  timeValue:0.2*t;
  stepIndex:math.floor(timeValue);
  stepPhase:timeValue - stepIndex;
  progress:if stepPhase < 0 then 0 else if stepPhase > 1 then 1 else stepPhase;
  movingSide:if stepIndex % 2 = 0 then "left" else "right";
  strideLength:constants.profileTester?.strideLength ?? 12;
  anchorBase:constants.profileTester?.anchor ?? [0, 18.6];
  loopIndex:stepIndex % 10;
  anchor:[anchorBase[0] + loopIndex * strideLength, anchorBase[1]];
  legLengths:constants.profileTester?.legLengths ?? { upper:12.4; lower:11.6 };
  leftOffset:constants.profileTester?.leftOffset ?? [-4, -18.6];
  rightOffset:constants.profileTester?.rightOffset ?? [4, -18.6];
  fixedFoot:addPoints(anchor, if movingSide = "left" then rightOffset else leftOffset);
  movingStart:addPoints(anchor, if movingSide = "left" then leftOffset else rightOffset);
  movingTarget:addPoints(movingStart, [strideLength, 0]);

  baseMeasurements:{
    torso:{ direction:"right"; height:constants.shared.torso.height; width:constants.shared.torso.width };
    head:{ direction:"right"; verticalExtent:constants.shared.head.verticalExtent};
    hands:{
      left:{ effectorCoordinate:constants.shared.hands.left };
      right:{ effectorCoordinate:constants.shared.hands.right };
    };
    legs:{
      left:{ upperLength:legLengths.upper; lowerLength:legLengths.lower; effectorCoordinate:leftOffset };
      right:{ upperLength:legLengths.upper; lowerLength:legLengths.lower; effectorCoordinate:rightOffset };
    };
  };

  stepPose:steperBuilder({
    position:anchor;
    measurements:baseMeasurements;
    movingSide:movingSide;
    movingFeetTargetPoint:movingTarget;
    progress:progress;
  });

  poseAnchor:stepPose.position;
  poseMeasurements: stepPose.measurements;
  posed:staticBuilder({ position:poseAnchor; measurements:poseMeasurements });

  movingPoint:{
    clamped:progress;
    baseX:movingStart[0] + (movingTarget[0] - movingStart[0]) * clamped;
    baseY:movingStart[1] + (movingTarget[1] - movingStart[1]) * clamped;
    dx:movingTarget[0] - movingStart[0];
    dy:movingTarget[1] - movingStart[1];
    distance:math.sqrt(dx * dx + dy * dy);
    heightCandidate:distance * 0.25;
    height:if heightCandidate > 1.5 then heightCandidate else 1.5;
    lift:math.sin(math.pi * clamped) * height;
    eval [baseX, baseY + lift];
  };

  eval if missingBuilders then {
    view:view;
    graphics:[
      {
        type:"text";
        text:"steperManProfile/static not available – install @funcdraw/testlib";
        position:[0, 8];
        align:"center";
        fontSize:12;
        fill:"#ef4444";
      }
    ];
  } else {
    view:view;
    graphics:createGroundLine(view.left, view.right) +
      posed.graphics +
      createMarker(fixedFoot, "#10b981") +
      createMarker(movingStart, "#94a3b8") +
      createMarker(movingTarget, "#facc15") +
      createMarker(movingPoint, "#fb923c") +
      createMarker([progress * 10, view.bottom + 5], "#ff0000", 0.4);
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

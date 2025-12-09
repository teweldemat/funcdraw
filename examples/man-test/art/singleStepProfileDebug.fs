{
  cartoon:package("@funcdraw/testlib").cartoon;
  stickman:cartoon.stickman;

  view:constants.profileTester.view;
  anchor:[0, 18.6];
  legLengths:constants.profileTester.legLengths;
  leftOffset:constants.profileTester.leftOffset;
  rightOffset:constants.profileTester.rightOffset;
  movingTarget:[11, 0];
  handSwing:{ enabled:true; mode:"mirror"; amplitude:14; lift:1.8; forwardOffset:0 };

  measurements:{
    torso:{ direction:"right"; height:20; width:2 };
    head:{ direction:"right"; verticalExtent:5 };
    hands:{
      left:{ effectorCoordinate:constants.shared.hands.left,upperLength:8,lowerLength:8 };
      right:{ effectorCoordinate:constants.shared.hands.right,upperLength:8,lowerLength:8 };
    };
    legs:{
      left:{ upperLength:legLengths.upper; lowerLength:legLengths.lower; effectorCoordinate:leftOffset };
      right:{ upperLength:legLengths.upper; lowerLength:legLengths.lower; effectorCoordinate:rightOffset };
    };
  };

  poseFull:stickman.singleStepProfile({
    position:anchor;
    measurements:measurements;
    movingSide:"left";
    movingFeetTargetPoint:movingTarget;
    progress:1;
    handSwing:handSwing;
    disableStatic:true;
  });

  posePartial:stickman.singleStepProfile({
    position:anchor;
    measurements:measurements;
    movingSide:"left";
    movingFeetTargetPoint:movingTarget;
    progress:0.6;
    handSwing:handSwing;
    disableStatic:true;
  });

  debugPoint:(pose)=> {
    step:pose.step;
    anchors:pose.anchorCandidates;
    eval {
      anchor:pose.position;
      moving:step.movingPoint;
      fixed:step.fixedPoint;
      target:movingTarget;
      anchorA:anchors.a;
      anchorB:anchors.b;
    };
  };

  debugFull:debugPoint(poseFull);
  debugPartial:debugPoint(posePartial);

  eval {
    view:view;
    graphics:createGround(view.left, view.right) +
      createMarker(debugPartial.anchor, "#10b981") +
      createMarker(debugFull.anchor, "#f97316") +
      createMarker(debugFull.moving, "#2563eb") +
      createMarker(debugFull.fixed, "#111827") +
      createMarker(movingTarget, "#facc15") +
      createText([view.left + 2, view.top - 4], ["partial anchor", debugPartial.anchor, "moving", debugPartial.moving]) +
      createText([view.left + 2, view.top - 8], ["partial anchors", debugPartial.anchorA, debugPartial.anchorB]) +
      createText([view.left + 2, view.top - 14], ["full anchor", debugFull.anchor, "moving", debugFull.moving]) +
      createText([view.left + 2, view.top - 18], ["full anchors", debugFull.anchorA, debugFull.anchorB]);
  };

  createGround:(minX, maxX)=> [
    { type:"line"; from:[minX, 0]; to:[maxX, 0]; stroke:"#94a3b8"; width:0.5 }
  ];

  createMarker:(point, fill)=> [
    { type:"circle"; center:point; radius:0.7; fill:fill; stroke:"#0f172a"; width:0.2 }
  ];

  createText:(position, textValue)=> [
    { type:"text"; position:position; text:textValue; fontSize:4; color:"#0f172a" }
  ];
}

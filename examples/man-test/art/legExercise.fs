{
  cartoon:package("@funcdraw/testlib").cartoon;
  stickman:cartoon.stickman;
  createStickman:stickman.static;

  settings:constants.legExercise;
  heroPosition:settings.heroPosition;
  footLiftScale:settings.footLiftScale;
  footLateralBase:settings.footLateralBase;
  footLateralSwing:footLateralBase * 0.3;
  handAttachmentY:settings.handAttachmentY;
  handOffsetX:settings.handOffsetX;
  handLengths:settings.handLengths;
  handReach:handLengths.upper + handLengths.lower;
  groundHalfSpan:settings.groundHalfSpan;
  handPathColor:"#06b6d4";
  legPathColor:"#22c55e";
  handPathWidth:0.4;
  legPathWidth:0.5;

  time:t;
  phase:time * 3;
  leftLift:math.max(math.sin(phase), 0);
  rightLift:math.max(math.sin(phase + math.pi), 0);

  reference:createStickman({ position:heroPosition });
  referenceSkeleton:reference.skeleton;

  resolveLegBaseDrop:(skeleton, side)=> {
    leg:(if side = "left" then skeleton.legs.left else skeleton.legs.right).lengths;
    eval -(leg.upper + leg.lower);
  };

  leftBaseDrop:resolveLegBaseDrop(referenceSkeleton, "left");
  rightBaseDrop:resolveLegBaseDrop(referenceSkeleton, "right");

  leftFootLift:leftBaseDrop + leftLift * footLiftScale;
  leftFootOffsetX:-footLateralBase + leftLift * footLateralSwing;
  rightFootLift:rightBaseDrop + rightLift * footLiftScale;
  rightFootOffsetX:footLateralBase - rightLift * footLateralSwing;
  leftLegTarget:[leftFootOffsetX, leftFootLift];
  rightLegTarget:[rightFootOffsetX, rightFootLift];

  armSpeed:time * 1.2;
  handAnchorYOffset:handAttachmentY - heroPosition[1];
  leftHandAnchorWorld:[heroPosition[0] - handOffsetX, handAttachmentY];
  rightHandAnchorWorld:[heroPosition[0] + handOffsetX, handAttachmentY];
  leftHandEffector:[
    -handOffsetX + handReach * math.cos(armSpeed),
    handAnchorYOffset + handReach * math.sin(armSpeed)
  ];
  rightHandEffector:[
    handOffsetX + handReach * math.cos(armSpeed + math.pi),
    handAnchorYOffset + handReach * math.sin(armSpeed + math.pi)
  ];

  hero:createStickman({
    position:heroPosition;
    measurements:{
      torso:{ direction:"front" };
      hands:{
        left:{ effectorCoordinate:leftHandEffector; upperLength:handLengths.upper; lowerLength:handLengths.lower };
        right:{ effectorCoordinate:rightHandEffector; upperLength:handLengths.upper; lowerLength:handLengths.lower };
      };
      legs:{
        left:{ effectorCoordinate:leftLegTarget,positiveBend:false };
        right:{ effectorCoordinate:rightLegTarget };
      };
    };
  });

  ground:{
    type:"line";
    from:[-groundHalfSpan, 0];
    to:[groundHalfSpan, 0];
    stroke:"#94a3b8";
    width:0.6;
  };

  createFootPath:(basePoint, peakPoint, color)=> [
    { type:"line"; from:basePoint; to:peakPoint; stroke:color; width:legPathWidth; dash:[1.1, 0.7] },
    { type:"line"; from:peakPoint; to:basePoint; stroke:color; width:legPathWidth; dash:[1.1, 0.7] }
  ];

  leftFootBase:[heroPosition[0] - footLateralBase, heroPosition[1] + leftBaseDrop];
  leftFootPeak:[heroPosition[0] - footLateralBase + footLateralSwing, heroPosition[1] + leftBaseDrop + footLiftScale];
  rightFootBase:[heroPosition[0] + footLateralBase, heroPosition[1] + rightBaseDrop];
  rightFootPeak:[heroPosition[0] + footLateralBase - footLateralSwing, heroPosition[1] + rightBaseDrop + footLiftScale];
  legPaths:[
    createFootPath(leftFootBase, leftFootPeak, legPathColor),
    createFootPath(rightFootBase, rightFootPeak, legPathColor)
  ];

  handPaths:[
    { type:"circle"; center:leftHandAnchorWorld; radius:handReach; stroke:handPathColor; width:handPathWidth; opacity:0.65; fill:"#00000000" },
    { type:"circle"; center:rightHandAnchorWorld; radius:handReach; stroke:handPathColor; width:handPathWidth; opacity:0.65; fill:"#00000000" }
  ];

  caption:{
    type:"text";
    text:"Left leg lift exercise";
    position:[0, -3];
    fill:"#0f172a";
    fontSize:12;
    align:"center";
  };

  createDebugCross:(center, size)=> {
    half:size * 0.5;
    stroke:"#ffffff";
    width:math.max(size * 0.18, 0.18);
    eval [
      { type:"line"; from:[center[0] - half, center[1]]; to:[center[0] + half, center[1]]; stroke:stroke; width:width },
      { type:"line"; from:[center[0], center[1] - half]; to:[center[0], center[1] + half]; stroke:stroke; width:width }
    ];
  };

  finalSkeleton:hero.skeleton;
  finalHands:finalSkeleton.hands;
  finalLegs:finalSkeleton.legs;

  finalLeftShoulder:finalHands.left.attachmentPoint;
  finalRightShoulder:finalHands.right.attachmentPoint;
  finalLeftHandTarget:finalHands.left.targetPoint;
  finalRightHandTarget:finalHands.right.targetPoint;
  finalLeftLegTarget:finalLegs.left.targetPoint;
  finalRightLegTarget:finalLegs.right.targetPoint;

  debugPoints:[
    createDebugCross(finalLeftShoulder, 1.6),
    createDebugCross(finalRightShoulder, 1.6),
    createDebugCross(finalLeftHandTarget, 1.6),
    createDebugCross(finalRightHandTarget, 1.6),
    createDebugCross(finalLeftLegTarget, 1.8),
    createDebugCross(finalRightLegTarget, 1.8)
  ];

  createDebugDot:(center)=> {
      type:"circle";
      center:center;
      radius:0.1;
      fill:"#dc2626";
      stroke:"#991b1b";
      width:0.08;
  };

  skeletonDots:[
    createDebugDot(finalLeftShoulder);
    createDebugDot(finalRightShoulder);
    createDebugDot(finalLeftHandTarget);
    createDebugDot(finalRightHandTarget);
    createDebugDot(finalLeftLegTarget);
    createDebugDot(finalRightLegTarget);
  ];

  eval {
    view:constants.zoomedInView;
    graphics:[ground,
      legPaths,
      handPaths,
      hero.graphics,      
      debugPoints,
      skeletonDots,
      caption];    
  };
}

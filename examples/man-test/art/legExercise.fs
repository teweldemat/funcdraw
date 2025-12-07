{
  cartoon:package("@funcdraw/testlib").cartoon;
  stickman:cartoon.stickman;
  createStickman:stickman.static;

  time:t;
  phase:time * 3;
  leftLift:math.max(math.sin(phase), 0);
  rightLift:math.max(math.sin(phase + math.pi), 0);
  heroPosition:[0, 20];

  reference:createStickman({ position:heroPosition });
  referenceSkeleton:reference.skeleton;

  resolveLegBaseDrop:(skeleton, side)=> {
    leg:(if side = "left" then skeleton.legs.left else skeleton.legs.right).lengths;
    eval -(leg.upper + leg.lower);
  };

  leftBaseDrop:resolveLegBaseDrop(referenceSkeleton, "left");
  rightBaseDrop:resolveLegBaseDrop(referenceSkeleton, "right");

  leftFootLift:leftBaseDrop + leftLift * 6.4;
  leftFootOffsetX:-4 + leftLift * 1.2;
  rightFootLift:rightBaseDrop + rightLift * 6.4;
  rightFootOffsetX:4 - rightLift * 1.2;
  leftLegTarget:[leftFootOffsetX, leftFootLift];
  rightLegTarget:[rightFootOffsetX, rightFootLift];

  baseHero:createStickman({
    position:heroPosition;
    measurements:{
      torso:{ direction:"front" };
      legs:{
        left:{ effectorCoordinate:leftLegTarget };
        right:{ effectorCoordinate:rightLegTarget };
      };
    };
  });

  armSpeed:time * 1.2;
  leftAttachment:baseHero.skeleton.hands.left.attachmentPoint;
  rightAttachment:baseHero.skeleton.hands.right.attachmentPoint;
  leftArmLength:baseHero.skeleton.hands.left.lengths.upper + baseHero.skeleton.hands.left.lengths.lower;
  rightArmLength:baseHero.skeleton.hands.right.lengths.upper + baseHero.skeleton.hands.right.lengths.lower;

  leftHandTargetWorld:[
    leftAttachment[0] + leftArmLength * math.cos(armSpeed),
    leftAttachment[1] + leftArmLength * math.sin(armSpeed)
  ];
  rightHandTargetWorld:[
    rightAttachment[0] + rightArmLength * math.cos(armSpeed + math.pi),
    rightAttachment[1] + rightArmLength * math.sin(armSpeed + math.pi)
  ];

  leftHandEffector:[
    leftHandTargetWorld[0] - heroPosition[0],
    leftHandTargetWorld[1] - heroPosition[1]
  ];
  rightHandEffector:[
    rightHandTargetWorld[0] - heroPosition[0],
    rightHandTargetWorld[1] - heroPosition[1]
  ];

  hero:createStickman({
    position:heroPosition;
    measurements:{
      torso:{ direction:"front" };
      hands:{
        left:{ effectorCoordinate:leftHandEffector };
        right:{ effectorCoordinate:rightHandEffector };
      };
      legs:{
        left:{ effectorCoordinate:leftLegTarget };
        right:{ effectorCoordinate:rightLegTarget };
      };
    };
  });

  ground:{
    type:"line";
    from:[-380, 0];
    to:[380, 0];
    stroke:"#94a3b8";
    width:0.6;
  };

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
    ...createDebugCross(finalLeftShoulder, 1.6),
    ...createDebugCross(finalRightShoulder, 1.6),
    ...createDebugCross(finalLeftHandTarget, 1.6),
    ...createDebugCross(finalRightHandTarget, 1.6),
    ...createDebugCross(finalLeftLegTarget, 1.8),
    ...createDebugCross(finalRightLegTarget, 1.8)
  ];

  createDebugDot:(center)=> {
    eval {
      type:"circle";
      center:center;
      radius:0.1;
      fill:"#dc2626";
      stroke:"#991b1b";
      width:0.08;
    };
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
    graphics:[
      ground,
      ...hero.graphics,
      ...debugPoints,
      ...skeletonDots,
      caption
    ];
  };
}

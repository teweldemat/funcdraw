{
  helpers:cartoon.helpers;
  vector:helpers.vector;
  geometry:geometry;
  swing:swing;
  defaultsModule:defaults;
  skeletonModule:skeleton;

  defaults:defaultsModule.defaults;
  defaultHandOffsets:defaultsModule.defaultHandOffsets;
  minReachRatio:defaultsModule.minReachRatio;
  maxVerticalAnchorDelta:defaultsModule.maxVerticalAnchorDelta;

  baseStaticBuilder:static;
  distanceHelper:helpers.distance;
  clampHelper:helpers.clamp;
  addPoints:vector.add;
  subtractPoints:vector.subtract;
  averagePoints:vector.average;
  clampSymmetric:vector.clampSymmetric;
  scaleVectorToLength:vector.scaleToLength;
  normalizeSide:geometry.normalizeSide;
  normalizeDirectionValue:geometry.normalizeDirectionValue;
  clamp01:geometry.clamp01;
  lerpPoint:geometry.lerpPoint;
  computeArcPoint:geometry.computeArcPoint;
  distanceBetweenPoints:(a, b)=> geometry.distanceBetweenPoints(a, b, distanceHelper);
  resolveArcHeight:(start, end)=> geometry.resolveArcHeight(start, end, distanceBetweenPoints);
  clampAnchorVerticalDrift:(candidate, baseline)=> geometry.clampAnchorVerticalDrift(candidate, baseline, defaults.position, maxVerticalAnchorDelta, clampHelper);

  singleStepProfile:(optionsInput)=> {
    options:optionsInput ?? {};
    includeStatic:options.disableStatic != true;
    anchorBase:if options.position = null then defaults.position else options.position;
    measurementInput:options.measurements ?? {};
    progress:clamp01(options.progress);
    movingSideFromOptions:normalizeSide(options.movingSide ?? options.movingFeet, null);
    fixedSideFromOptions:normalizeSide(options.fixedFeet, null);
    movingSide:if movingSideFromOptions != null then movingSideFromOptions else if fixedSideFromOptions = "left" then "right" else if fixedSideFromOptions = "right" then "left" else "left";
    fixedSide:if movingSide = "left" then "right" else "left";

    skeletonContext:skeletonModule.build({});
    baseSkeleton:skeletonContext.skeleton ?? {};
    skeletonPosition:if baseSkeleton.position = null then defaults.position else baseSkeleton.position;

    defaultLeftFoot:if baseSkeleton.legs?.left?.targetPoint = null then addPoints(skeletonPosition, defaults.leftOffset) else baseSkeleton.legs.left.targetPoint;
    defaultRightFoot:if baseSkeleton.legs?.right?.targetPoint = null then addPoints(skeletonPosition, defaults.rightOffset) else baseSkeleton.legs.right.targetPoint;
    defaultOffsets:{
      left:subtractPoints(defaultLeftFoot, skeletonPosition);
      right:subtractPoints(defaultRightFoot, skeletonPosition);
    };

    defaultLeftHandPoint:if baseSkeleton.hands?.left?.targetPoint = null then addPoints(skeletonPosition, defaultHandOffsets.left) else baseSkeleton.hands.left.targetPoint;
    defaultRightHandPoint:if baseSkeleton.hands?.right?.targetPoint = null then addPoints(skeletonPosition, defaultHandOffsets.right) else baseSkeleton.hands.right.targetPoint;
    defaultHandOffsetsBySide:{
      left:subtractPoints(defaultLeftHandPoint, skeletonPosition);
      right:subtractPoints(defaultRightHandPoint, skeletonPosition);
    };

    baseTorsoDirection:normalizeDirectionValue(baseSkeleton.torso?.direction, "front");
    baseTorsoMeasurements:baseSkeleton.torso ?? {};
    defaultTorsoWidth:baseTorsoMeasurements.width ?? 6;
    defaultTorsoHeight:baseTorsoMeasurements.height ?? 11;
    defaultShoulderExtension:math.max(baseTorsoMeasurements.shoulderExtension ?? defaultTorsoWidth * 0.15, 0);
    defaultShoulderOffsets:resolveShoulderOffsets({
      width:defaultTorsoWidth;
      height:defaultTorsoHeight;
      shoulderExtension:defaultShoulderExtension;
      direction:baseTorsoDirection;
    });
    defaultHandReach:{
      left:swing.resolveDefaultHandReach("left", defaultShoulderOffsets, baseSkeleton, defaultHandOffsetsBySide, distanceBetweenPoints);
      right:swing.resolveDefaultHandReach("right", defaultShoulderOffsets, baseSkeleton, defaultHandOffsetsBySide, distanceBetweenPoints);
    };

    baseLegs:measurementInput.legs ?? {};
    legOffsets:{
      left:if baseLegs.left?.effectorCoordinate = null then defaultOffsets.left else baseLegs.left.effectorCoordinate;
      right:if baseLegs.right?.effectorCoordinate = null then defaultOffsets.right else baseLegs.right.effectorCoordinate;
    };

    defaultFixedWorld:addPoints(anchorBase, if fixedSide = "left" then legOffsets.left else legOffsets.right);
    defaultMovingWorld:addPoints(anchorBase, if movingSide = "left" then legOffsets.left else legOffsets.right);

    fixedWorld:if options.fixedFeetPoint != null then options.fixedFeetPoint else if options.fixedFeetTargetPoint != null then options.fixedFeetTargetPoint else defaultFixedWorld;
    movingStartWorld:if options.movingFeetStartPoint != null then options.movingFeetStartPoint else if options.movingFeetStart != null then options.movingFeetStart else defaultMovingWorld;
    movingTargetWorld:if options.movingFeetTargetPoint != null then options.movingFeetTargetPoint
      else if options.movingFeetTarget != null then options.movingFeetTarget
      else if options.targetFeetPoint != null then options.targetFeetPoint
      else if options.targetFootPoint != null then options.targetFootPoint
      else movingStartWorld;
    arcHeightRaw:options.arcHeight;
    arcHeight:if arcHeightRaw != null then arcHeightRaw else resolveArcHeight(movingStartWorld, movingTargetWorld);
    movingWorld:computeArcPoint(movingStartWorld, movingTargetWorld, progress, arcHeight);

    anchorCandidateA:subtractPoints(fixedWorld, if fixedSide = "left" then legOffsets.left else legOffsets.right);
    anchorCandidateB:subtractPoints(movingWorld, if movingSide = "left" then legOffsets.left else legOffsets.right);
    averagedAnchor:averagePoints(anchorCandidateA, anchorCandidateB);
    anchorPosition:clampAnchorVerticalDrift(if averagedAnchor = null then anchorBase else averagedAnchor, anchorBase);

    updatedLegOffsets:{
      left:subtractPoints(if fixedSide = "left" then fixedWorld else movingWorld, anchorPosition);
      right:subtractPoints(if fixedSide = "right" then fixedWorld else movingWorld, anchorPosition);
    };

    torsoMeasurements:measurementInput.torso ?? {};
    headMeasurements:measurementInput.head ?? {};
    baseHands:measurementInput.hands ?? {};
    torsoDirection:normalizeDirectionValue(
      if torsoMeasurements.direction != null then torsoMeasurements.direction else headMeasurements.direction,
      baseTorsoDirection
    );
    swingOptions:swing.resolveHandSwingOptions(options.handSwing);

    updatedMeasurements:measurementInput + {
      torso:torsoMeasurements + { direction:torsoDirection };
      head:headMeasurements + { direction:torsoDirection };
      legs:{
        left:(baseLegs.left ?? {}) + { effectorCoordinate:updatedLegOffsets.left };
        right:(baseLegs.right ?? {}) + { effectorCoordinate:updatedLegOffsets.right };
      };
    };

    hands:swing.applyHandSwing(baseHands, {
      swing:swingOptions;
      progress:progress;
      torsoDirection:torsoDirection;
      torsoMeasurements:updatedMeasurements.torso;
      movingSide:movingSide;
      legOffsets:updatedLegOffsets;
      defaultHandOffsetsBySide:defaultHandOffsetsBySide;
      defaultHandReach:defaultHandReach;
      defaultShoulderOffsets:defaultShoulderOffsets;
      defaultTorsoWidth:defaultTorsoWidth;
      defaultTorsoHeight:defaultTorsoHeight;
      defaultShoulderExtension:defaultShoulderExtension;
      baseTorsoDirection:baseTorsoDirection;
      normalizeDirectionValue:normalizeDirectionValue;
      distanceBetweenPoints:distanceBetweenPoints;
      clamp01:clamp01;
      clampSymmetric:clampSymmetric;
      scaleVectorToLength:scaleVectorToLength;
    });

    finalMeasurements:updatedMeasurements + { hands:hands };

    staticResult:if includeStatic = false or baseStaticBuilder = null then {} else baseStaticBuilder({
      position:anchorPosition;
      measurements:finalMeasurements;
    }) ?? {};

    sequenceState:{
      position:if anchorPosition = null then defaults.position else anchorPosition;
      measurements:{} + finalMeasurements;
    };

    step:{
      fixedSide:fixedSide;
      movingSide:movingSide;
      fixedPoint:fixedWorld;
      movingPoint:movingWorld;
      anchorPoint:if anchorPosition = null then defaults.position else anchorPosition;
      progress:progress;
    };

    eval (staticResult ?? {}) + {
      position:sequenceState.position;
      measurements:sequenceState.measurements;
      finalPosition:sequenceState.position;
      finalMeasurements:sequenceState.measurements;
      sequenceState:sequenceState;
      step:step;
      progressInput:options.progress;
      progressValue:progress;
      anchorCandidates:{ a:anchorCandidateA; b:anchorCandidateB };
    };
  };

  resolveShoulderOffsets:(torsoMeasurements)=> swing.resolveShoulderOffsets(torsoMeasurements, {
    defaultTorsoWidth:defaultTorsoWidth;
    defaultTorsoHeight:defaultTorsoHeight;
    defaultShoulderExtension:defaultShoulderExtension;
    baseTorsoDirection:baseTorsoDirection;
    normalizeDirectionValue:normalizeDirectionValue;
  });

  eval singleStepProfile;
}

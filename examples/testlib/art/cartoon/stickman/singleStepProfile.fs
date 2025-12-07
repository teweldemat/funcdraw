{
  defaults:{
    position:[0,10];
    leftOffset:[-2,-11];
    rightOffset:[2,-11];
    handForward:3.9;
    handDrop:2.35;
  };

  num:(value, fallback)=> if value = null then fallback else value;

  defaultHandOffsets:{
    left:[-defaults.handForward, defaults.handDrop];
    right:[defaults.handForward, defaults.handDrop];
  };

  baseStaticBuilder:staticMan;
  distanceHelper:if helpers = null then null else helpers.distance ?? null;
  skeletonContext:if skeleton = null or skeleton.build = null then { skeleton:{} } else skeleton.build({});
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
  defaultTorsoWidth:num(baseTorsoMeasurements.width, 6);
  defaultTorsoHeight:num(baseTorsoMeasurements.height, 11);
  defaultShoulderExtension:math.max(num(baseTorsoMeasurements.shoulderExtension, defaultTorsoWidth * 0.15), 0);
  defaultShoulderOffsets:resolveShoulderOffsets({
    width:defaultTorsoWidth;
    height:defaultTorsoHeight;
    shoulderExtension:defaultShoulderExtension;
    direction:baseTorsoDirection;
  });
  defaultHandReach:{
    left:resolveDefaultHandReach("left", defaultShoulderOffsets);
    right:resolveDefaultHandReach("right", defaultShoulderOffsets);
  };

  minReachRatio:0.9;
  maxVerticalAnchorDelta:1.2;

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
    arcHeightRaw:num(options.arcHeight, null);
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
    swing:resolveHandSwingOptions(options.handSwing);

    updatedMeasurements:measurementInput + {
      torso:torsoMeasurements + { direction:torsoDirection };
      head:headMeasurements + { direction:torsoDirection };
      legs:{
        left:(baseLegs.left ?? {}) + { effectorCoordinate:updatedLegOffsets.left };
        right:(baseLegs.right ?? {}) + { effectorCoordinate:updatedLegOffsets.right };
      };
    };

    hands:applyHandSwing(baseHands, {
      swing:swing;
      progress:progress;
      torsoDirection:torsoDirection;
      torsoMeasurements:updatedMeasurements.torso;
      movingSide:movingSide;
      legOffsets:updatedLegOffsets;
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

  computeArcPoint:(start, end, progress, height)=> {
    t:clamp01(progress);
    base:lerpPoint(start, end, t);
    lift:math.sin(math.pi * t) * height;
    eval [base[0], base[1] + lift];
  };

  resolveArcHeight:(start, end)=> {
    span:distanceBetweenPoints(start, end);
    eval math.max(1.5, span * 0.25);
  };

  distanceBetweenPoints:(a, b)=> {
    eval if distanceHelper != null then distanceHelper(a, b) else {
      p1:if a = null then [0,0] else a;
      p2:if b = null then [0,0] else b;
      dx:p2[0] - p1[0];
      dy:p2[1] - p1[1];
      eval math.sqrt(dx * dx + dy * dy);
    };
  };

  normalizeSide:(value, fallback)=> {
    defaultSide:fallback ?? "left";
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "left" then "left" else if textValue = "right" then "right" else defaultSide;
  };

  normalizeDirectionValue:(value, fallback)=> {
    defaultDir:fallback ?? "front";
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "left" then "left"
    else if textValue = "right" then "right"
    else if textValue = "front" then "front"
    else if textValue = "back" then "back"
    else defaultDir;
  };

  lerpPoint:(start, end, t)=> [
    start[0] + (end[0] - start[0]) * t,
    start[1] + (end[1] - start[1]) * t
  ];

  clamp01:(value)=> {
    v:num(value, 0);
    eval if v < 0 then 0 else if v > 1 then 1 else v;
  };

  addPoints:(a, b)=> {
    pa:if a = null then [0,0] else a;
    pb:if b = null then [0,0] else b;
    eval [pa[0] + pb[0], pa[1] + pb[1]];
  };

  subtractPoints:(a, b)=> {
    pa:if a = null then [0,0] else a;
    pb:if b = null then [0,0] else b;
    eval [pa[0] - pb[0], pa[1] - pb[1]];
  };

  averagePoints:(a, b)=> {
    hasA:a != null;
    hasB:b != null;
    count:(if hasA then 1 else 0) + (if hasB then 1 else 0);
    sumX:(if hasA then a[0] else 0) + (if hasB then b[0] else 0);
    sumY:(if hasA then a[1] else 0) + (if hasB then b[1] else 0);
    eval if count = 0 then null else [sumX / count, sumY / count];
  };

  clampAnchorVerticalDrift:(candidate, baseline)=> {
    reference:if baseline = null then defaults.position else baseline;
    safeCandidate:if candidate = null then reference else candidate;
    minY:reference[1] - maxVerticalAnchorDelta;
    maxY:reference[1] + maxVerticalAnchorDelta;
    clampedY:helpers.clamp(safeCandidate[1], minY, maxY);
    eval [safeCandidate[0], clampedY];
  };

  resolveHandSwingOptions:(value)=> {
    options:value ?? {};
    enabled:if options.enabled = false then false else true;
    amplitude:math.max(0, num(options.amplitude, 1.4));
    lift:math.max(0, num(options.lift, 0.35));
    forwardOffset:num(options.forwardOffset, 0);
    phase:num(options.phase, 0);
    mode:normalizeSwingMode(options.mode);
    eval { enabled:enabled; amplitude:amplitude; lift:lift; forwardOffset:forwardOffset; phase:phase; mode:mode };
  };

  normalizeSwingMode:(value)=> {
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "mirror" then "mirror" else if textValue = "sine" then "sine" else "mirror";
  };

  resolveShoulderOffsetsFromContext:(context)=> {
    torso:context.torsoMeasurements ?? {};
    width:num(torso.width, defaultTorsoWidth);
    height:num(torso.height, defaultTorsoHeight);
    shoulderExtension:math.max(
      num(torso.shoulderExtension, if defaultShoulderExtension != null then defaultShoulderExtension else width * 0.15),
      0
    );
    direction:normalizeDirectionValue(torso.direction, context.torsoDirection);
    resolved:resolveShoulderOffsets({ width:width; height:height; shoulderExtension:shoulderExtension; direction:direction });
    eval {
      left:if resolved.left != null then resolved.left else defaultShoulderOffsets.left ?? [0,0];
      right:if resolved.right != null then resolved.right else defaultShoulderOffsets.right ?? [0,0];
    };
  };

  resolveShoulderOffsets:(torsoMeasurements)=> {
    width:num(torsoMeasurements.width, defaultTorsoWidth);
    height:num(torsoMeasurements.height, defaultTorsoHeight);
    shoulderExtension:math.max(num(torsoMeasurements.shoulderExtension, width * 0.15), 0);
    direction:normalizeDirectionValue(torsoMeasurements.direction, baseTorsoDirection);
    halfWidth:width / 2;
    handsY:height * 0.85;
    handOffset:halfWidth + shoulderExtension;
    eval if direction = "left" or direction = "right" then {
      center:[0, handsY];
      left:center;
      right:center;
    } else {
      left:[-handOffset, handsY];
      right:[handOffset, handsY];
    };
  };

  resolveDefaultHandReach:(side, shoulderOffsets)=> {
    baseHands:baseSkeleton.hands;
    sideHands:if side = "left" then baseHands?.left else baseHands?.right;
    attachment:if sideHands = null then null else sideHands.attachmentPoint;
    target:if sideHands = null then null else sideHands.targetPoint;
    lengths:if sideHands = null or sideHands.lengths = null then {} else sideHands.lengths;
    upper:num(lengths.upper, null);
    lower:num(lengths.lower, null);
    eval if attachment != null and target != null then distanceBetweenPoints(attachment, target)
    else if upper != null and lower != null then math.max(upper + lower, 0)
    else {
      shoulder:if side = "left" then shoulderOffsets.left else shoulderOffsets.right;
      defaultEffector:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      eval if shoulder != null and defaultEffector != null then {
        reach:distanceBetweenPoints(shoulder, defaultEffector);
        eval if reach > 0 then reach else 7;
      } else 7;
    };
  };

  resolveHandReachLength:(side, shoulderOffsets, handMeasurements)=> {
    baseReach:if side = "left" then defaultHandReach.left else defaultHandReach.right;
    sideMeasurements:if handMeasurements = null or handMeasurements[side] = null then {} else handMeasurements[side];
    upper:num(sideMeasurements.upperLength, null);
    lower:num(sideMeasurements.lowerLength, null);
    eval if upper != null and lower != null then {
      measured:math.max(upper + lower, 0);
      eval if measured > 0 then measured else baseReach;
    } else {
      shoulder:if side = "left" then shoulderOffsets.left else shoulderOffsets.right;
      defaultEffector:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      eval if shoulder != null and defaultEffector != null then {
        reach:distanceBetweenPoints(shoulder, defaultEffector);
        eval if reach > 0 then math.max(baseReach, reach) else baseReach;
      } else math.max(baseReach, 1);
    };
  };

  applyHandSwing:(handMeasurements, context)=> {
    swing:context.swing;
    eval if swing.enabled = false then handMeasurements else {
      baseHands:handMeasurements ?? {};
      shoulderOffsets:resolveShoulderOffsetsFromContext(context);
      reachBySide:{
        left:resolveHandReachLength("left", shoulderOffsets, baseHands);
        right:resolveHandReachLength("right", shoulderOffsets, baseHands);
      };
      swingContext:context + { shoulderOffsets:shoulderOffsets; reachBySide:reachBySide };
      eval if swing.mode = "sine" then applySineHandSwing(baseHands, swingContext) else applyMirrorHandSwing(baseHands, swingContext);
    };
  };

  applySineHandSwing:(baseHands, context)=> {
    swing:context.swing;
    angle:math.pi * clamp01(context.progress) + swing.phase;
    swingSignal:math.cos(angle);
    liftSignal:math.sin(angle);
    forwardSign:resolveForwardSign(context.torsoDirection);
    amplitude:swing.amplitude * forwardSign;
    liftAmount:swing.lift;
    forwardOffset:swing.forwardOffset * forwardSign;
    shoulderOffsets:context.shoulderOffsets;
    reachBySide:context.reachBySide;

    applySide:(side)=> {
      fallback:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      baseEffector:if baseHands[side]?.effectorCoordinate = null then fallback else baseHands[side].effectorCoordinate;
      shoulderOffset:if shoulderOffsets = null then fallback else if side = "left" then shoulderOffsets.left ?? fallback else shoulderOffsets.right ?? fallback;
      reachValue:if reachBySide = null then null else if side = "left" then reachBySide.left else reachBySide.right;
      defaultReach:if side = "left" then defaultHandReach.left else defaultHandReach.right;
      reach:if reachValue != null then reachValue else if defaultReach != null then defaultReach else distanceBetweenPoints(baseEffector, shoulderOffset);
      isMoving:side = context.movingSide;
      horizontalSwing:(if isMoving then swingSignal else -swingSignal) * amplitude + forwardOffset;
      verticalSwing:(if isMoving then liftSignal else -liftSignal) * liftAmount;
      candidate:[horizontalSwing, baseEffector[1] + verticalSwing];
      targetEffector:scaleVectorToLength(candidate, shoulderOffset, reach);
      eval (baseHands[side] ?? {}) + { effectorCoordinate:targetEffector };
    };

    eval {
      left:applySide("left");
      right:applySide("right");
    };
  };

  applyMirrorHandSwing:(baseHands, context)=> {
    swing:context.swing;
    legOffsets:context.legOffsets;
    forwardSign:resolveForwardSign(context.torsoDirection);
    depthScale:resolveLegDepthScale(legOffsets);
    averageY:resolveAverageY(legOffsets);
    phaseSignal:math.cos(math.pi * clamp01(context.progress) + swing.phase);
    phaseWeight:0.9;
    legWeight:0.45;
    verticalPhaseWeight:0.3;
    verticalLegWeight:0.05;
    shoulderOffsets:context.shoulderOffsets;
    reachBySide:context.reachBySide;

    applySide:(side)=> {
      fallback:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      baseEffector:if baseHands[side]?.effectorCoordinate = null then fallback else baseHands[side].effectorCoordinate;
      mirroredLeg:if side = "left" then legOffsets?.right else legOffsets?.left;
      shoulderOffset:if side = "left" then shoulderOffsets.left else shoulderOffsets.right;
      targetLength:if reachBySide = null then null else if side = "left" then reachBySide.left else reachBySide.right;

      eval if mirroredLeg = null then (baseHands[side] ?? {}) + { effectorCoordinate:baseEffector } else {
        radius:math.max(0.000001, distanceBetweenPoints([0,0], baseEffector));
        normalizedHorizontal:clampSymmetric(mirroredLeg[0] / depthScale, 1);
        normalizedVertical:clampSymmetric((mirroredLeg[1] - averageY) / depthScale, 1);
        signedPhase:if side = context.movingSide then -phaseSignal else phaseSignal;
        horizontalInfluence:signedPhase * (phaseWeight + math.abs(normalizedHorizontal) * legWeight);
        verticalInfluence:signedPhase * verticalPhaseWeight + normalizedVertical * verticalLegWeight;
        horizontalSwing:horizontalInfluence * swing.amplitude * forwardSign + swing.forwardOffset * forwardSign;
        verticalSwing:verticalInfluence * swing.lift;
        candidateX:baseEffector[0] + horizontalSwing;
        candidateY:baseEffector[1] + verticalSwing;
        targetRadius:if targetLength = null then radius else targetLength;
        targetEffector:scaleVectorToLength([candidateX, candidateY], shoulderOffset, targetRadius);
        eval (baseHands[side] ?? {}) + { effectorCoordinate:targetEffector };
      };
    };

    eval {
      left:applySide("left");
      right:applySide("right");
    };
  };

  resolveLegDepthScale:(legOffsets)=> {
    left:if legOffsets = null then null else legOffsets.left;
    right:if legOffsets = null then null else legOffsets.right;
    leftDepth:if left = null then 0 else math.abs(left[1]);
    rightDepth:if right = null then 0 else math.abs(right[1]);
    eval math.max(1, leftDepth, rightDepth);
  };

  resolveAverageY:(legOffsets)=> {
    left:if legOffsets = null then null else legOffsets.left;
    right:if legOffsets = null then null else legOffsets.right;
    sum:(if left = null then 0 else left[1]) + (if right = null then 0 else right[1]);
    count:(if left = null then 0 else 1) + (if right = null then 0 else 1);
    eval if count = 0 then 0 else sum / count;
  };

  clampSymmetric:(value, limit)=> {
    maxValue:num(limit, 1);
    v:num(value, null);
    eval if v = null then 0 else if v > maxValue then maxValue else if v < -maxValue then -maxValue else v;
  };

  resolveForwardSign:(direction)=> if direction = "left" then -1 else 1;

  scaleVectorToLength:(point, origin, length)=> {
    ox:num(if origin = null then null else origin[0], 0);
    oy:num(if origin = null then null else origin[1], 0);
    dx:num(if point = null then null else point[0], 0) - ox;
    dy:num(if point = null then null else point[1], 0) - oy;
    distance:math.sqrt(dx * dx + dy * dy);
    target:math.max(num(length, 0), 0);
    eval if distance < 0.000001 then [ox, oy - target] else {
      scale:if distance = 0 then 0 else target / distance;
      eval [ox + dx * scale, oy + dy * scale];
    };
  };

  eval singleStepProfile;
}

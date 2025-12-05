{
  defaults:{
    position:[0,10];
    leftOffset:[-2,-11];
    rightOffset:[2,-11];
    handForward:3.9;
    handDrop:2.35;
  };

  defaultHandOffsets:{
    left:[-defaults.handForward, defaults.handDrop];
    right:[defaults.handForward, defaults.handDrop];
  };

  baseStaticBuilder:staticMan;
  distanceHelper:if helpers = null then null else helpers.distance ?? null;
  skeletonContext:if skeleton = null or skeleton.build = null then { skeleton:{} } else skeleton.build({});
  baseSkeleton:helpers.normalizeInput(skeletonContext.skeleton, {});
  skeletonPosition:if isPoint(baseSkeleton.position) then baseSkeleton.position else defaults.position;

  defaultLeftFoot:if isPoint(baseSkeleton.legs?.left?.targetPoint) then baseSkeleton.legs.left.targetPoint else addPoints(skeletonPosition, defaults.leftOffset);
  defaultRightFoot:if isPoint(baseSkeleton.legs?.right?.targetPoint) then baseSkeleton.legs.right.targetPoint else addPoints(skeletonPosition, defaults.rightOffset);
  defaultOffsets:{
    left:subtractPoints(defaultLeftFoot, skeletonPosition);
    right:subtractPoints(defaultRightFoot, skeletonPosition);
  };

  defaultLeftHandPoint:if isPoint(baseSkeleton.hands?.left?.targetPoint) then baseSkeleton.hands.left.targetPoint else addPoints(skeletonPosition, defaultHandOffsets.left);
  defaultRightHandPoint:if isPoint(baseSkeleton.hands?.right?.targetPoint) then baseSkeleton.hands.right.targetPoint else addPoints(skeletonPosition, defaultHandOffsets.right);
  defaultHandOffsetsBySide:{
    left:subtractPoints(defaultLeftHandPoint, skeletonPosition);
    right:subtractPoints(defaultRightHandPoint, skeletonPosition);
  };

  baseTorsoDirection:normalizeDirectionValue(baseSkeleton.torso?.direction, "front");
  baseTorsoMeasurements:helpers.normalizeInput(baseSkeleton.torso, {});
  defaultTorsoWidth:helpers.resolveNumber(baseTorsoMeasurements.width, 6);
  defaultTorsoHeight:helpers.resolveNumber(baseTorsoMeasurements.height, 11);
  defaultShoulderExtension:math.max(helpers.resolveNumber(baseTorsoMeasurements.shoulderExtension, defaultTorsoWidth * 0.15), 0);
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

  steperManProfile:(optionsInput)=> {
    options:helpers.normalizeInput(optionsInput ?? {}, {});
    anchorBase:helpers.normalizePoint(options.position, defaults.position);
    measurementInput:helpers.normalizeInput(options.measurements, {});
    progress:clamp01(options.progress);
    movingSideFromOptions:normalizeSide(options.movingSide ?? options.movingFeet, null);
    fixedSideFromOptions:normalizeSide(options.fixedFeet, null);
    movingSide:if movingSideFromOptions != null then movingSideFromOptions else if fixedSideFromOptions = "left" then "right" else if fixedSideFromOptions = "right" then "left" else "left";
    fixedSide:if movingSide = "left" then "right" else "left";

    baseLegs:helpers.normalizeInput(measurementInput.legs, {});
    legOffsets:{
      left:helpers.normalizePoint(baseLegs.left?.effectorCoordinate, defaultOffsets.left);
      right:helpers.normalizePoint(baseLegs.right?.effectorCoordinate, defaultOffsets.right);
    };

    defaultFixedWorld:addPoints(anchorBase, if fixedSide = "left" then legOffsets.left else legOffsets.right);
    defaultMovingWorld:addPoints(anchorBase, if movingSide = "left" then legOffsets.left else legOffsets.right);

    fixedWorld:helpers.normalizePoint(options.fixedFeetPoint ?? options.fixedFeetTargetPoint, defaultFixedWorld);
    movingStartWorld:helpers.normalizePoint(options.movingFeetStartPoint ?? options.movingFeetStart, defaultMovingWorld);
    movingTargetWorld:helpers.normalizePoint(
      if options.movingFeetTargetPoint != null then options.movingFeetTargetPoint
      else if options.movingFeetTarget != null then options.movingFeetTarget
      else if options.targetFeetPoint != null then options.targetFeetPoint
      else options.targetFootPoint,
      movingStartWorld
    );
    arcHeightRaw:helpers.resolveNumber(options.arcHeight, null);
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

    torsoMeasurements:helpers.normalizeInput(measurementInput.torso, {});
    headMeasurements:helpers.normalizeInput(measurementInput.head, {});
    baseHands:helpers.normalizeInput(measurementInput.hands, {});
    torsoDirection:normalizeDirectionValue(
      if torsoMeasurements.direction != null then torsoMeasurements.direction else headMeasurements.direction,
      baseTorsoDirection
    );
    swing:resolveHandSwingOptions(options.handSwing);

    updatedMeasurements:helpers.mergeDeep(measurementInput, {
      torso:helpers.mergeDeep(torsoMeasurements, { direction:torsoDirection });
      head:helpers.mergeDeep(headMeasurements, { direction:torsoDirection });
      legs:{
        left:helpers.mergeDeep(helpers.normalizeInput(baseLegs.left, {}), { effectorCoordinate:updatedLegOffsets.left });
        right:helpers.mergeDeep(helpers.normalizeInput(baseLegs.right, {}), { effectorCoordinate:updatedLegOffsets.right });
      };
    });

    hands:applyHandSwing(baseHands, {
      swing:swing;
      progress:progress;
      torsoDirection:torsoDirection;
      torsoMeasurements:updatedMeasurements.torso;
      movingSide:movingSide;
      legOffsets:updatedLegOffsets;
    });

    finalMeasurements:helpers.mergeDeep(updatedMeasurements, { hands:hands });

    staticResult:if baseStaticBuilder = null then {} else baseStaticBuilder({
      position:anchorPosition;
      measurements:finalMeasurements;
    }) ?? {};

    sequenceState:{
      position:helpers.normalizePoint(anchorPosition, defaults.position);
      measurements:helpers.mergeDeep({}, finalMeasurements);
    };

    step:{
      fixedSide:fixedSide;
      movingSide:movingSide;
      fixedPoint:helpers.normalizePoint(fixedWorld, [0,0]);
      movingPoint:helpers.normalizePoint(movingWorld, [0,0]);
      anchorPoint:helpers.normalizePoint(anchorPosition, defaults.position);
      progress:progress;
    };

    eval helpers.mergeDeep(helpers.normalizeInput(staticResult, {}), {
      position:sequenceState.position;
      measurements:sequenceState.measurements;
      finalPosition:sequenceState.position;
      finalMeasurements:sequenceState.measurements;
      sequenceState:sequenceState;
      step:step;
    });
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
      p1:helpers.normalizePoint(a, [0,0]);
      p2:helpers.normalizePoint(b, [0,0]);
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
    num:helpers.resolveNumber(value, 0);
    eval if num < 0 then 0 else if num > 1 then 1 else num;
  };

  isPoint:(value)=> {
    x:helpers.resolveNumber(if value = null then null else value[0], null);
    y:helpers.resolveNumber(if value = null then null else value[1], null);
    eval value != null and x != null and y != null;
  };

  addPoints:(a, b)=> {
    pa:helpers.normalizePoint(a, [0,0]);
    pb:helpers.normalizePoint(b, [0,0]);
    eval [pa[0] + pb[0], pa[1] + pb[1]];
  };

  subtractPoints:(a, b)=> {
    pa:helpers.normalizePoint(a, [0,0]);
    pb:helpers.normalizePoint(b, [0,0]);
    eval [pa[0] - pb[0], pa[1] - pb[1]];
  };

  averagePoints:(a, b)=> {
    hasA:isPoint(a);
    hasB:isPoint(b);
    count:(if hasA then 1 else 0) + (if hasB then 1 else 0);
    sumX:(if hasA then a[0] else 0) + (if hasB then b[0] else 0);
    sumY:(if hasA then a[1] else 0) + (if hasB then b[1] else 0);
    eval if count = 0 then null else [sumX / count, sumY / count];
  };

  clampAnchorVerticalDrift:(candidate, baseline)=> {
    reference:if isPoint(baseline) then baseline else defaults.position;
    safeCandidate:if isPoint(candidate) then candidate else reference;
    minY:reference[1] - maxVerticalAnchorDelta;
    maxY:reference[1] + maxVerticalAnchorDelta;
    clampedY:helpers.clamp(safeCandidate[1], minY, maxY);
    eval [safeCandidate[0], clampedY];
  };

  resolveHandSwingOptions:(value)=> {
    options:helpers.normalizeInput(value ?? {}, {});
    enabled:if options.enabled = false then false else true;
    amplitude:math.max(0, helpers.resolveNumber(options.amplitude, 1.4));
    lift:math.max(0, helpers.resolveNumber(options.lift, 0.35));
    forwardOffset:helpers.resolveNumber(options.forwardOffset, 0);
    phase:helpers.resolveNumber(options.phase, 0);
    mode:normalizeSwingMode(options.mode);
    eval { enabled:enabled; amplitude:amplitude; lift:lift; forwardOffset:forwardOffset; phase:phase; mode:mode };
  };

  normalizeSwingMode:(value)=> {
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "mirror" then "mirror" else if textValue = "sine" then "sine" else "mirror";
  };

  resolveShoulderOffsetsFromContext:(context)=> {
    torso:helpers.normalizeInput(context.torsoMeasurements, {});
    width:helpers.resolveNumber(torso.width, defaultTorsoWidth);
    height:helpers.resolveNumber(torso.height, defaultTorsoHeight);
    shoulderExtension:math.max(
      helpers.resolveNumber(torso.shoulderExtension, if defaultShoulderExtension != null then defaultShoulderExtension else width * 0.15),
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
    width:helpers.resolveNumber(torsoMeasurements.width, defaultTorsoWidth);
    height:helpers.resolveNumber(torsoMeasurements.height, defaultTorsoHeight);
    shoulderExtension:math.max(helpers.resolveNumber(torsoMeasurements.shoulderExtension, width * 0.15), 0);
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
    lengths:helpers.normalizeInput(if sideHands = null then null else sideHands.lengths, {});
    upper:helpers.resolveNumber(lengths.upper, null);
    lower:helpers.resolveNumber(lengths.lower, null);
    eval if isPoint(attachment) and isPoint(target) then distanceBetweenPoints(attachment, target)
    else if upper != null and lower != null then math.max(upper + lower, 0)
    else {
      shoulder:if side = "left" then shoulderOffsets.left else shoulderOffsets.right;
      defaultEffector:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      eval if isPoint(shoulder) and isPoint(defaultEffector) then {
        reach:distanceBetweenPoints(shoulder, defaultEffector);
        eval if reach > 0 then reach else 7;
      } else 7;
    };
  };

  resolveHandReachLength:(side, shoulderOffsets, handMeasurements)=> {
    baseReach:if side = "left" then defaultHandReach.left else defaultHandReach.right;
    sideMeasurements:helpers.normalizeInput(if handMeasurements = null then null else handMeasurements[side], {});
    upper:helpers.resolveNumber(sideMeasurements.upperLength, null);
    lower:helpers.resolveNumber(sideMeasurements.lowerLength, null);
    eval if upper != null and lower != null then {
      measured:math.max(upper + lower, 0);
      eval if measured > 0 then measured else baseReach;
    } else {
      shoulder:if side = "left" then shoulderOffsets.left else shoulderOffsets.right;
      defaultEffector:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      eval if isPoint(shoulder) and isPoint(defaultEffector) then {
        reach:distanceBetweenPoints(shoulder, defaultEffector);
        eval if reach > 0 then math.max(baseReach, reach) else baseReach;
      } else math.max(baseReach, 1);
    };
  };

  applyHandSwing:(handMeasurements, context)=> {
    swing:context.swing;
    eval if swing.enabled = false then handMeasurements else {
      baseHands:helpers.normalizeInput(handMeasurements ?? {}, {});
      shoulderOffsets:resolveShoulderOffsetsFromContext(context);
      reachBySide:{
        left:resolveHandReachLength("left", shoulderOffsets, baseHands);
        right:resolveHandReachLength("right", shoulderOffsets, baseHands);
      };
      swingContext:helpers.mergeDeep(context, { shoulderOffsets:shoulderOffsets; reachBySide:reachBySide });
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
      baseEffector:helpers.normalizePoint(baseHands[side]?.effectorCoordinate, fallback);
      shoulderOffset:if shoulderOffsets = null then fallback else if side = "left" then shoulderOffsets.left ?? fallback else shoulderOffsets.right ?? fallback;
      reachValue:if reachBySide = null then null else if side = "left" then reachBySide.left else reachBySide.right;
      defaultReach:if side = "left" then defaultHandReach.left else defaultHandReach.right;
      reach:if reachValue != null then reachValue else if defaultReach != null then defaultReach else distanceBetweenPoints(baseEffector, shoulderOffset);
      isMoving:side = context.movingSide;
      horizontalSwing:(if isMoving then swingSignal else -swingSignal) * amplitude + forwardOffset;
      verticalSwing:(if isMoving then liftSignal else -liftSignal) * liftAmount;
      candidate:[horizontalSwing, baseEffector[1] + verticalSwing];
      targetEffector:scaleVectorToLength(candidate, shoulderOffset, reach);
      eval helpers.mergeDeep(helpers.normalizeInput(baseHands[side], {}), { effectorCoordinate:targetEffector });
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

    applySide:(side)=> {
      fallback:if side = "left" then defaultHandOffsetsBySide.left else defaultHandOffsetsBySide.right;
      baseEffector:helpers.normalizePoint(baseHands[side]?.effectorCoordinate, fallback);
      mirroredLeg:if side = "left" then legOffsets?.right else legOffsets?.left;

      eval if !isPoint(mirroredLeg) then helpers.mergeDeep(helpers.normalizeInput(baseHands[side], {}), { effectorCoordinate:baseEffector }) else {
        radius:math.max(0.000001, distanceBetweenPoints([0,0], baseEffector));
        normalizedHorizontal:clampSymmetric(mirroredLeg[0] / depthScale, 1);
        normalizedVertical:clampSymmetric((mirroredLeg[1] - averageY) / depthScale, 1);
        horizontalSwing:normalizedHorizontal * swing.amplitude * forwardSign + swing.forwardOffset * forwardSign;
        verticalSwing:normalizedVertical * swing.lift;
        candidateX:baseEffector[0] + horizontalSwing;
        candidateY:baseEffector[1] + verticalSwing;
        candidateLen:math.sqrt(candidateX * candidateX + candidateY * candidateY);
        scale:radius / candidateLen;
        eval helpers.mergeDeep(helpers.normalizeInput(baseHands[side], {}), { effectorCoordinate:[candidateX * scale, candidateY * scale] });
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
    leftDepth:if isPoint(left) then math.abs(left[1]) else 0;
    rightDepth:if isPoint(right) then math.abs(right[1]) else 0;
    eval math.max(1, leftDepth, rightDepth);
  };

  resolveAverageY:(legOffsets)=> {
    left:if legOffsets = null then null else legOffsets.left;
    right:if legOffsets = null then null else legOffsets.right;
    sum:(if isPoint(left) then left[1] else 0) + (if isPoint(right) then right[1] else 0);
    count:(if isPoint(left) then 1 else 0) + (if isPoint(right) then 1 else 0);
    eval if count = 0 then 0 else sum / count;
  };

  clampSymmetric:(value, limit)=> {
    maxValue:helpers.resolveNumber(limit, 1);
    num:helpers.resolveNumber(value, null);
    eval if num = null then 0 else if num > maxValue then maxValue else if num < -maxValue then -maxValue else num;
  };

  resolveForwardSign:(direction)=> if direction = "left" then -1 else 1;

  scaleVectorToLength:(point, origin, length)=> {
    ox:helpers.resolveNumber(if origin = null then null else origin[0], 0);
    oy:helpers.resolveNumber(if origin = null then null else origin[1], 0);
    dx:helpers.resolveNumber(if point = null then null else point[0], 0) - ox;
    dy:helpers.resolveNumber(if point = null then null else point[1], 0) - oy;
    distance:math.sqrt(dx * dx + dy * dy);
    target:math.max(helpers.resolveNumber(length, 0), 0);
    eval if distance < 0.000001 then [ox, oy - target] else {
      scale:if distance = 0 then 0 else target / distance;
      eval [ox + dx * scale, oy + dy * scale];
    };
  };

  eval steperManProfile;
}

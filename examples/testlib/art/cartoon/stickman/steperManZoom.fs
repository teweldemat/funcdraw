{
  defaults:{
    position:[0, 10];
    leftLegOffset:[-2, -11];
    rightLegOffset:[2, -11];
    leftHandOffset:[-3.9, 2.35];
    rightHandOffset:[3.9, 2.35];
    torso:{ width:6; height:11 };
    headHeight:4.5;
    handLengths:{ upper:4; lower:3 };
    legLengths:{ upper:5.2; lower:4.8 };
  };

  baseStaticBuilder:staticMan;
  skeletonContext:if skeleton = null or skeleton.build = null then { skeleton:{} } else skeleton.build({});
  baseSkeleton:helpers.normalizeInput(skeletonContext.skeleton, {});
  skeletonPosition:if isPoint(baseSkeleton.position) then baseSkeleton.position else defaults.position;

  defaultLeftFoot:if isPoint(baseSkeleton.legs?.left?.targetPoint) then baseSkeleton.legs.left.targetPoint else addPoints(skeletonPosition, defaults.leftLegOffset);
  defaultRightFoot:if isPoint(baseSkeleton.legs?.right?.targetPoint) then baseSkeleton.legs.right.targetPoint else addPoints(skeletonPosition, defaults.rightLegOffset);
  defaultOffsets:{
    left:subtractPoints(defaultLeftFoot, skeletonPosition);
    right:subtractPoints(defaultRightFoot, skeletonPosition);
  };

  defaultLeftHandPoint:if isPoint(baseSkeleton.hands?.left?.targetPoint) then baseSkeleton.hands.left.targetPoint else addPoints(skeletonPosition, defaults.leftHandOffset);
  defaultRightHandPoint:if isPoint(baseSkeleton.hands?.right?.targetPoint) then baseSkeleton.hands.right.targetPoint else addPoints(skeletonPosition, defaults.rightHandOffset);
  defaultHandOffsets:{
    left:subtractPoints(defaultLeftHandPoint, skeletonPosition);
    right:subtractPoints(defaultRightHandPoint, skeletonPosition);
  };

  defaultTorsoMeasurements:helpers.normalizeInput(baseSkeleton.torso, {});
  defaultTorsoWidth:helpers.resolveNumber(defaultTorsoMeasurements.width, defaults.torso.width);
  defaultTorsoHeight:helpers.resolveNumber(defaultTorsoMeasurements.height, defaults.torso.height);
  defaultShoulderExtension:math.max(helpers.resolveNumber(defaultTorsoMeasurements.shoulderExtension, defaultTorsoWidth * 0.15), 0);

  defaultHandLengths:{
    left:{
      upper:helpers.resolveNumber(baseSkeleton.hands?.left?.lengths?.upper, defaults.handLengths.upper);
      lower:helpers.resolveNumber(baseSkeleton.hands?.left?.lengths?.lower, defaults.handLengths.lower);
    };
    right:{
      upper:helpers.resolveNumber(baseSkeleton.hands?.right?.lengths?.upper, defaults.handLengths.upper);
      lower:helpers.resolveNumber(baseSkeleton.hands?.right?.lengths?.lower, defaults.handLengths.lower);
    };
  };

  defaultLegLengths:{
    left:{
      upper:helpers.resolveNumber(baseSkeleton.legs?.left?.lengths?.upper, defaults.legLengths.upper);
      lower:helpers.resolveNumber(baseSkeleton.legs?.left?.lengths?.lower, defaults.legLengths.lower);
    };
    right:{
      upper:helpers.resolveNumber(baseSkeleton.legs?.right?.lengths?.upper, defaults.legLengths.upper);
      lower:helpers.resolveNumber(baseSkeleton.legs?.right?.lengths?.lower, defaults.legLengths.lower);
    };
  };

  steperManZoom:(optionsInput)=> {
    options:helpers.normalizeInput(optionsInput ?? {}, {});
    measurementsInput:helpers.normalizeInput(options.measurements ?? options.initialMeasurements, {});
    movingSide:normalizeSide(options.movingSide ?? options.movingFeet ?? options.movingFoot, "left");
    fixedSide:if movingSide = "left" then "right" else "left";
    progress:clamp01(helpers.resolveNumber(options.progress, 0));
    zoomTarget:math.max(0, helpers.resolveNumber(if options.zoom != null then options.zoom else options.zoomFactor, 1));
    zoomProgress:clamp01(helpers.resolveNumber(options.zoomProgress, 1));
    bodyScale:lerp(1, zoomTarget, zoomProgress);
    torsoBase:helpers.normalizeInput(measurementsInput.torso, {});
    headBase:helpers.normalizeInput(measurementsInput.head, {});
    anchorBase:helpers.normalizePoint(options.position, skeletonPosition);

    legOffsets:{
      left:readEffectorOffset(measurementsInput.legs?.left, defaultOffsets.left);
      right:readEffectorOffset(measurementsInput.legs?.right, defaultOffsets.right);
    };
    handOffsets:{
      left:readEffectorOffset(measurementsInput.hands?.left, defaultHandOffsets.left);
      right:readEffectorOffset(measurementsInput.hands?.right, defaultHandOffsets.right);
    };

    fixedWorldY:anchorBase[1] + legOffsets[fixedSide][1];
    movingStartWorldY:anchorBase[1] + legOffsets[movingSide][1];
    movingTargetWorldY:helpers.resolveNumber(
      if options.movingFootTargetY != null then options.movingFootTargetY
      else if options.movingFeetTargetY != null then options.movingFeetTargetY
      else if options.targetY != null then options.targetY
      else movingStartWorldY,
      movingStartWorldY
    );
    movingWorldY:lerp(movingStartWorldY, movingTargetWorldY, progress);
    deltaY:movingWorldY - movingStartWorldY;

    baseDirection:normalizeDirection(
      if torsoBase.direction != null then torsoBase.direction
      else if headBase.direction != null then headBase.direction
      else baseSkeleton.torso?.direction,
      "front"
    );
    direction:if deltaY > 0 then "back" else if deltaY < 0 then "front" else baseDirection;

    averageStartY:(fixedWorldY + movingStartWorldY) * 0.5;
    initialDistanceRaw:anchorBase[1] - averageStartY;
    initialDistance:if math.abs(initialDistanceRaw) > 1.0e-6 then initialDistanceRaw else defaultTorsoHeight;
    distanceSign:if initialDistance >= 0 then 1 else -1;
    averageCurrentY:(fixedWorldY + movingWorldY) * 0.5;
    baseDistance:math.max(math.abs(initialDistance), defaultTorsoHeight);
    desiredDistance:baseDistance * bodyScale * distanceSign;
    anchorPoint:[anchorBase[0], averageCurrentY + desiredDistance];

    legLengths:{
      left:readLimbLengths(measurementsInput.legs?.left, defaultLegLengths.left);
      right:readLimbLengths(measurementsInput.legs?.right, defaultLegLengths.right);
    };
    handLengths:{
      left:readLimbLengths(measurementsInput.hands?.left, defaultHandLengths.left);
      right:readLimbLengths(measurementsInput.hands?.right, defaultHandLengths.right);
    };

    torsoDimensions:resolveTorsoDimensions(torsoBase, bodyScale, direction);
    attachments:resolveAttachments(torsoDimensions);
    straighten:clamp01(math.abs(zoomTarget - 1) * zoomProgress);

    hipOffsets:{
      left:helpers.normalizePoint(attachments.legs?.left, [0,0]);
      right:helpers.normalizePoint(attachments.legs?.right, [0,0]);
    };

    legEffectors:{
      left:[hipOffsets.left[0], (if fixedSide = "left" then fixedWorldY else movingWorldY) - anchorPoint[1]];
      right:[hipOffsets.right[0], (if fixedSide = "right" then fixedWorldY else movingWorldY) - anchorPoint[1]];
    };

    handEffectors:{
      left:[lerp(handOffsets.left[0] * bodyScale, 0, straighten), handOffsets.left[1] * bodyScale];
      right:[lerp(handOffsets.right[0] * bodyScale, 0, straighten), handOffsets.right[1] * bodyScale];
    };

    legLengthsScaled:{
      left:scaleLengths(legLengths.left, bodyScale);
      right:scaleLengths(legLengths.right, bodyScale);
    };
    handLengthsScaled:{
      left:scaleLengths(handLengths.left, bodyScale);
      right:scaleLengths(handLengths.right, bodyScale);
    };

    resolvedLegs:{
      left:resolveStraightLimb(legEffectors.left, legLengthsScaled.left, true, hipOffsets.left);
      right:resolveStraightLimb(legEffectors.right, legLengthsScaled.right, true, hipOffsets.right);
    };

    handBend:{
      left:toBoolean(measurementsInput.hands?.left?.positiveBend, false);
      right:toBoolean(measurementsInput.hands?.right?.positiveBend, true);
    };
    resolvedHands:{
      left:resolveStraightLimb(handEffectors.left, handLengthsScaled.left, handBend.left, attachments.hands?.left);
      right:resolveStraightLimb(handEffectors.right, handLengthsScaled.right, handBend.right, attachments.hands?.right);
    };

    legsBase:helpers.normalizeInput(measurementsInput.legs, {});
    handsBase:helpers.normalizeInput(measurementsInput.hands, {});
    leftFootBase:helpers.normalizeInput(legsBase.left?.foot, {});
    rightFootBase:helpers.normalizeInput(legsBase.right?.foot, {});
    leftFootLength:scaleOptionalLength(leftFootBase.length, bodyScale);
    rightFootLength:scaleOptionalLength(rightFootBase.length, bodyScale);

    updatedMeasurements:{
      torso:torsoBase + torsoDimensions;
      head:headBase + {
        verticalExtent:helpers.resolveNumber(headBase.verticalExtent, defaults.headHeight) * bodyScale;
        angle:helpers.resolveNumber(headBase.angle, 90);
        direction:normalizeDirection(headBase.direction, direction);
      };
      legs:{
        left:helpers.normalizeInput(legsBase.left, {}) + {
          effectorCoordinate:resolvedLegs.left.effectorCoordinate;
          upperLength:resolvedLegs.left.upperLength;
          lowerLength:resolvedLegs.left.lowerLength;
          positiveBend:resolvedLegs.left.positiveBend;
          foot:mergeFoot(leftFootBase, leftFootLength);
        };
        right:helpers.normalizeInput(legsBase.right, {}) + {
          effectorCoordinate:resolvedLegs.right.effectorCoordinate;
          upperLength:resolvedLegs.right.upperLength;
          lowerLength:resolvedLegs.right.lowerLength;
          positiveBend:resolvedLegs.right.positiveBend;
          foot:mergeFoot(rightFootBase, rightFootLength);
        };
      };
      hands:{
        left:helpers.normalizeInput(handsBase.left, {}) + {
          effectorCoordinate:resolvedHands.left.effectorCoordinate;
          upperLength:resolvedHands.left.upperLength;
          lowerLength:resolvedHands.left.lowerLength;
          positiveBend:resolvedHands.left.positiveBend;
        };
        right:helpers.normalizeInput(handsBase.right, {}) + {
          effectorCoordinate:resolvedHands.right.effectorCoordinate;
          upperLength:resolvedHands.right.upperLength;
          lowerLength:resolvedHands.right.lowerLength;
          positiveBend:resolvedHands.right.positiveBend;
        };
      };
    };

    finalMeasurements:measurementsInput + updatedMeasurements;

    staticResult:if baseStaticBuilder = null then {} else baseStaticBuilder({
      position:anchorPoint;
      measurements:finalMeasurements;
    }) ?? {};

    sequenceState:{
      position:helpers.normalizePoint(anchorPoint, skeletonPosition);
      measurements:{} + finalMeasurements;
    };

    step:{
      mode:"zoom";
      progress:progress;
      zoomProgress:zoomProgress;
      zoom:zoomTarget;
      zoomFactor:zoomTarget;
      anchorPoint:anchorPoint;
      direction:direction;
      fixedSide:fixedSide;
      movingSide:movingSide;
      fixedPoint:addPoints(anchorPoint, legEffectors[fixedSide]);
      movingPoint:addPoints(anchorPoint, legEffectors[movingSide]);
    };

    eval helpers.normalizeInput(staticResult, {}) + {
      position:sequenceState.position;
      measurements:sequenceState.measurements;
      finalPosition:sequenceState.position;
      finalMeasurements:sequenceState.measurements;
      sequenceState:sequenceState;
      step:step;
    };
  };

  readEffectorOffset:(measurement, fallback)=> {
    raw:if measurement = null then null else measurement.effectorCoordinate;
    fallbackPoint:helpers.normalizePoint(fallback, [0,0]);
    numeric:helpers.resolveNumber(raw, null);
    eval if numeric != null then [fallbackPoint[0], numeric] else helpers.normalizePoint(raw, fallbackPoint);
  };

  readLimbLengths:(measurement, defaults)=> {
    base:helpers.normalizeInput(measurement, {});
    eval {
      upper:math.max(0, helpers.resolveNumber(base.upperLength, defaults.upper));
      lower:math.max(0, helpers.resolveNumber(base.lowerLength, defaults.lower));
      positiveBend:toBoolean(base.positiveBend, defaults.positiveBend);
    };
  };

  scaleLengths:(lengths, scale)=> {
    factor:math.max(0, helpers.resolveNumber(scale, 1));
    eval {
      upper:helpers.resolveNumber(lengths?.upper, 0) * factor;
      lower:helpers.resolveNumber(lengths?.lower, 0) * factor;
      positiveBend:toBoolean(lengths?.positiveBend, false);
    };
  };

  resolveTorsoDimensions:(torsoBase, bodyScale, direction)=> {
    width:helpers.resolveNumber(torsoBase.width, defaultTorsoWidth) * bodyScale;
    height:helpers.resolveNumber(torsoBase.height, defaultTorsoHeight) * bodyScale;
    shoulderExtension:math.max(helpers.resolveNumber(torsoBase.shoulderExtension, defaultShoulderExtension), 0) * bodyScale;
    eval { width:width; height:height; shoulderExtension:shoulderExtension; direction:direction };
  };

  resolveAttachments:(torso)=> {
    halfWidth:helpers.resolveNumber(torso.width, defaultTorsoWidth) * 0.5;
    handOffset:halfWidth + helpers.resolveNumber(torso.shoulderExtension, defaultShoulderExtension);
    handsY:helpers.resolveNumber(torso.height, defaultTorsoHeight) * 0.85;
    legOffset:helpers.resolveNumber(torso.width, defaultTorsoWidth) * 0.25;
    dir:normalizeDirection(torso.direction, "front");
    eval if dir = "left" or dir = "right" then {
      hands:{ left:[0, handsY]; right:[0, handsY] };
      legs:{ left:[0, 0]; right:[0, 0] };
    } else {
      hands:{ left:[-handOffset, handsY]; right:[handOffset, handsY] };
      legs:{ left:[-legOffset, 0]; right:[legOffset, 0] };
    };
  };

  resolveStraightLimb:(effector, lengths, bendFallback, attachment)=> {
    eff:helpers.normalizePoint(effector, [0,0]);
    attach:helpers.normalizePoint(attachment, [0,0]);
    dx:eff[0] - attach[0];
    dy:eff[1] - attach[1];
    effectorLength:math.max(0.000001, math.sqrt(dx * dx + dy * dy));
    upperBase:math.max(0, helpers.resolveNumber(lengths.upper, 0));
    lowerBase:math.max(0, helpers.resolveNumber(lengths.lower, 0));
    totalBase:math.max(0.000001, upperBase + lowerBase);
    upperRatio:upperBase / totalBase;
    lowerRatio:lowerBase / totalBase;
    eval {
      effectorCoordinate:eff;
      upperLength:effectorLength * upperRatio;
      lowerLength:effectorLength * lowerRatio;
      positiveBend:toBoolean(lengths.positiveBend, bendFallback);
    };
  };

  scaleOptionalLength:(value, factor)=> {
    lengthValue:helpers.resolveNumber(value, null);
    eval if lengthValue = null then value else lengthValue * math.max(0, helpers.resolveNumber(factor, 1));
  };

  mergeFoot:(baseFoot, scaledLength)=> {
    footInput:helpers.normalizeInput(baseFoot, {});
    eval if hasFootOverrides(footInput) then footInput + { length:scaledLength } else footInput;
  };

  hasFootOverrides:(foot)=> foot != null and (foot.length != null or foot.direction != null);

  clamp01:(value)=> {
    num:helpers.resolveNumber(value, 0);
    eval if num < 0 then 0 else if num > 1 then 1 else num;
  };

  lerp:(a, b, t)=> {
    start:helpers.resolveNumber(a, 0);
    end:helpers.resolveNumber(b, 0);
    blend:helpers.resolveNumber(t, 0);
    eval start + (end - start) * blend;
  };

  normalizeSide:(value, fallback)=> {
    defaultSide:fallback ?? "left";
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "left" then "left" else if textValue = "right" then "right" else defaultSide;
  };

  normalizeDirection:(value, fallback)=> {
    defaultDir:fallback ?? "front";
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "left" then "left"
    else if textValue = "right" then "right"
    else if textValue = "front" then "front"
    else if textValue = "back" then "back"
    else defaultDir;
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

  isPoint:(value)=> {
    x:helpers.resolveNumber(if value = null then null else value[0], null);
    y:helpers.resolveNumber(if value = null then null else value[1], null);
    eval value != null and x != null and y != null;
  };

  toBoolean:(value, fallback)=> if value = true then true else if value = false then false else fallback ?? false;

  eval steperManZoom;
}

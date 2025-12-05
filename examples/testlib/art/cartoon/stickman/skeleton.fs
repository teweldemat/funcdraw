{
  defaultTorsoWidth:6;
  defaultTorsoHeight:11;
  defaultArmUpper:4;
  defaultArmLower:3;
  defaultLegUpper:5.2;
  defaultLegLower:4.8;
  defaultShoulderExtension:defaultTorsoWidth * 0.15;
  defaultHandOffset:defaultTorsoWidth / 2 + defaultShoulderExtension;
  defaultHandDrop:defaultTorsoHeight * 0.85 - (defaultArmUpper + defaultArmLower);
  defaultLegOffset:defaultTorsoWidth * 0.25;
  defaultLegTotal:defaultLegUpper + defaultLegLower;
  defaultFootThickness:0.5;
  defaultPositionY:defaultLegTotal + defaultFootThickness;
  frontBackFootLineLength:defaultTorsoWidth * 0.12;
  ikEpsilon:0.000001;

  defaultMeasurements:{
    torso:{ width:defaultTorsoWidth; height:defaultTorsoHeight; shoulderExtension:defaultShoulderExtension; direction:"front" };
    head:{ verticalExtent:4.5; angle:90; direction:"front" };
    hands:{
      left:{ upperLength:defaultArmUpper; lowerLength:defaultArmLower; effectorCoordinate:[-defaultHandOffset, defaultHandDrop]; positiveBend:false };
      right:{ upperLength:defaultArmUpper; lowerLength:defaultArmLower; effectorCoordinate:[defaultHandOffset, defaultHandDrop]; positiveBend:true };
    };
    legs:{
      left:{ upperLength:defaultLegUpper; lowerLength:defaultLegLower; effectorCoordinate:[-defaultLegOffset, -defaultLegTotal]; positiveBend:true };
      right:{ upperLength:defaultLegUpper; lowerLength:defaultLegLower; effectorCoordinate:[defaultLegOffset, -defaultLegTotal]; positiveBend:true };
    };
  };

  normalizeDirection:(value, fallback)=> {
    defaultDir:fallback ?? "front";
    lowered:if value = null then "" else text.lower(format(value));
    eval if lowered = "left" then "left"
    else if lowered = "right" then "right"
    else if lowered = "back" then "back"
    else if lowered = "front" then "front"
    else defaultDir;
  };

  normalizeFootDirection:(value, fallback)=> {
    defaultDir:fallback ?? "left";
    lowered:if value = null then "" else text.lower(format(value));
    eval if lowered = "left" then "left"
    else if lowered = "right" then "right"
    else if lowered = "center" then "center"
    else if lowered = "middle" then "center"
    else defaultDir;
  };

  adjustEffectorForProfile:(effector, torsoDirection)=> {
    eval if torsoDirection = "left" or torsoDirection = "right" then [0, effector[1]] else effector;
  };

  resolveFootMeasurement:(measurement, defaultDirection, defaultLengthOverride)=> {
    normalizedFoot:helpers.normalizeInput(measurement.foot, null);
    eval if normalizedFoot != null then {
      length:helpers.resolveOptionalNumber(normalizedFoot.length);
      direction:normalizeFootDirection(normalizedFoot.direction, defaultDirection);
    } else {
      length:if defaultLengthOverride != null then defaultLengthOverride else null;
      direction:defaultDirection;
    };
  };

  solveLimbPose:(attachmentPoint, targetPoint, upperLength, lowerLength, positiveBend)=> {
    safeUpper:math.max(math.abs(upperLength), ikEpsilon);
    safeLower:math.max(math.abs(lowerLength), ikEpsilon);
    dx:targetPoint[0] - attachmentPoint[0];
    dy:targetPoint[1] - attachmentPoint[1];
    distance:math.sqrt(dx * dx + dy * dy);
    maxReach:safeUpper + safeLower;
    minReach:math.abs(safeUpper - safeLower) + ikEpsilon;
    direction:if distance > ikEpsilon then [dx / distance, dy / distance] else [0,1];
    reach:helpers.clamp(distance, minReach, maxReach);
    reachTarget:[
      attachmentPoint[0] + direction[0] * reach,
      attachmentPoint[1] + direction[1] * reach
    ];
    cosShoulder:helpers.clamp(
      (safeUpper * safeUpper + reach * reach - safeLower * safeLower) / (2 * safeUpper * reach),
      -1,
      1
    );
    sinShoulder:math.sqrt(math.max(0, 1 - cosShoulder * cosShoulder));
    perp:[-direction[1], direction[0]];
    bendSign:if positiveBend then 1 else -1;
    hingePoint:[
      attachmentPoint[0] + direction[0] * (safeUpper * cosShoulder) + perp[0] * (bendSign * safeUpper * sinShoulder),
      attachmentPoint[1] + direction[1] * (safeUpper * cosShoulder) + perp[1] * (bendSign * safeUpper * sinShoulder)
    ];
    finalVec:[reachTarget[0] - hingePoint[0], reachTarget[1] - hingePoint[1]];
    finalMag:math.sqrt(finalVec[0] * finalVec[0] + finalVec[1] * finalVec[1]);
    reachDirection:if finalMag > ikEpsilon then [finalVec[0] / finalMag, finalVec[1] / finalMag] else [direction[0], direction[1]];

    eval {
      hingePoint:hingePoint;
      reachTarget:reachTarget;
      reachDirection:reachDirection;
      bendSign:bendSign;
    };
  };

  computeTorsoFrame:(position, torsoMeasurements)=> {
    torsoConfig:helpers.normalizeInput(torsoMeasurements ?? {}, {});
    width:helpers.resolveNumber(torsoConfig.width, defaultMeasurements.torso.width);
    height:helpers.resolveNumber(torsoConfig.height, defaultMeasurements.torso.height);
    rawShoulderExtension:helpers.resolveNumber(torsoConfig.shoulderExtension, null);
    direction:normalizeDirection(torsoConfig.direction, defaultMeasurements.torso.direction);
    centerX:position[0];
    bottomY:position[1];
    halfWidth:width / 2;
    topY:bottomY + height;
    handsY:topY - height * 0.15;
    shoulderExtension:math.max(if rawShoulderExtension != null then rawShoulderExtension else width * 0.15, 0);
    handOffset:halfWidth + shoulderExtension;
    legOffset:width * 0.25;
    isProfile:direction = "left" or direction = "right";
    handAttachments:{
      left:if isProfile then [centerX, handsY] else [centerX - handOffset, handsY];
      right:if isProfile then [centerX, handsY] else [centerX + handOffset, handsY];
    };
    legAttachments:{
      left:if isProfile then [centerX, bottomY] else [centerX - legOffset, bottomY];
      right:if isProfile then [centerX, bottomY] else [centerX + legOffset, bottomY];
    };

    eval {
      centerBottomPoint:position;
      width:width;
      height:height;
      shoulderExtension:shoulderExtension;
      direction:direction;
      headAttachmentPoint:[centerX, topY];
      handAttachmentPoints:handAttachments;
      legAttachmentPoints:legAttachments;
    };
  };

  buildHeadSkeleton:(torso, headMeasurements)=> {
    headConfig:helpers.normalizeInput(headMeasurements ?? {}, {});
    eval {
      attachmentPoint:torso.headAttachmentPoint;
      verticalExtent:helpers.resolveNumber(headConfig.verticalExtent, defaultMeasurements.head.verticalExtent);
      angle:helpers.resolveNumber(headConfig.angle, defaultMeasurements.head.angle);
      direction:normalizeDirection(headConfig.direction, torso.direction ?? defaultMeasurements.head.direction);
    };
  };

  buildHandSkeleton:(position, attachmentPoints, handMeasurements, torsoDirection, handOverrideInputs)=> {
    defaults:defaultMeasurements.hands;
    measurements:helpers.normalizeInput(handMeasurements ?? {}, {});
    handOverrides:helpers.normalizeInput(handOverrideInputs, null);

    buildSide:(side)=> {
      measurement:helpers.normalizeInput(measurements[side], {});
      sideDefaults:defaults[side];
      overrideInput:if handOverrides = null then null else helpers.normalizeInput(handOverrides[side], null);
      hasCustomEffector:overrideInput != null and overrideInput.effectorCoordinate != null;
      upper:helpers.resolveNumber(measurement.upperLength, sideDefaults.upperLength);
      lower:helpers.resolveNumber(measurement.lowerLength, sideDefaults.lowerLength);
      attachmentOffset:[
        attachmentPoints[side][0] - position[0],
        attachmentPoints[side][1] - position[1] - (upper + lower)
      ];
      baseDefaultEffector:helpers.normalizePoint(sideDefaults.effectorCoordinate, attachmentOffset);
      fallbackEffector:adjustEffectorForProfile(baseDefaultEffector, torsoDirection);
      effector:if hasCustomEffector then helpers.normalizePoint(measurement.effectorCoordinate, fallbackEffector) else fallbackEffector;
      targetPoint:helpers.addOffset(position, effector);
      positiveBend:helpers.resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend);
      ik:solveLimbPose(attachmentPoints[side], targetPoint, upper, lower, positiveBend);

      eval {
        attachmentPoint:attachmentPoints[side];
        targetPoint:targetPoint;
        reachTarget:ik.reachTarget;
        bendPoint:ik.hingePoint;
        reachDirection:ik.reachDirection;
        bendDirection:ik.bendSign;
        lengths:{ upper:upper; lower:lower };
        positiveBend:positiveBend;
        joints:{
          attachment:attachmentPoints[side];
          hinge:ik.hingePoint;
          effector:ik.reachTarget;
        };
      };
    };

    eval { left:buildSide("left"); right:buildSide("right") };
  };

  buildLegSkeleton:(position, attachmentPoints, legMeasurements, torsoDirection, legOverrideInputs)=> {
    defaults:defaultMeasurements.legs;
    measurements:helpers.normalizeInput(legMeasurements ?? {}, {});
    legOverrides:helpers.normalizeInput(legOverrideInputs, null);
    isProfile:torsoDirection = "left" or torsoDirection = "right";
    isFrontFacing:torsoDirection = "front" or torsoDirection = "back";

    buildSide:(side)=> {
      measurement:helpers.normalizeInput(measurements[side], {});
      sideDefaults:defaults[side];
      overrideInput:if legOverrides = null then null else helpers.normalizeInput(legOverrides[side], null);
      hasCustomEffector:overrideInput != null and overrideInput.effectorCoordinate != null;
      upperLength:helpers.resolveNumber(measurement.upperLength, sideDefaults.upperLength);
      lowerLength:helpers.resolveNumber(measurement.lowerLength, sideDefaults.lowerLength);
      attachmentOffset:[attachmentPoints[side][0] - position[0], -(upperLength + lowerLength)];
      baseDefaultEffector:helpers.normalizePoint(sideDefaults.effectorCoordinate, attachmentOffset);
      fallbackEffector:adjustEffectorForProfile(baseDefaultEffector, torsoDirection);
      effector:if hasCustomEffector then helpers.normalizePoint(measurement.effectorCoordinate, fallbackEffector) else fallbackEffector;
      positiveBend:helpers.resolveBoolean(measurement.positiveBend, sideDefaults.positiveBend);
      defaultFootDirection:if isProfile then torsoDirection else if isFrontFacing then "center" else if positiveBend then "right" else "left";
      frontFacingFootLength:if isFrontFacing then frontBackFootLineLength else null;
      foot:resolveFootMeasurement(measurement, defaultFootDirection, frontFacingFootLength);
      targetPoint:helpers.addOffset(position, effector);
      ik:solveLimbPose(attachmentPoints[side], targetPoint, upperLength, lowerLength, positiveBend);

      eval {
        attachmentPoint:attachmentPoints[side];
        targetPoint:targetPoint;
        reachTarget:ik.reachTarget;
        bendPoint:ik.hingePoint;
        reachDirection:ik.reachDirection;
        bendDirection:ik.bendSign;
        lengths:{ upper:upperLength; lower:lowerLength };
        positiveBend:positiveBend;
        foot:foot;
        joints:{
          attachment:attachmentPoints[side];
          hinge:ik.hingePoint;
          effector:ik.reachTarget;
        };
      };
    };

    eval { left:buildSide("left"); right:buildSide("right") };
  };

  buildStickManSkeleton:(optionsInput)=> {
    normalizedOptions:helpers.normalizeInput(optionsInput ?? {}, {});
    measurementOverrides:helpers.normalizeInput(normalizedOptions.measurements, {});
    measurements:defaultMeasurements + measurementOverrides;
    positionFallback:[0, defaultPositionY];
    position:helpers.normalizePoint(normalizedOptions.position, positionFallback);
    handOverrideInputs:helpers.normalizeInput(measurementOverrides.hands, null);
    legOverrideInputs:helpers.normalizeInput(measurementOverrides.legs, null);

    torso:computeTorsoFrame(position, measurements.torso);
    head:buildHeadSkeleton(torso, measurements.head);
    hands:buildHandSkeleton(position, torso.handAttachmentPoints, measurements.hands, torso.direction, handOverrideInputs);
    legs:buildLegSkeleton(position, torso.legAttachmentPoints, measurements.legs, torso.direction, legOverrideInputs);

    eval {
      skeleton:{
        position:position;
        torso:torso;
        head:head;
        hands:hands;
        legs:legs;
      };
      normalizedOptions:normalizedOptions;
    };
  };

  eval {
    build:buildStickManSkeleton;
    defaultMeasurements:defaultMeasurements;
    normalizeInput:helpers.normalizeInput;
    mergeDeep:helpers.mergeDeep;
  };
}

(normalizedInput)=>
{
  defaults:defaults;
  normalized:if normalizedInput = null then normalize({}) else normalizedInput;

  position:normalized.position;
  measurements:normalized.measurements;
  handOverrides:normalized.handOverrides;
  legOverrides:normalized.legOverrides;

  torso:computeTorsoFrame(position, measurements.torso);
  head:buildHeadSkeleton(torso, measurements.head);
  hands:buildHandSkeleton(position, torso.handAttachmentPoints, measurements.hands, torso.direction, handOverrides);
  legs:buildLegSkeleton(position, torso.legAttachmentPoints, measurements.legs, torso.direction, legOverrides);

  eval {
    skeleton:{
      position:position;
      torso:torso;
      head:head;
      hands:hands;
      legs:legs;
    };
    normalizedOptions:normalized.normalizedOptions ?? normalized;
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
    rawFoot:measurement.foot ?? null;
    eval if rawFoot != null then {
      length:rawFoot.length;
      direction:normalizeFootDirection(rawFoot.direction, defaultDirection);
    } else {
      length:if defaultLengthOverride != null then defaultLengthOverride else null;
      direction:defaultDirection;
    };
  };

  solveLimbPose:(attachmentPoint, targetPoint, upperLength, lowerLength, positiveBend)=> {
    safeUpper:math.max(math.abs(upperLength), defaults.ikEpsilon);
    safeLower:math.max(math.abs(lowerLength), defaults.ikEpsilon);
    dx:targetPoint[0] - attachmentPoint[0];
    dy:targetPoint[1] - attachmentPoint[1];
    distance:math.sqrt(dx * dx + dy * dy);
    maxReach:safeUpper + safeLower;
    minReach:math.abs(safeUpper - safeLower) + defaults.ikEpsilon;
    direction:if distance > defaults.ikEpsilon then [dx / distance, dy / distance] else [0,1];
    reach:clamp(distance, minReach, maxReach);
    reachTarget:[
      attachmentPoint[0] + direction[0] * reach,
      attachmentPoint[1] + direction[1] * reach
    ];
    numerator:safeUpper * safeUpper + reach * reach - safeLower * safeLower;
    denominator:(2 * safeUpper) * reach;
    cosShoulder:clamp(numerator / denominator, -1, 1);
    sinShoulder:math.sqrt(math.max(0, 1 - cosShoulder * cosShoulder));
    perp:[-direction[1], direction[0]];
    bendSign:if positiveBend then 1 else -1;
    scaledSin:sinShoulder * safeUpper;
    hingePoint:[
      attachmentPoint[0] + direction[0] * (safeUpper * cosShoulder) + perp[0] * (bendSign * scaledSin),
      attachmentPoint[1] + direction[1] * (safeUpper * cosShoulder) + perp[1] * (bendSign * scaledSin)
    ];
    finalVec:[reachTarget[0] - hingePoint[0], reachTarget[1] - hingePoint[1]];
    finalMag:math.sqrt(finalVec[0] * finalVec[0] + finalVec[1] * finalVec[1]);
    reachDirection:if finalMag > defaults.ikEpsilon then [finalVec[0] / finalMag, finalVec[1] / finalMag] else [direction[0], direction[1]];

    eval {
      hingePoint:hingePoint;
      reachTarget:reachTarget;
      reachDirection:reachDirection;
      bendSign:bendSign;
    };
  };

  computeTorsoFrame:(position, torsoMeasurements)=> {
    torsoConfig:torsoMeasurements ?? {};
    base:defaults.defaultMeasurements.torso;
    width:if torsoConfig.width = null then base.width else torsoConfig.width;
    height:if torsoConfig.height = null then base.height else torsoConfig.height;
    rawShoulderExtension:torsoConfig.shoulderExtension;
    direction:normalizeDirection(torsoConfig.direction, base.direction);
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
    headConfig:headMeasurements ?? {};
    base:defaults.defaultMeasurements.head;
    eval {
      attachmentPoint:torso.headAttachmentPoint;
      verticalExtent:if headConfig.verticalExtent = null then base.verticalExtent else headConfig.verticalExtent;
      angle:if headConfig.angle = null then base.angle else headConfig.angle;
      direction:normalizeDirection(headConfig.direction, torso.direction ?? base.direction);
    };
  };

  buildHandSkeleton:(position, attachmentPoints, handMeasurements, torsoDirection, handOverrideInputs)=> {
    defaultsHands:defaults.defaultMeasurements.hands;
    measurements:handMeasurements ?? {};
    overrides:handOverrideInputs ?? null;

    buildSide:(side)=> {
      measurement:measurements[side] ?? {};
      sideDefaults:defaultsHands[side];
      overrideInput:if overrides = null then null else (overrides[side] ?? null);
      hasCustomEffector:overrideInput != null and overrideInput.effectorCoordinate != null;
      upper:if measurement.upperLength = null then sideDefaults.upperLength else measurement.upperLength;
      lower:if measurement.lowerLength = null then sideDefaults.lowerLength else measurement.lowerLength;
      attachmentOffset:[
        attachmentPoints[side][0] - position[0],
        attachmentPoints[side][1] - position[1] - (upper + lower)
      ];
      baseDefaultEffector:if sideDefaults.effectorCoordinate = null then attachmentOffset else sideDefaults.effectorCoordinate;
      fallbackEffector:adjustEffectorForProfile(baseDefaultEffector, torsoDirection);
      effector:if hasCustomEffector then (measurement.effectorCoordinate ?? fallbackEffector) else fallbackEffector;
      targetPoint:addOffset(position, effector);
      positiveBend:if measurement.positiveBend = null then sideDefaults.positiveBend else measurement.positiveBend;
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
    defaultsLegs:defaults.defaultMeasurements.legs;
    measurements:legMeasurements ?? {};
    overrides:legOverrideInputs ?? null;
    isProfile:torsoDirection = "left" or torsoDirection = "right";
    isFrontFacing:torsoDirection = "front" or torsoDirection = "back";

    buildSide:(side)=> {
      measurement:measurements[side] ?? {};
      sideDefaults:defaultsLegs[side];
      overrideInput:if overrides = null then null else (overrides[side] ?? null);
      hasCustomEffector:overrideInput != null and overrideInput.effectorCoordinate != null;
      upperLength:if measurement.upperLength = null then sideDefaults.upperLength else measurement.upperLength;
      lowerLength:if measurement.lowerLength = null then sideDefaults.lowerLength else measurement.lowerLength;
      attachmentOffset:[attachmentPoints[side][0] - position[0], -(upperLength + lowerLength)];
      baseDefaultEffector:if sideDefaults.effectorCoordinate = null then attachmentOffset else sideDefaults.effectorCoordinate;
      fallbackEffector:adjustEffectorForProfile(baseDefaultEffector, torsoDirection);
      effector:if hasCustomEffector then (measurement.effectorCoordinate ?? fallbackEffector) else fallbackEffector;
      positiveBend:if measurement.positiveBend = null then sideDefaults.positiveBend else measurement.positiveBend;
      defaultFootDirection:if isProfile then torsoDirection else if isFrontFacing then "center" else if positiveBend then "right" else "left";
      frontFacingFootLength:if isFrontFacing then defaults.frontBackFootLineLength else null;
      foot:resolveFootMeasurement(measurement, defaultFootDirection, frontFacingFootLength);
      targetPoint:addOffset(position, effector);
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

  clamp:(value, min, max)=> {
    eval if value < min then min else if value > max then max else value;
  };

  addOffset:(point, offset)=> [
    point[0] + offset[0],
    point[1] + offset[1]
  ];
}

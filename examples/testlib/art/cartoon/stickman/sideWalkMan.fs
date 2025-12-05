(optionsInput)=>
{
  defaults:{ position:[0,10]; leftOffset:[-2,-11]; rightOffset:[2,-11] };

  input:helpers.normalizeInput(optionsInput ?? {}, {});
  anchorBase:helpers.normalizePoint(input.initialPosition, defaults.position);
  measurementsInput:helpers.normalizeInput(input.initialMeasurements, {});
  displacement:helpers.resolveNumber(input.displacement, 0);
  progress:clamp01(helpers.resolveNumber(input.progress, 0));
  direction:normalizeDirection(input.direction, "right");
  strideDirection:if displacement >= 0 then 1 else -1;

  defaultOffsets:{ left:defaults.leftOffset; right:defaults.rightOffset };
  initialLegs:helpers.normalizeInput(measurementsInput.legs, {});
  initialLeftFoot:helpers.addOffset(anchorBase, resolveLegOffset(initialLegs.left, defaultOffsets.left));
  initialRightFoot:helpers.addOffset(anchorBase, resolveLegOffset(initialLegs.right, defaultOffsets.right));

  movingSide:if strideDirection >= 0
    then if initialLeftFoot[0] <= initialRightFoot[0] then "left" else "right"
    else if initialLeftFoot[0] >= initialRightFoot[0] then "left" else "right";
  fixedSide:if movingSide = "left" then "right" else "left";
  fixedOffset:if fixedSide = "left" then resolveLegOffset(initialLegs.left, defaultOffsets.left) else resolveLegOffset(initialLegs.right, defaultOffsets.right);
  movingOffset:if movingSide = "left" then resolveLegOffset(initialLegs.left, defaultOffsets.left) else resolveLegOffset(initialLegs.right, defaultOffsets.right);

  fixedPoint:helpers.addOffset(anchorBase, fixedOffset);
  movingStartPoint:helpers.addOffset(anchorBase, movingOffset);
  movingTargetPoint:[movingStartPoint[0] + displacement, movingStartPoint[1]];
  strideOverride:helpers.resolveNumber(input.strideLength, null);
  strideLength:if strideOverride != null and strideOverride > 0 then strideOverride else resolveStrideLength(measurementsInput, defaultOffsets);
  spacing:math.max(0.000001, strideLength);
  overreach:math.max(spacing * 0.25, 0.75);
  passDistance:spacing + overreach;
  stepCount:math.max(1, math.ceil(math.abs(displacement) / passDistance));
  totalProgress:progress * stepCount;
  activeStepIndex:math.min(stepCount - 1, math.floor(totalProgress));
  activeStepPhase:totalProgress - activeStepIndex;

  mergedMeasurements:mergeFacing(measurementsInput, direction);
  initialState:{
    anchor:anchorBase;
    measurements:mergedMeasurements;
    leftFoot:initialLeftFoot;
    rightFoot:initialRightFoot;
    remainingDistance:math.abs(displacement);
  };
  finalState:simulateWalk(0, initialState);

  eval {
    measurements:helpers.mergeDeep({}, finalState.measurements);
    position:helpers.normalizePoint(finalState.anchor, defaults.position);
  };

  simulateWalk:(index, state)=> {
    eval if index >= stepCount then state else {
      stepFixedSide:if index % 2 = 0 then fixedSide else movingSide;
      stepMovingSide:if stepFixedSide = "left" then "right" else "left";
      isActive:index = activeStepIndex;
      stepProgress:if index < activeStepIndex then 1 else if isActive then activeStepPhase else 0;
      remainingSteps:math.max(1, stepCount - index);
      fixedX:if stepFixedSide = "left" then state.leftFoot[0] else state.rightFoot[0];

      strideMagnitude:if remainingSteps = 1 then state.remainingDistance else {
        reserve:(remainingSteps - 1) * passDistance;
        allowed:math.max(passDistance, state.remainingDistance - reserve);
        eval math.min(allowed, state.remainingDistance);
      };

      targetX:fixedX + strideMagnitude * strideDirection;
      movingStart:if stepMovingSide = "left" then state.leftFoot else state.rightFoot;
      movingTarget:[targetX, movingStart[1]];

      measurementsWithOffsets:applyLegOffsets(state.measurements, state.anchor, state.leftFoot, state.rightFoot, defaultOffsets);
      stepperResult:steperManProfile({
        position:state.anchor;
        measurements:measurementsWithOffsets;
        handSwing:input.handSwing;
        movingSide:stepMovingSide;
        movingFeetTargetPoint:movingTarget;
        progress:stepProgress;
      });

      normalizedResult:helpers.normalizeInput(stepperResult ?? {}, {});
      updatedMeasurements:helpers.normalizeInput(normalizedResult.measurements, measurementsWithOffsets);
      updatedAnchor:helpers.normalizePoint(normalizedResult.position, state.anchor);
      updatedLegs:helpers.normalizeInput(updatedMeasurements.legs, {});
      updatedLeftFoot:helpers.addOffset(updatedAnchor, resolveLegOffset(updatedLegs.left, defaultOffsets.left));
      updatedRightFoot:helpers.addOffset(updatedAnchor, resolveLegOffset(updatedLegs.right, defaultOffsets.right));
      updatedRemaining:math.max(0, state.remainingDistance - strideMagnitude);
      updatedState:{
        anchor:updatedAnchor;
        measurements:updatedMeasurements;
        leftFoot:updatedLeftFoot;
        rightFoot:updatedRightFoot;
        remainingDistance:updatedRemaining;
      };

      eval if isActive then updatedState else simulateWalk(index + 1, updatedState);
    };
  };

  resolveLegOffset:(measurement, fallback)=> {
    legInput:helpers.normalizeInput(measurement ?? {}, {});
    eval helpers.normalizePoint(legInput.effectorCoordinate, fallback);
  };

  mergeFacing:(measurements, facing)=> {
    base:helpers.normalizeInput(measurements ?? {}, {});
    torso:helpers.normalizeInput(base.torso, {});
    head:helpers.normalizeInput(base.head, {});
    resolvedDirection:normalizeDirection(if torso.direction != null then torso.direction else head.direction, facing);
    eval helpers.mergeDeep(base, {
      torso:{ direction:resolvedDirection };
      head:{ direction:resolvedDirection };
    });
  };

  resolveStrideLength:(measurements, defaults)=> {
    legs:helpers.normalizeInput((measurements ?? {}).legs, {});
    left:helpers.normalizeInput(legs.left, {});
    right:helpers.normalizeInput(legs.right, {});
    leftLength:math.max(0, helpers.resolveNumber(left.upperLength, 0) + helpers.resolveNumber(left.lowerLength, 0));
    rightLength:math.max(0, helpers.resolveNumber(right.upperLength, 0) + helpers.resolveNumber(right.lowerLength, 0));
    avgLength:(leftLength + rightLength) / 2;
    eval if avgLength > 0 then math.max(1, avgLength * 0.35) else {
      spacing:math.abs(defaults.right[0] - defaults.left[0]);
      eval if spacing > 0 then spacing else 1;
    };
  };

  applyLegOffsets:(measurements, anchor, leftFoot, rightFoot, defaults)=> {
    base:helpers.normalizeInput(measurements ?? {}, {});
    legs:helpers.normalizeInput(base.legs, {});
    anchorPoint:helpers.normalizePoint(anchor, [0,0]);
    leftPoint:helpers.normalizePoint(leftFoot, helpers.addOffset(anchorPoint, defaults.left));
    rightPoint:helpers.normalizePoint(rightFoot, helpers.addOffset(anchorPoint, defaults.right));
    leftOffset:subtractPoints(leftPoint, anchorPoint);
    rightOffset:subtractPoints(rightPoint, anchorPoint);
    eval helpers.mergeDeep(base, {
      legs:{
        left:helpers.mergeDeep(helpers.normalizeInput(legs.left, {}), { effectorCoordinate:leftOffset });
        right:helpers.mergeDeep(helpers.normalizeInput(legs.right, {}), { effectorCoordinate:rightOffset });
      }
    });
  };

  normalizeDirection:(value, fallback)=> {
    defaultDir:fallback ?? "right";
    text:if value = null then "" else text.lower(format(value));
    eval if text = "front" or text = "back" or text = "left" or text = "right" then text else defaultDir;
  };

  clamp01:(value)=> {
    num:helpers.resolveNumber(value, 0);
    eval if num < 0 then 0 else if num > 1 then 1 else num;
  };

  subtractPoints:(a, b)=> {
    aSafe:if a = null then [0,0] else a;
    bSafe:if b = null then [0,0] else b;
    eval [
      helpers.resolveNumber(aSafe[0], 0) - helpers.resolveNumber(bSafe[0], 0),
      helpers.resolveNumber(aSafe[1], 0) - helpers.resolveNumber(bSafe[1], 0)
    ];
  };
}

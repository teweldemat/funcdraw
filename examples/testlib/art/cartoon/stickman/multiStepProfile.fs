(optionsInput)=>
{
  helpers:cartoon.helpers;
  defaults:{ position:[0,10]; leftOffset:[-2,-11]; rightOffset:[2,-11] };

  input:optionsInput ?? {};
  anchorBase:if input.initialPosition = null then defaults.position else input.initialPosition;
  measurementsInput:input.initialMeasurements ?? {};
  displacement:if input.displacement = null then 0 else input.displacement;
  progress:clamp01(if input.progress = null then 0 else input.progress + 0);
  direction:normalizeDirection(input.direction, "right");
  strideDirection:if displacement >= 0 then 1 else -1;
  debugEnabled:input.debug = true;

  defaultOffsets:{ left:defaults.leftOffset; right:defaults.rightOffset };
  initialLegs:cloneLegs(measurementsInput.legs);
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
  strideOverride:if input.strideLength = null then null else input.strideLength;
  strideLength:if strideOverride != null and strideOverride > 0 then strideOverride else resolveStrideLength(measurementsInput, defaultOffsets);
  spacing:math.max(0.000001, strideLength);
  overreach:math.max(spacing * 0.25, 0.75);
  passDistance:spacing + overreach;
  stepCount:math.max(1, math.ceil(math.abs(displacement) / passDistance));
  totalProgress:progress * stepCount;
  activeStepIndex:math.min(stepCount - 1, math.floor(totalProgress));
  activeStepPhase:totalProgress - activeStepIndex;

  mergedMeasurements:cloneMeasurements(mergeFacing(measurementsInput, direction));
  initialState:{
    anchor:clonePoint(anchorBase);
    measurements:cloneMeasurements(mergedMeasurements);
    leftFoot:clonePoint(initialLeftFoot);
    rightFoot:clonePoint(initialRightFoot);
    remainingDistance:math.abs(displacement);
    history:if debugEnabled then [] else null;
  };
  finalState:simulateWalk(0, initialState);

  eval {
    measurements:{} + finalState.measurements;
    position:if finalState.anchor = null then defaults.position else finalState.anchor;
    debug:if debugEnabled then finalState else null;
  };

  simulateWalk:(index, state)=> {
    eval if index >= stepCount then state else {
      stepFixedSide:if index % 2 = 0 then fixedSide else movingSide;
      stepMovingSide:if stepFixedSide = "left" then "right" else "left";
      isActive:index = activeStepIndex;
      stepProgressRaw:if index < activeStepIndex then 1 else if isActive then activeStepPhase else 0;
      stepProgress:stepProgressRaw + 0;
      remainingSteps:math.max(1, stepCount - index);
      fixedX:if stepFixedSide = "left" then state.leftFoot[0] else state.rightFoot[0];

      strideMagnitude:if remainingSteps = 1 then state.remainingDistance else {
        reserve:(remainingSteps - 1) * passDistance;
        allowed:math.max(passDistance, state.remainingDistance - reserve);
        eval math.min(allowed, state.remainingDistance);
      };

      movingStart:if stepMovingSide = "left" then state.leftFoot else state.rightFoot;
      targetX:movingStart[0] + strideMagnitude * strideDirection;
      movingTarget:[targetX, movingStart[1]];

      measurementsBase:cloneMeasurements(state.measurements ?? mergedMeasurements);
      measurementsWithOffsets:applyLegOffsets(measurementsBase, state.anchor, state.leftFoot, state.rightFoot, defaultOffsets);
      stepperHelper:if singleStepProfile = null then steperManProfile else singleStepProfile;
      stepperResult:stepperHelper({
        position:clonePoint(state.anchor);
        measurements:measurementsWithOffsets;
        handSwing:input.handSwing;
        movingSide:stepMovingSide;
        movingFeetTargetPoint:clonePoint(movingTarget);
        progress:stepProgress;
        disableStatic:true;
      });

      normalizedResult:if stepperResult = null then {} else stepperResult;
      updatedMeasurements:cloneMeasurements(
        if normalizedResult.measurements = null then measurementsWithOffsets else normalizedResult.measurements
      );
      updatedAnchor:clonePoint(if normalizedResult.position = null then state.anchor else normalizedResult.position);
      updatedLegs:updatedMeasurements.legs ?? {};
      updatedLeftFoot:clonePoint(helpers.addOffset(updatedAnchor, resolveLegOffset(updatedLegs.left, defaultOffsets.left)));
      updatedRightFoot:clonePoint(helpers.addOffset(updatedAnchor, resolveLegOffset(updatedLegs.right, defaultOffsets.right)));
      updatedRemaining:math.max(0, state.remainingDistance - strideMagnitude);
      debugStep:if debugEnabled then {
        index:index;
        activeStepIndex:activeStepIndex;
        stepProgress:stepProgress;
        stepProgressRaw:stepProgressRaw;
        stepFixedSide:stepFixedSide;
        stepMovingSide:stepMovingSide;
        strideMagnitude:strideMagnitude;
        movingStart:movingStart;
        movingTarget:movingTarget;
        incomingAnchor:state.anchor;
        incomingLeftFoot:state.leftFoot;
        incomingRightFoot:state.rightFoot;
        incomingMeasurements:state.measurements;
        measurementsWithOffsets:measurementsWithOffsets;
        stepperResult:stepperResult;
        updatedAnchor:updatedAnchor;
        updatedMeasurements:updatedMeasurements;
        updatedLeftFoot:updatedLeftFoot;
        updatedRightFoot:updatedRightFoot;
      } else null;
      updatedHistory:if debugEnabled then (state.history ?? []) + [{
        index:index;
        anchor:clonePoint(updatedAnchor);
        leftFoot:clonePoint(updatedLeftFoot);
        rightFoot:clonePoint(updatedRightFoot);
        measurements:updatedMeasurements;
        stepProgress:stepProgress;
      }] else state.history;
      updatedState:{
        anchor:updatedAnchor;
        measurements:updatedMeasurements;
        leftFoot:updatedLeftFoot;
        rightFoot:updatedRightFoot;
        remainingDistance:updatedRemaining;
        debugStep:debugStep;
        history:updatedHistory;
    };

    eval if isActive then updatedState else simulateWalk(index + 1, updatedState);
  };
  };

  resolveLegOffset:(measurement, fallback)=> {
    legInput:measurement ?? {};
    eval if legInput.effectorCoordinate = null then fallback else legInput.effectorCoordinate;
  };

  mergeFacing:(measurements, facing)=> {
    base:measurements ?? {};
    torso:base.torso ?? {};
    head:base.head ?? {};
    resolvedDirection:normalizeDirection(if torso.direction != null then torso.direction else head.direction, facing);
    eval base + {
      torso:{ direction:resolvedDirection };
      head:{ direction:resolvedDirection };
    };
  };

  resolveStrideLength:(measurements, defaults)=> {
    legs:(measurements ?? {}).legs ?? {};
    left:legs.left ?? {};
    right:legs.right ?? {};
    leftLength:math.max(0, (if left.upperLength = null then 0 else left.upperLength) + (if left.lowerLength = null then 0 else left.lowerLength));
    rightLength:math.max(0, (if right.upperLength = null then 0 else right.upperLength) + (if right.lowerLength = null then 0 else right.lowerLength));
    avgLength:(leftLength + rightLength) / 2;
    eval if avgLength > 0 then math.max(1, avgLength * 0.35) else {
      spacing:math.abs(defaults.right[0] - defaults.left[0]);
      eval if spacing > 0 then spacing else 1;
    };
  };

  applyLegOffsets:(measurements, anchor, leftFoot, rightFoot, defaults)=> {
    base:{} + (measurements ?? {});
    legInput:{} + (base.legs ?? {});
    anchorPoint:if anchor = null then [0,0] else anchor;
    leftPoint:if leftFoot = null then helpers.addOffset(anchorPoint, defaults.left) else leftFoot;
    rightPoint:if rightFoot = null then helpers.addOffset(anchorPoint, defaults.right) else rightFoot;
    leftOffset:subtractPoints(leftPoint, anchorPoint);
    rightOffset:subtractPoints(rightPoint, anchorPoint);
    eval base + {
      legs:{
        left:(legInput.left ?? {}) + { effectorCoordinate:leftOffset };
        right:(legInput.right ?? {}) + { effectorCoordinate:rightOffset };
      }
    };
  };

  normalizeDirection:(value, fallback)=> {
    defaultDir:fallback ?? "right";
    text:if value = null then "" else text.lower(format(value));
    eval if text = "front" or text = "back" or text = "left" or text = "right" then text else defaultDir;
  };

  clamp01:(value)=> {
    num:if value = null then 0 else value;
    eval if num < 0 then 0 else if num > 1 then 1 else num;
  };

  cloneMeasurements:(value)=> {
    base:value ?? {};
    eval base + {
      torso:if base.torso = null then null else {} + base.torso;
      head:if base.head = null then null else {} + base.head;
      hands:cloneHands(base.hands);
      legs:cloneLegs(base.legs);
    };
  };

  cloneHands:(value)=> {
    base:value ?? {};
    eval {
      left:cloneHand(base.left);
      right:cloneHand(base.right);
    };
  };

  cloneHand:(hand)=> {
    eval if hand = null then null else hand + { effectorCoordinate:clonePoint(hand.effectorCoordinate) };
  };

  cloneLegs:(value)=> {
    base:value ?? {};
    eval {
      left:cloneLeg(base.left);
      right:cloneLeg(base.right);
    };
  };

  cloneLeg:(leg)=> {
    eval if leg = null then null else leg + { effectorCoordinate:clonePoint(leg.effectorCoordinate) };
  };

  clonePoint:(value)=> if value = null then null else [value[0], value[1]];

  subtractPoints:(a, b)=> {
    aSafe:if a = null then [0,0] else a;
    bSafe:if b = null then [0,0] else b;
    eval [
      (if aSafe[0] = null then 0 else aSafe[0]) - (if bSafe[0] = null then 0 else bSafe[0]),
      (if aSafe[1] = null then 0 else aSafe[1]) - (if bSafe[1] = null then 0 else bSafe[1])
    ];
  };
}

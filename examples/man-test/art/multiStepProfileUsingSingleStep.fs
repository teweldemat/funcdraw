{
  stickman:package("@funcdraw/testlib").cartoon.stickman;
  singleStep:stickman.singleStepProfile;

  defaults:{ position:[0,10]; leftOffset:[-2,-11]; rightOffset:[2,-11] };

  multiStepProfileUsingSingleStep:(options)=> {
    input:options ?? {};
    anchorBase:if input.initialPosition = null then defaults.position else input.initialPosition;
    measurementsInput:input.initialMeasurements ?? {};
    displacement:if input.displacement = null then 0 else input.displacement;
    progress:clamp01(if input.progress = null then 0 else input.progress);
    direction:normalizeDirection(input.direction, "right");
    strideDirection:if displacement >= 0 then 1 else -1;

    defaultOffsets:{ left:defaults.leftOffset; right:defaults.rightOffset };
    initialLegs:measurementsInput.legs ?? {};
    initialLeftFoot:addPoints(anchorBase, resolveLegOffset(initialLegs.left, defaultOffsets.left));
    initialRightFoot:addPoints(anchorBase, resolveLegOffset(initialLegs.right, defaultOffsets.right));

    strideOverride:input.strideLength;
    strideLength:if strideOverride != null and strideOverride > 0 then strideOverride else resolveStrideLength(measurementsInput, defaultOffsets);
    spacing:math.max(0.000001, strideLength);
    overreach:math.max(spacing * 0.25, 0.75);
    passDistance:spacing + overreach;
    stepCount:math.max(1, math.ceil(math.abs(displacement) / passDistance));
    totalProgress:progress * stepCount;
    activeStepIndex:math.min(stepCount - 1, math.floor(totalProgress));
    activeStepPhase:totalProgress - activeStepIndex;

    movingSide:if strideDirection >= 0
      then if initialLeftFoot[0] <= initialRightFoot[0] then "left" else "right"
      else if initialLeftFoot[0] >= initialRightFoot[0] then "left" else "right";
    fixedSide:if movingSide = "left" then "right" else "left";

    mergedMeasurements:mergeFacing(measurementsInput, direction);
    initialState:{
      anchor:anchorBase;
      measurements:mergedMeasurements;
      leftFoot:initialLeftFoot;
      rightFoot:initialRightFoot;
      remainingDistance:math.abs(displacement);
      history:if input.debug = true then [] else null;
    };

    finalState:simulateWalk(0, initialState);

    eval {
      measurements:{} + finalState.measurements;
      position:if finalState.anchor = null then defaults.position else finalState.anchor;
      debug:if input.debug = true then finalState else null;
    };

    simulateWalk:(index, state)=> {
      eval if index >= stepCount then state else {
        stepFixedSide:if index % 2 = 0 then fixedSide else movingSide;
        stepMovingSide:if stepFixedSide = "left" then "right" else "left";
        isActive:index = activeStepIndex;
        stepProgress:if index < activeStepIndex then 1 else if isActive then activeStepPhase else 0;
        remainingSteps:math.max(1, stepCount - index);

        strideMagnitude:if remainingSteps = 1 then state.remainingDistance else {
          reserve:(remainingSteps - 1) * passDistance;
          allowed:math.max(passDistance, state.remainingDistance - reserve);
          eval math.min(allowed, state.remainingDistance);
        };

        movingStart:if stepMovingSide = "left" then state.leftFoot else state.rightFoot;
        movingTarget:[movingStart[0] + strideMagnitude * strideDirection, movingStart[1]];

        measurementsWithOffsets:applyLegOffsets(state.measurements, state.anchor, state.leftFoot, state.rightFoot, defaultOffsets);

        stepResult:singleStep({
          position:state.anchor;
          measurements:measurementsWithOffsets;
          handSwing:input.handSwing;
          movingSide:stepMovingSide;
          movingFeetTargetPoint:movingTarget;
          progress:stepProgress;
          disableStatic:true;
        });

        updatedMeasurements:stepResult.measurements;
        updatedAnchor:stepResult.position;
        updatedLeftFoot:addPoints(updatedAnchor, updatedMeasurements.legs.left.effectorCoordinate);
        updatedRightFoot:addPoints(updatedAnchor, updatedMeasurements.legs.right.effectorCoordinate);
        activeDebug:{
          stepIndex:index;
          stepProgress:stepProgress;
          movingSide:stepMovingSide;
          fixedSide:stepFixedSide;
          strideMagnitude:strideMagnitude;
          movingStart:movingStart;
          movingTarget:movingTarget;
          anchorBefore:state.anchor;
          anchorAfter:updatedAnchor;
          measurementsBefore:state.measurements;
          measurementsAfter:updatedMeasurements;
        };
        updatedState:{
          anchor:updatedAnchor;
          measurements:updatedMeasurements;
          leftFoot:updatedLeftFoot;
          rightFoot:updatedRightFoot;
          remainingDistance:math.max(0, state.remainingDistance - strideMagnitude);
          debugStep:if isActive then activeDebug else state.debugStep;
          history:if input.debug = true then (state.history ?? []) + [{
            index:index;
            anchorBefore:state.anchor;
            anchor:updatedAnchor;
            left:updatedLeftFoot;
            right:updatedRightFoot;
            movingSide:stepMovingSide;
            fixedSide:stepFixedSide;
            movingStart:movingStart;
            movingTarget:movingTarget;
            stride:strideMagnitude;
            progress:stepProgress;
            legOffsetsBefore:{
              left:measurementsWithOffsets.legs.left.effectorCoordinate;
              right:measurementsWithOffsets.legs.right.effectorCoordinate;
            };
            legOffsetsAfter:{
              left:updatedMeasurements.legs.left.effectorCoordinate;
              right:updatedMeasurements.legs.right.effectorCoordinate;
            };
            anchorCandidates:stepResult.anchorCandidates;
            step:stepResult.step;
            stepInput:{
              position:state.anchor;
              movingSide:stepMovingSide;
              movingFeetTargetPoint:movingTarget;
              progress:stepProgress;
              legs:measurementsWithOffsets.legs;
              torso:measurementsWithOffsets.torso;
              head:measurementsWithOffsets.head;
              hands:measurementsWithOffsets.hands;
            };
          }] else state.history;
        };

        eval if isActive then updatedState else simulateWalk(index + 1, updatedState);
      };
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
      torso:torso + { direction:resolvedDirection };
      head:head + { direction:resolvedDirection };
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
    leftPoint:if leftFoot = null then addPoints(anchorPoint, defaults.left) else leftFoot;
    rightPoint:if rightFoot = null then addPoints(anchorPoint, defaults.right) else rightFoot;
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
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "front" or textValue = "back" or textValue = "left" or textValue = "right" then textValue else defaultDir;
  };

  clamp01:(value)=> {
    num:if value = null then 0 else value;
    eval if num < 0 then 0 else if num > 1 then 1 else num;
  };

  addPoints:(a, b)=> [a[0] + b[0], a[1] + b[1]];
  subtractPoints:(a, b)=> [a[0] - b[0], a[1] - b[1]];

  eval multiStepProfileUsingSingleStep;
}

(optionsInput)=>
{
  defaults:{ position:[0,10]; depthDelta:-12; zoom:1; direction:"front" };
  fallbackOffsets:{ left:[-2,-11]; right:[2,-11] };

  options:helpers.normalizeInput(optionsInput ?? {}, {});
  anchorBase:helpers.normalizePoint(options.initialPosition, defaults.position);
  measurementsInput:helpers.normalizeInput(options.initialMeasurements, {});
  depthDelta:helpers.resolveNumber(options.depthDelta, defaults.depthDelta);
  progress:clamp01(helpers.resolveNumber(options.progress, 0));
  direction:normalizeDirection(options.direction, defaults.direction);
  zoomTarget:math.max(0, helpers.resolveNumber(options.zoom, defaults.zoom));
  stepper:if options.stepper != null then options.stepper else steperManZoom;

  defaultOffsets:resolveDefaultLegOffsets(measurementsInput, fallbackOffsets);
  strideDirection:if depthDelta >= 0 then 1 else -1;
  strideLength:resolveStrideLength(measurementsInput, defaultOffsets);
  spacing:math.max(0.000001, strideLength);
  overreach:math.max(spacing * 0.25, 0.75);
  passDistance:spacing + overreach;
  stepCount:math.max(1, math.ceil(math.abs(depthDelta) / passDistance));
  totalProgress:progress * stepCount;
  activeStepIndex:math.min(stepCount - 1, math.floor(totalProgress));
  activeStepPhase:totalProgress - activeStepIndex;

  mergedMeasurements:mergeFacing(measurementsInput, direction);
  initialLeftFoot:helpers.addOffset(anchorBase, resolveLegOffset(mergedMeasurements.legs?.left, defaultOffsets.left));
  initialRightFoot:helpers.addOffset(anchorBase, resolveLegOffset(mergedMeasurements.legs?.right, defaultOffsets.right));
  remainingDistance:math.abs(depthDelta);

  movingSideInitial:if strideDirection >= 0
    then if initialLeftFoot[1] >= initialRightFoot[1] then "left" else "right"
    else if initialLeftFoot[1] <= initialRightFoot[1] then "left" else "right";
  fixedSideInitial:if movingSideInitial = "left" then "right" else "left";

  initialState:{
    anchor:anchorBase;
    measurements:mergedMeasurements;
    leftFoot:initialLeftFoot;
    rightFoot:initialRightFoot;
    remaining:remainingDistance;
  };

  finalState:simulateWalk(0, initialState);

  eval {
    measurements:{} + finalState.measurements;
    position:helpers.normalizePoint(finalState.anchor, defaults.position);
  };

  simulateWalk:(index, state)=> {
    eval if index >= stepCount then state else {
      stepFixedSide:if index % 2 = 0 then fixedSideInitial else movingSideInitial;
      stepMovingSide:if stepFixedSide = "left" then "right" else "left";
      isActive:index = activeStepIndex;
      stepProgress:if index < activeStepIndex then 1 else if isActive then activeStepPhase else 0;
      remainingSteps:math.max(1, stepCount - index);
      fixedY:if stepFixedSide = "left" then state.leftFoot[1] else state.rightFoot[1];

      strideMagnitude:if remainingSteps = 1 then state.remaining else {
        reserve:(remainingSteps - 1) * passDistance;
        allowed:math.max(passDistance, state.remaining - reserve);
        eval math.min(allowed, state.remaining);
      };

      targetY:fixedY + strideMagnitude * strideDirection;
      measurementsWithOffsets:applyLegOffsets(state.measurements, state.anchor, state.leftFoot, state.rightFoot, defaultOffsets);
      zoomPhase:(index + stepProgress) / stepCount;
      currentZoom:1 + (zoomTarget - 1) * zoomPhase;

      stepResult:stepper({
        position:state.anchor;
        measurements:measurementsWithOffsets;
        movingSide:stepMovingSide;
        movingFootTargetY:targetY;
        zoom:currentZoom;
        progress:stepProgress;
      });

      resolvedMeasurements:helpers.normalizeInput(
        if stepResult?.measurements != null then stepResult.measurements else stepResult?.sequenceState?.measurements,
        measurementsWithOffsets
      );
      resolvedAnchor:helpers.normalizePoint(
        if stepResult?.position != null then stepResult.position else stepResult?.sequenceState?.position,
        state.anchor
      );

      updatedLeftFoot:helpers.addOffset(resolvedAnchor, resolveLegOffset(resolvedMeasurements.legs?.left, defaultOffsets.left));
      updatedRightFoot:helpers.addOffset(resolvedAnchor, resolveLegOffset(resolvedMeasurements.legs?.right, defaultOffsets.right));
      updatedRemaining:math.max(0, state.remaining - strideMagnitude);

      updatedState:{
        anchor:resolvedAnchor;
        measurements:resolvedMeasurements;
        leftFoot:updatedLeftFoot;
        rightFoot:updatedRightFoot;
        remaining:updatedRemaining;
      };

      eval if isActive then updatedState else simulateWalk(index + 1, updatedState);
    };
  };

  resolveLegOffset:(measurement, fallback)=> {
    legInput:helpers.normalizeInput(measurement ?? {}, {});
    eval helpers.normalizePoint(legInput.effectorCoordinate, fallback);
  };

  resolveDefaultLegOffsets:(measurements, fallback)=> {
    legs:helpers.normalizeInput((measurements ?? {}).legs, {});
    eval {
      left:helpers.normalizePoint(legs.left?.effectorCoordinate, fallback.left);
      right:helpers.normalizePoint(legs.right?.effectorCoordinate, fallback.right);
    };
  };

  resolveStrideLength:(measurements, defaults)=> {
    legs:helpers.normalizeInput((measurements ?? {}).legs, {});
    left:helpers.normalizeInput(legs.left, {});
    right:helpers.normalizeInput(legs.right, {});
    leftLength:math.max(0, helpers.resolveNumber(left.upperLength, 0) + helpers.resolveNumber(left.lowerLength, 0));
    rightLength:math.max(0, helpers.resolveNumber(right.upperLength, 0) + helpers.resolveNumber(right.lowerLength, 0));
    avgLength:(leftLength + rightLength) / (if leftLength > 0 and rightLength > 0 then 2 else if leftLength > 0 or rightLength > 0 then 1 else 0);
    eval if avgLength > 0 then math.max(1, avgLength * 0.35) else {
      fallbackSpan:math.max(
        math.abs(defaults.left[1]),
        math.abs(defaults.right[1]),
        math.abs(defaults.right[1] - defaults.left[1])
      );
      eval math.max(1, fallbackSpan * 0.35);
    };
  };

  applyLegOffsets:(measurements, anchor, leftFoot, rightFoot, defaults)=> {
    base:helpers.normalizeInput(measurements, {});
    legs:helpers.normalizeInput(base.legs, {});
    anchorPoint:helpers.normalizePoint(anchor, [0,0]);
    leftPoint:helpers.normalizePoint(leftFoot, helpers.addOffset(anchorPoint, defaults.left));
    rightPoint:helpers.normalizePoint(rightFoot, helpers.addOffset(anchorPoint, defaults.right));
    leftOffset:subtractPoints(leftPoint, anchorPoint);
    rightOffset:subtractPoints(rightPoint, anchorPoint);
    eval base + {
      legs:{
        left:helpers.normalizeInput(legs.left, {}) + { effectorCoordinate:leftOffset };
        right:helpers.normalizeInput(legs.right, {}) + { effectorCoordinate:rightOffset };
      };
    };
  };

  mergeFacing:(measurements, facing)=> {
    base:helpers.normalizeInput(measurements ?? {}, {});
    torso:helpers.normalizeInput(base.torso, {});
    head:helpers.normalizeInput(base.head, {});
    resolvedDirection:normalizeDirection(if torso.direction != null then torso.direction else head.direction, facing);
    eval base + { torso:{ direction:resolvedDirection }; head:{ direction:resolvedDirection } };
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

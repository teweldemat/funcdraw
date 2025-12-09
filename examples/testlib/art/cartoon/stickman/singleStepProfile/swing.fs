{
  resolveHandSwingOptions:(value)=> {
    options:value ?? {};
    enabled:if options.enabled = false then false else true;
    amplitude:math.max(0, options.amplitude ?? 1.4);
    lift:math.max(0, options.lift ?? 0.35);
    forwardOffset:options.forwardOffset ?? 0;
    phase:options.phase ?? 0;
    mode:normalizeSwingMode(options.mode);
    eval { enabled:enabled; amplitude:amplitude; lift:lift; forwardOffset:forwardOffset; phase:phase; mode:mode };
  };

  normalizeSwingMode:(value)=> {
    textValue:if value = null then "" else text.lower(format(value));
    eval if textValue = "mirror" then "mirror" else if textValue = "sine" then "sine" else "mirror";
  };

  resolveShoulderOffsets:(torsoMeasurements, context)=> {
    width:torsoMeasurements.width ?? context.defaultTorsoWidth;
    height:torsoMeasurements.height ?? context.defaultTorsoHeight;
    shoulderExtension:math.max(torsoMeasurements.shoulderExtension ?? context.defaultShoulderExtension ?? width * 0.15, 0);
    direction:context.normalizeDirectionValue(torsoMeasurements.direction, context.baseTorsoDirection);
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

  resolveShoulderOffsetsFromContext:(context)=> {
    resolved:resolveShoulderOffsets(context.torsoMeasurements ?? {}, context);
    eval {
      left:resolved.left ?? context.defaultShoulderOffsets.left ?? [0,0];
      right:resolved.right ?? context.defaultShoulderOffsets.right ?? [0,0];
    };
  };

  resolveDefaultHandReach:(side, shoulderOffsets, baseSkeleton, defaultHandOffsetsBySide, distanceBetweenPoints)=> {
    baseHands:baseSkeleton.hands;
    sideHands:if side = "left" then baseHands?.left else baseHands?.right;
    attachment:sideHands?.attachmentPoint;
    target:sideHands?.targetPoint;
    lengths:if sideHands = null or sideHands.lengths = null then {} else sideHands.lengths;
    upper:lengths.upper ?? null;
    lower:lengths.lower ?? null;
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

  resolveHandReachLength:(side, shoulderOffsets, handMeasurements, defaultHandOffsetsBySide, defaultHandReach, distanceBetweenPoints)=> {
    baseReach:if side = "left" then defaultHandReach.left else defaultHandReach.right;
    sideMeasurements:if handMeasurements = null or handMeasurements[side] = null then {} else handMeasurements[side];
    upper:sideMeasurements.upperLength ?? null;
    lower:sideMeasurements.lowerLength ?? null;
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

  resolveLegDepthScale:(legOffsets)=> {
    left:legOffsets?.left;
    right:legOffsets?.right;
    leftDepth:math.abs(left?!left[1] ?? 0);
    rightDepth:math.abs(right?!right[1] ?? 0);
    eval math.max(1, leftDepth, rightDepth);
  };

  resolveAverageY:(legOffsets)=> {
    left:legOffsets?.left;
    right:legOffsets?.right;
    sum:(left?!left[1] ?? 0) + (right?!right[1] ?? 0);
    count:(if left = null then 0 else 1) + (if right = null then 0 else 1);
    eval if count = 0 then 0 else sum / count;
  };

  resolveForwardSign:(direction)=> if direction = "left" then -1 else 1;

  applyHandSwing:(handMeasurements, context)=> {
    swing:context.swing;
    eval if swing.enabled = false then handMeasurements else {
      baseHands:handMeasurements ?? {};
      shoulderOffsets:resolveShoulderOffsetsFromContext(context);
      reachBySide:{
        left:resolveHandReachLength("left", shoulderOffsets, baseHands, context.defaultHandOffsetsBySide, context.defaultHandReach, context.distanceBetweenPoints);
        right:resolveHandReachLength("right", shoulderOffsets, baseHands, context.defaultHandOffsetsBySide, context.defaultHandReach, context.distanceBetweenPoints);
      };
      swingContext:context + { shoulderOffsets:shoulderOffsets; reachBySide:reachBySide };
      eval if swing.mode = "sine" then applySineHandSwing(baseHands, swingContext) else applyMirrorHandSwing(baseHands, swingContext);
    };
  };

  applySineHandSwing:(baseHands, context)=> {
    swing:context.swing;
    angle:math.pi * context.clamp01(context.progress) + swing.phase;
    swingSignal:math.cos(angle);
    liftSignal:math.sin(angle);
    forwardSign:resolveForwardSign(context.torsoDirection);
    amplitude:swing.amplitude * forwardSign;
    liftAmount:swing.lift;
    forwardOffset:swing.forwardOffset * forwardSign;
    shoulderOffsets:context.shoulderOffsets;
    reachBySide:context.reachBySide;

    applySide:(side)=> {
      fallback:if side = "left" then context.defaultHandOffsetsBySide.left else context.defaultHandOffsetsBySide.right;
      baseEffector:if baseHands[side]?.effectorCoordinate = null then fallback else baseHands[side].effectorCoordinate;
      shoulderOffset:if shoulderOffsets = null then fallback else if side = "left" then shoulderOffsets.left ?? fallback else shoulderOffsets.right ?? fallback;
      reachValue:reachBySide?! (if side = "left" then reachBySide.left else reachBySide.right);
      defaultReach:if side = "left" then context.defaultHandReach.left else context.defaultHandReach.right;
      reach:if reachValue != null then reachValue else if defaultReach != null then defaultReach else context.distanceBetweenPoints(baseEffector, shoulderOffset);
      isMoving:side = context.movingSide;
      horizontalSwing:(if isMoving then swingSignal else -swingSignal) * amplitude + forwardOffset;
      verticalSwing:(if isMoving then liftSignal else -liftSignal) * liftAmount;
      candidate:[horizontalSwing, baseEffector[1] + verticalSwing];
      targetEffector:context.scaleVectorToLength(candidate, shoulderOffset, reach);
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
    phaseSignal:math.cos(math.pi * context.clamp01(context.progress) + swing.phase);
    phaseWeight:0.9;
    legWeight:0.45;
    verticalPhaseWeight:0.3;
    verticalLegWeight:0.05;
    shoulderOffsets:context.shoulderOffsets;
    reachBySide:context.reachBySide;

    applySide:(side)=> {
      fallback:if side = "left" then context.defaultHandOffsetsBySide.left else context.defaultHandOffsetsBySide.right;
      baseEffector:if baseHands[side]?.effectorCoordinate = null then fallback else baseHands[side].effectorCoordinate;
      mirroredLeg:if side = "left" then legOffsets?.right else legOffsets?.left;
      shoulderOffset:if side = "left" then shoulderOffsets.left else shoulderOffsets.right;
      targetLength:reachBySide?! (if side = "left" then reachBySide.left else reachBySide.right);

      eval if mirroredLeg = null then (baseHands[side] ?? {}) + { effectorCoordinate:baseEffector } else {
        radius:math.max(0.000001, context.distanceBetweenPoints([0,0], baseEffector));
        normalizedHorizontal:context.clampSymmetric(mirroredLeg[0] / depthScale, 1);
        normalizedVertical:context.clampSymmetric((mirroredLeg[1] - averageY) / depthScale, 1);
        signedPhase:if side = context.movingSide then -phaseSignal else phaseSignal;
        horizontalInfluence:signedPhase * (phaseWeight + math.abs(normalizedHorizontal) * legWeight);
        verticalInfluence:signedPhase * verticalPhaseWeight + normalizedVertical * verticalLegWeight;
        horizontalSwing:horizontalInfluence * swing.amplitude * forwardSign + swing.forwardOffset * forwardSign;
        verticalSwing:verticalInfluence * swing.lift;
        candidateX:baseEffector[0] + horizontalSwing;
        candidateY:baseEffector[1] + verticalSwing;
        targetRadius:if targetLength = null then radius else targetLength;
        targetEffector:context.scaleVectorToLength([candidateX, candidateY], shoulderOffset, targetRadius);
        eval (baseHands[side] ?? {}) + { effectorCoordinate:targetEffector };
      };
    };

    eval {
      left:applySide("left");
      right:applySide("right");
    };
  };

  eval {
    resolveHandSwingOptions:resolveHandSwingOptions;
    resolveShoulderOffsetsFromContext:resolveShoulderOffsetsFromContext;
    resolveDefaultHandReach:resolveDefaultHandReach;
    resolveHandReachLength:resolveHandReachLength;
    applyHandSwing:applyHandSwing;
    resolveForwardSign:resolveForwardSign;
  };
}

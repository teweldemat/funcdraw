{
  defaultPalette:{
    torsoFill:"#1f2937";
    torsoStroke:"#cbd5f5";
    torsoStrokeWidth:0.6;
    headFill:"#fff7ed";
    headStroke:"#fdba74";
    headStrokeWidth:0.4;
    headGazeColor:"#ea580c";
    skinStroke:"#f97316";
    handWidth:0.8;
    legWidth:1.1;
    footStroke:"#f59e0b";
    footStrokeWidth:0.5;
    overlayHand:"#fb7185";
    overlayLeg:"#38bdf8";
  };

  staticMan:(optionsInput)=> {
    skeletonContext:skeleton.build(optionsInput ?? {});
    skeletonPose:skeletonContext.skeleton;
    normalizedOptions:helpers.normalizeInput(skeletonContext.normalizedOptions, {});
    paletteOverrides:if skeleton.normalizeInput = null then helpers.normalizeInput(normalizedOptions.palette, {}) else skeleton.normalizeInput(normalizedOptions.palette, {});
    palette:if skeleton.mergeDeep = null then helpers.mergeDeep(defaultPalette, paletteOverrides) else skeleton.mergeDeep(defaultPalette, paletteOverrides);
    skinStroke:if normalizedOptions.palette?.skinStroke != null then normalizedOptions.palette.skinStroke
      else if palette.skinStroke != null then palette.skinStroke
      else if palette.handStroke != null then palette.handStroke
      else if palette.legStroke != null then palette.legStroke
      else defaultPalette.skinStroke;
    handStroke:if normalizedOptions.palette?.handStroke != null then normalizedOptions.palette.handStroke else skinStroke;
    legStroke:if normalizedOptions.palette?.legStroke != null then normalizedOptions.palette.legStroke else skinStroke;
    footStyle:{ stroke:palette.footStroke; width:palette.footStrokeWidth };

    torsoResult:torso({
      centerBottomPoint:skeletonPose.torso.centerBottomPoint;
      width:skeletonPose.torso.width;
      height:skeletonPose.torso.height;
      shoulderExtension:skeletonPose.torso.shoulderExtension;
      direction:skeletonPose.torso.direction;
      handAttachmentPoints:skeletonPose.torso.handAttachmentPoints;
      legAttachmentPoints:skeletonPose.torso.legAttachmentPoints;
      headAttachmentPoint:skeletonPose.torso.headAttachmentPoint;
      fill:palette.torsoFill;
      stroke:palette.torsoStroke;
      strokeWidth:palette.torsoStrokeWidth;
    });

    headResult:head(skeletonPose.head.attachmentPoint, {
      verticalExtent:skeletonPose.head.verticalExtent;
      angle:skeletonPose.head.angle;
      direction:skeletonPose.head.direction;
      fill:palette.headFill;
      stroke:palette.headStroke;
      strokeWidth:palette.headStrokeWidth;
      gazeColor:palette.headGazeColor;
    });

    leftHand:hand({
      joints:skeletonPose.hands.left.joints;
      targetPoint:skeletonPose.hands.left.targetPoint;
      lengths:skeletonPose.hands.left.lengths;
      positiveBend:skeletonPose.hands.left.positiveBend;
      bendDirection:skeletonPose.hands.left.bendDirection;
      style:{ stroke:handStroke; width:palette.handWidth };
    });

    rightHand:hand({
      joints:skeletonPose.hands.right.joints;
      targetPoint:skeletonPose.hands.right.targetPoint;
      lengths:skeletonPose.hands.right.lengths;
      positiveBend:skeletonPose.hands.right.positiveBend;
      bendDirection:skeletonPose.hands.right.bendDirection;
      style:{ stroke:handStroke; width:palette.handWidth };
    });

    leftLeg:leg({
      joints:skeletonPose.legs.left.joints;
      targetPoint:skeletonPose.legs.left.targetPoint;
      lengths:skeletonPose.legs.left.lengths;
      positiveBend:skeletonPose.legs.left.positiveBend;
      bendDirection:skeletonPose.legs.left.bendDirection;
      style:{ stroke:legStroke; width:palette.legWidth };
    });

    rightLeg:leg({
      joints:skeletonPose.legs.right.joints;
      targetPoint:skeletonPose.legs.right.targetPoint;
      lengths:skeletonPose.legs.right.lengths;
      positiveBend:skeletonPose.legs.right.positiveBend;
      bendDirection:skeletonPose.legs.right.bendDirection;
      style:{ stroke:legStroke; width:palette.legWidth };
    });

    leftFootConfig:helpers.normalizeInput(skeletonPose.legs.left.foot, {});
    rightFootConfig:helpers.normalizeInput(skeletonPose.legs.right.foot, {});

    leftFoot:feet({
      anklePoint:skeletonPose.legs.left.reachTarget;
      side:"left";
      length:leftFootConfig.length;
      directionHint:leftFootConfig.direction;
      style:footStyle;
    });

    rightFoot:feet({
      anklePoint:skeletonPose.legs.right.reachTarget;
      side:"right";
      length:rightFootConfig.length;
      directionHint:rightFootConfig.direction;
      style:footStyle;
    });

    overlays:[
      { point:skeletonPose.hands.left.attachmentPoint; color:palette.overlayHand };
      { point:skeletonPose.hands.left.targetPoint; color:palette.overlayHand };
      { point:skeletonPose.hands.right.attachmentPoint; color:palette.overlayHand };
      { point:skeletonPose.hands.right.targetPoint; color:palette.overlayHand };
      { point:skeletonPose.legs.left.attachmentPoint; color:palette.overlayLeg };
      { point:skeletonPose.legs.left.targetPoint; color:palette.overlayLeg };
      { point:skeletonPose.legs.right.attachmentPoint; color:palette.overlayLeg };
      { point:skeletonPose.legs.right.targetPoint; color:palette.overlayLeg };
    ];

    leftArmGraphics:ensureArray(leftHand.graphics);
    rightArmGraphics:ensureArray(rightHand.graphics);
    leftLegGraphics:ensureArray(leftLeg.graphics) + ensureArray(leftFoot.graphics);
    rightLegGraphics:ensureArray(rightLeg.graphics) + ensureArray(rightFoot.graphics);
    centerTorsoGraphics:ensureArray(torsoResult.graphics);
    headGraphics:ensureArray(headResult.graphics);

    direction:if skeletonPose.torso.direction = null then "front" else text.lower(format(skeletonPose.torso.direction));
    graphics:if direction = "left" then rightLegGraphics + rightArmGraphics + centerTorsoGraphics + headGraphics + leftLegGraphics + leftArmGraphics
    else if direction = "right" then leftLegGraphics + leftArmGraphics + centerTorsoGraphics + headGraphics + rightLegGraphics + rightArmGraphics
    else if direction = "back" then leftArmGraphics + rightArmGraphics + leftLegGraphics + rightLegGraphics + centerTorsoGraphics + headGraphics
    else leftLegGraphics + rightLegGraphics + centerTorsoGraphics + headGraphics + leftArmGraphics + rightArmGraphics;

    eval {
      graphics:graphics;
      overlays:overlays;
      skeleton:skeletonPose;
    };
  };

  ensureArray:(value)=> if value = null then [] else value;

  eval staticMan;
}

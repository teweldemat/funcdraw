const defaultPalette = {
  torsoFill: "#1f2937",
  torsoStroke: "#cbd5f5",
  torsoStrokeWidth: 0.6,
  headFill: "#fff7ed",
  headStroke: "#fdba74",
  headStrokeWidth: 0.4,
  headGazeColor: "#ea580c",
  skinStroke: "#f97316",
  handWidth: 0.8,
  legWidth: 1.1,
  footStroke: "#f97316",
  footStrokeWidth: 0.5,
  overlayHand: "#fb7185",
  overlayLeg: "#38bdf8"
};

function resolveLayerPlan(direction = "front") {
  const normalized = typeof direction === "string" ? direction.toLowerCase() : "front";
  if (normalized === "left") {
    return ["rightLeg", "rightArm", "torso", "leftLeg", "leftArm"];
  }
  if (normalized === "right") {
    return ["leftLeg", "leftArm", "torso", "rightLeg", "rightArm"];
  }
  if (normalized === "back") {
    return ["leftArm", "rightArm", "leftLeg", "rightLeg", "torso"];
  }
  return ["leftLeg", "rightLeg", "torso", "leftArm", "rightArm"];
}

function stickMan(optionsInput = {}) {
  const skeletonContext = skeleton.build(optionsInput);
  const skeletonPose = skeletonContext.skeleton;
  const normalizedOptions = skeletonContext.normalizedOptions || {};
  const paletteOverrides = skeleton.normalizeInput
    ? skeleton.normalizeInput(normalizedOptions.palette, {})
    : normalizedOptions.palette || {};
  const palette = skeleton.mergeDeep
    ? skeleton.mergeDeep(defaultPalette, paletteOverrides)
    : { ...defaultPalette, ...paletteOverrides };
  const skinStroke =
    normalizedOptions.palette?.skinStroke ??
    palette.skinStroke ??
    palette.handStroke ??
    palette.legStroke ??
    defaultPalette.skinStroke;
  const handStroke = normalizedOptions.palette?.handStroke ?? skinStroke;
  const legStroke = normalizedOptions.palette?.legStroke ?? skinStroke;
  const footStyle = {
    stroke: palette.footStroke,
    width: palette.footStrokeWidth
  };

  const torsoResult = torso({
    centerBottomPoint: skeletonPose.torso.centerBottomPoint,
    width: skeletonPose.torso.width,
    height: skeletonPose.torso.height,
    shoulderExtension: skeletonPose.torso.shoulderExtension,
    direction: skeletonPose.torso.direction,
    fill: palette.torsoFill,
    stroke: palette.torsoStroke,
    strokeWidth: palette.torsoStrokeWidth
  });

  const headResult = head(skeletonPose.head.attachmentPoint, {
    verticalExtent: skeletonPose.head.verticalExtent,
    angle: skeletonPose.head.angle,
    direction: skeletonPose.head.direction,
    fill: palette.headFill,
    stroke: palette.headStroke,
    strokeWidth: palette.headStrokeWidth,
    gazeColor: palette.headGazeColor
  });

  const leftHand = hand({
    joints: skeletonPose.hands.left.joints,
    targetPoint: skeletonPose.hands.left.targetPoint,
    lengths: skeletonPose.hands.left.lengths,
    positiveBend: skeletonPose.hands.left.positiveBend,
    bendDirection: skeletonPose.hands.left.bendDirection,
    style: { stroke: handStroke, width: palette.handWidth }
  });

  const rightHand = hand({
    joints: skeletonPose.hands.right.joints,
    targetPoint: skeletonPose.hands.right.targetPoint,
    lengths: skeletonPose.hands.right.lengths,
    positiveBend: skeletonPose.hands.right.positiveBend,
    bendDirection: skeletonPose.hands.right.bendDirection,
    style: { stroke: handStroke, width: palette.handWidth }
  });

  if (typeof leg !== "function") {
    throw "leg export type: " + typeof leg;
  }

  const leftLeg = leg({
    joints: skeletonPose.legs.left.joints,
    targetPoint: skeletonPose.legs.left.targetPoint,
    lengths: skeletonPose.legs.left.lengths,
    positiveBend: skeletonPose.legs.left.positiveBend,
    bendDirection: skeletonPose.legs.left.bendDirection,
    style: { stroke: legStroke, width: palette.legWidth }
  });

  const rightLeg = leg({
    joints: skeletonPose.legs.right.joints,
    targetPoint: skeletonPose.legs.right.targetPoint,
    lengths: skeletonPose.legs.right.lengths,
    positiveBend: skeletonPose.legs.right.positiveBend,
    bendDirection: skeletonPose.legs.right.bendDirection,
    style: { stroke: legStroke, width: palette.legWidth }
  });

  const leftFootConfig = skeletonPose.legs.left.foot || {};
  const rightFootConfig = skeletonPose.legs.right.foot || {};

  const leftFoot = feet({
    anklePoint: skeletonPose.legs.left.reachTarget,
    side: "left",
    length: leftFootConfig.length,
    directionHint: leftFootConfig.direction,
    style: footStyle
  });

  const rightFoot = feet({
    anklePoint: skeletonPose.legs.right.reachTarget,
    side: "right",
    length: rightFootConfig.length,
    directionHint: rightFootConfig.direction,
    style: footStyle
  });

  const overlays = [
    { point: skeletonPose.hands.left.attachmentPoint, color: palette.overlayHand },
    { point: skeletonPose.hands.left.targetPoint, color: palette.overlayHand },
    { point: skeletonPose.hands.right.attachmentPoint, color: palette.overlayHand },
    { point: skeletonPose.hands.right.targetPoint, color: palette.overlayHand },
    { point: skeletonPose.legs.left.attachmentPoint, color: palette.overlayLeg },
    { point: skeletonPose.legs.left.targetPoint, color: palette.overlayLeg },
    { point: skeletonPose.legs.right.attachmentPoint, color: palette.overlayLeg },
    { point: skeletonPose.legs.right.targetPoint, color: palette.overlayLeg }
  ];

  const leftArmGraphics = [...leftHand.graphics];
  const rightArmGraphics = [...rightHand.graphics];
  const leftLegGraphics = [...leftLeg.graphics, ...leftFoot.graphics];
  const rightLegGraphics = [...rightLeg.graphics, ...rightFoot.graphics];
  const centerTorsoGraphics = [...torsoResult.graphics];
  const headGraphics = [...headResult.graphics];
  const layerPlan = resolveLayerPlan(skeletonPose.torso.direction);
  let headInserted = false;
  const graphics = [];
  for (const token of layerPlan) {
    if (token === "leftArm") {
      graphics.push(...leftArmGraphics);
    } else if (token === "rightArm") {
      graphics.push(...rightArmGraphics);
    } else if (token === "leftLeg") {
      graphics.push(...leftLegGraphics);
    } else if (token === "rightLeg") {
      graphics.push(...rightLegGraphics);
    } else if (token === "torso") {
      graphics.push(...centerTorsoGraphics);
      graphics.push(...headGraphics);
      headInserted = true;
    } else if (token === "head") {
      graphics.push(...headGraphics);
      headInserted = true;
    }
  }

  if (!headInserted) {
    graphics.push(...headGraphics);
  }

  return {
    graphics,
    overlays,
    skeleton: skeletonPose
  };
}
stickMan.skeleton = function stickManSkeleton(optionsInput = {}) {
  const context = skeleton.build(optionsInput);
  return context.skeleton;
};

return stickMan;

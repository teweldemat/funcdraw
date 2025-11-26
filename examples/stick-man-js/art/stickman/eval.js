const defaultPalette = {
  torsoFill: "#1f2937",
  torsoStroke: "#cbd5f5",
  torsoStrokeWidth: 0.6,
  headFill: "#fff7ed",
  headStroke: "#fdba74",
  headStrokeWidth: 0.4,
  headGazeColor: "#ea580c",
  handStroke: "#f97316",
  handWidth: 0.8,
  legStroke: "#0ea5e9",
  legWidth: 1.1,
  overlayHand: "#fb7185",
  overlayLeg: "#38bdf8"
};

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
  const handStroke = normalizedOptions.palette?.handStroke ?? palette.handStroke;
  const legStroke = normalizedOptions.palette?.legStroke ?? palette.legStroke;

  const torsoResult = torso({
    centerBottomPoint: skeletonPose.torso.centerBottomPoint,
    width: skeletonPose.torso.width,
    height: skeletonPose.torso.height,
    fill: palette.torsoFill,
    stroke: palette.torsoStroke,
    strokeWidth: palette.torsoStrokeWidth
  });

  const headResult = head(skeletonPose.head.attachmentPoint, {
    verticalExtent: skeletonPose.head.verticalExtent,
    angle: skeletonPose.head.angle,
    fill: palette.headFill,
    stroke: palette.headStroke,
    strokeWidth: palette.headStrokeWidth,
    gazeColor: palette.headGazeColor
  });

  const leftHand = hand({
    attachmentPoint: skeletonPose.hands.left.attachmentPoint,
    targetPoint: skeletonPose.hands.left.targetPoint,
    lengths: skeletonPose.hands.left.lengths,
    retractDistance: skeletonPose.hands.left.retractDistance,
    positiveBend: skeletonPose.hands.left.positiveBend,
    style: { stroke: handStroke, width: palette.handWidth }
  });

  const rightHand = hand({
    attachmentPoint: skeletonPose.hands.right.attachmentPoint,
    targetPoint: skeletonPose.hands.right.targetPoint,
    lengths: skeletonPose.hands.right.lengths,
    retractDistance: skeletonPose.hands.right.retractDistance,
    positiveBend: skeletonPose.hands.right.positiveBend,
    style: { stroke: handStroke, width: palette.handWidth }
  });

  const leftLeg = leg({
    attachmentPoint: skeletonPose.legs.left.attachmentPoint,
    targetPoint: skeletonPose.legs.left.targetPoint,
    lengths: skeletonPose.legs.left.lengths,
    positiveBend: skeletonPose.legs.left.positiveBend,
    style: { stroke: legStroke, width: palette.legWidth }
  });

  const rightLeg = leg({
    attachmentPoint: skeletonPose.legs.right.attachmentPoint,
    targetPoint: skeletonPose.legs.right.targetPoint,
    lengths: skeletonPose.legs.right.lengths,
    positiveBend: skeletonPose.legs.right.positiveBend,
    style: { stroke: legStroke, width: palette.legWidth }
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

  return {
    graphics: [
      ...torsoResult.graphics,
      ...headResult.graphics,
      ...leftHand.graphics,
      ...rightHand.graphics,
      ...leftLeg.graphics,
      ...rightLeg.graphics
    ],
    overlays,
    skeleton: skeletonPose
  };
}
stickMan.skeleton = function stickManSkeleton(optionsInput = {}) {
  const context = skeleton.build(optionsInput);
  return context.skeleton;
};

return stickMan;

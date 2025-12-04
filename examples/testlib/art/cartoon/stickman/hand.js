const defaultStyle = { stroke: "#f97316", width: 0.8 };
const { toPoint: toPointOrNull, normalizeInput } = helpers;

function extractJointPoints(config) {
  const joints = config.joints;
  if (!joints || typeof joints !== "object") {
    return null;
  }
  const attachment = toPointOrNull(joints.attachment ?? joints.shoulder);
  const hinge = toPointOrNull(joints.hinge ?? joints.elbow);
  const effector = toPointOrNull(joints.effector ?? joints.wrist);
  if (attachment && hinge && effector) {
    return {
      attachment,
      hinge,
      effector
    };
  }
  return null;
}

function renderFromJoints(style, joints) {
  const segments = [
    {
      type: "line",
      from: joints.attachment,
      to: joints.hinge,
      stroke: style.stroke,
      width: style.width
    },
    {
      type: "line",
      from: joints.hinge,
      to: joints.effector,
      stroke: style.stroke,
      width: style.width
    }
  ];

  return {
    graphics: segments
  };
}

function createHand(options) {
  const config = normalizeInput(options, {});
  const joints = extractJointPoints(config);
  if (!joints) {
    throw new Error(
      "hand model requires helpers.toPoint and skeleton-provided joints (hand.joints missing or helpers.toPoint undefined)"
    );
  }

  const style = {
    ...defaultStyle,
    ...(config.style && typeof config.style === "object" ? config.style : {})
  };

  return renderFromJoints(style, joints);
}

return createHand;

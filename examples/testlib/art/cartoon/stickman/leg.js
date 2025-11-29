const defaultStyle = { stroke: "#0ea5e9", width: 1.1 };
const helperCollection = typeof helpers === "object" ? helpers : null;
const toPointOrNull = helperCollection?.toPoint;
const normalizeInput = helperCollection?.normalizeInput;

function requireHelper(fn, name) {
  if (typeof fn !== "function") {
    throw new Error(`cartoon/helpers/${name}.js must export a function as helpers.${name}`);
  }
}

requireHelper(toPointOrNull, "toPoint");
requireHelper(normalizeInput, "normalizeInput");

function extractJointPoints(config) {
  if (typeof toPointOrNull !== "function") {
    throw new Error("leg model requires helpers.toPoint; ensure cartoon/helpers/toPoint.js is available");
  }
  const joints = config.joints;
  if (!joints || typeof joints !== "object") {
    return null;
  }
  const attachment = toPointOrNull(joints.attachment ?? joints.hip);
  const hinge = toPointOrNull(joints.hinge ?? joints.knee);
  const effector = toPointOrNull(joints.effector ?? joints.ankle);
  if (attachment && hinge && effector) {
    return { attachment, hinge, effector };
  }
  return null;
}

function renderFromJoints(style, joints) {
  const graphics = [
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
    graphics
  };
}

function createLeg(options) {
  const config = normalizeInput(options, {});
  const joints = extractJointPoints(config);
  if (!joints) {
    throw new Error("leg model now requires joints from the skeleton (leg.joints missing)");
  }

  const style = {
    stroke: config.style?.stroke ?? defaultStyle.stroke,
    width: config.style?.width ?? defaultStyle.width
  };

  return renderFromJoints(style, joints);
}

return createLeg;

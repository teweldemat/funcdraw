const fallbackSize = { width: 40, height: 30 };
const canvasData = (canvas && typeof canvas === "object") ? canvas : {};
const rawSize = (canvasData.size && typeof canvasData.size === "object") ? canvasData.size : {};
const rawWidth = rawSize.width ?? fallbackSize.width;
const rawHeight = rawSize.height ?? fallbackSize.height;
const widthValue = Number(rawWidth);
const heightValue = Number(rawHeight);
const canvasWidth = Number.isFinite(widthValue) && widthValue > 0 ? widthValue : fallbackSize.width;
const canvasHeight = Number.isFinite(heightValue) && heightValue > 0 ? heightValue : fallbackSize.height;

const centerX = canvasWidth / 2;
const centerY = canvasHeight / 2;

const view = { left: 0, bottom: 0, right: canvasWidth, top: canvasHeight };

const verticalLine = {
  type: "line",
  from: [centerX, 0],
  to: [centerX, canvasHeight],
  stroke: "#38bdf8",
  width: 1.5
};

const horizontalLine = {
  type: "line",
  from: [0, centerY],
  to: [canvasWidth, centerY],
  stroke: "#38bdf8",
  width: 1.5
};

const sizeLabel = {
  type: "text",
  text: `${canvasWidth} × ${canvasHeight}`,
  position: [2, canvasHeight - 100],
  fontSize: 40,
  color: "#ffffff"
};

return {
  view,
  graphics: [verticalLine, horizontalLine, sizeLabel]
};

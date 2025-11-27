const view = { left: 0, bottom: 0, right: 40, top: 40 };
const squareLib = package("@funcdraw/testlib-js");
if (typeof squareLib.square !== "function") {
  squareLib.square = fallbackSquare;
}
const squareFn = squareLib.square;

const background = {
  type: "rect",
  position: [0, 0],
  size: [40, 40],
  fill: "#020617",
  stroke: "#ffffff",
  width: 0.25
};

const aquaStyle = { fill: "#38bdf8", stroke: "#0f172a", width: 0.5 };
const magentaStyle = { fill: "#f472b6", stroke: "#881337", width: 0.6 };
const goldStyle = { fill: "#facc15", stroke: "#ca8a04", width: 0.75 };
const limeStyle = { fill: "#4ade80", stroke: "#166534", width: 0.5 };

return {
  view,
  graphics: [
    background,
    squareLib.square([10, 30], 18, aquaStyle),
    squareLib.square([30, 30], 8, magentaStyle),
    squareFn([10, 10], 12, goldStyle),
    squareFn([30, 10], 6, limeStyle)
  ]
};


function fallbackSquare(center = [0, 0], sideLength = 10, style = {}) {
  const options = typeof style === "object" && style !== null ? style : {};
  const centerPoint = Array.isArray(center) ? center : [0, 0];
  const centerX = Number(centerPoint[0]) || 0;
  const centerY = Number(centerPoint[1]) || 0;
  const side = Number.isFinite(sideLength) && sideLength > 0 ? sideLength : 10;
  const half = side / 2;
  return {
    type: "rect",
    position: [centerX - half, centerY - half],
    size: [side, side],
    fill: options.fill || "#38bdf8",
    stroke: options.stroke || "#0f172a",
    width: options.width || 0.5
  };
}

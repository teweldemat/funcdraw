const PI = 3.141592653589793;
const palette = ["#facc15", "#4ade80", "#38bdf8", "#fb7185", "#f472b6"];

function pickColor(index) {
  return palette[index % palette.length];
}

function createStarPoints(cx, cy, radius) {
  const points = [];
  for (let idx = 0; idx < 5; idx++) {
    const tipAngle = (idx * 72 * PI) / 180;
    const innerAngle = ((idx + 0.5) * 72 * PI) / 180;
    points.push([cx + Math.cos(tipAngle) * radius, cy + Math.sin(tipAngle) * radius]);
    points.push([cx + Math.cos(innerAngle) * radius * 0.5, cy + Math.sin(innerAngle) * radius * 0.5]);
  }
  return points;
}

function buildStar(centerX = 0, centerY = 0, size = 3, paletteIndex = 0) {
  const cx = centerX;
  const cy = centerY;
  const radius = Math.max(size, 0.1);
  const colorIdx = Math.max(0, Math.floor(paletteIndex));

  return {
    type: "polygon",
    points: createStarPoints(cx, cy, radius),
    fill: pickColor(colorIdx),
    stroke: "#0f172a",
    width: 0.4
  };
}

return buildStar;

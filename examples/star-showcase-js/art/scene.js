const view = [60, 40];
const starSeeds = [1, 7, 13, 21, 34];
const palette = ["#facc15", "#4ade80", "#38bdf8", "#fb7185", "#f472b6"];

const background = {
  type: "rect",
  position: [2, 0],
  size: [56, 40],
  fill: "#020617",
  stroke: "#0f172a",
  width: 0.5
};

const title = {
  type: "text",
  text: "Star Showcase",
  position: [4, 35],
  fontSize: 4.5,
  color: "#e2e8f0"
};

const caption = {
  type: "text",
  text: "FuncDraw JavaScript constellation",
  position: [4, 31.5],
  fontSize: 2.4,
  color: "#94a3b8"
};

function randomUnit(seed) {
  return (Math.sin(seed * 12.9898) + 1) / 2;
}

function randomFloat(seed, min, max) {
  return min + (max - min) * randomUnit(seed);
}

function selectColor(seed) {
  return palette[seed % palette.length];
}

function createStar(cx, cy, radius, color) {
  const centerX = cx ?? 0;
  const centerY = cy ?? 0;
  const size = radius ?? 3;
  const fill = color ?? "#facc15";
  const points = [];

  for (let idx = 0; idx < 5; idx += 1) {
    const angle = idx * 0.4 * Math.PI;
    const angle2 = (idx + 0.5) * 0.4 * Math.PI;
    points.push(
      [centerX + Math.cos(angle) * size, centerY + Math.sin(angle) * size],
      [centerX + Math.cos(angle2) * size * 0.5, centerY + Math.sin(angle2) * size * 0.5]
    );
  }

  return {
    type: "polygon",
    points,
    fill,
    stroke: "#0f172a",
    width: 0.35
  };
}

const starField = starSeeds.map((seed) =>
  createStar(
    randomFloat(seed, 6, 54),
    randomFloat(seed + 7, 6, 32),
    randomFloat(seed + 13, 2.6, 5.5),
    selectColor(seed)
  )
);

return {
  view,
  graphics: [background, title, caption, ...starField]
};

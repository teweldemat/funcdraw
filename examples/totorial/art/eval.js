const view = { left: 0, bottom: 0, right: 60, top: 40 };

const background = {
  type: "rect",
  position: [0, 0],
  size: [60, 40],
  fill: "#020617",
  stroke: "#0f172a",
  width: 0.5
};

const title = {
  type: "text",
  text: "FuncDraw Night Sky",
  position: [4, 32],
  fontSize: 4,
  color: "#e2e8f0"
};

const caption = {
  type: "text",
  text: "Functional art in orbit",
  position: [4, 28],
  fontSize: 2.4,
  color: "#94a3b8"
};

const stars = Array.from({ length: 5 }, (_, idx) => {
  const centerX = 8 + idx * 10;
  const centerY = idx % 2 === 0 ? 12 : 20;
  const size = 3.5 + idx * 0.6;
  return star(centerX, centerY, size, idx);
});

return {
  view,
  graphics: [background, title, caption, ...stars]
};

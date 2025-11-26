const view = { left: 0, bottom: 0, right: 40, top: 30 };
const stickManBuilder = stickman;

const groundY=0;
const manArse=8;
const figure = stickManBuilder({
  position: [20, groundY+manArse],
  measurements:{
      hands:{
          left:{
            upperLength:6,
            lowerLength:6,
            effectorCoordinate:[0,0],
            positiveBend:false,
          },
          right:{
            upperLength:6,
            lowerLength:6,
            effectorCoordinate:[0,0]
          }
        },
    legs: {
      left: {
        effectorCoordinate: [-4, -manArse],
        positiveBend: false
      },
      right: {
        upperLength: 4.5,
        lowerLength: 4,
        effectorCoordinate: [4, -manArse],
        positiveBend: true
      }
    }
      },
  palette: {
    overlayHand: "#fb7185",
    overlayLeg: "#38bdf8"
  }
});

return {
  view,
  graphics: [
    createGround(groundY),
    ...figure.graphics,
    ...renderOverlays(figure.overlays || [])
  ]
};

function createGround(y) {
  return {
    type: "line",
    from: [view.left, y],
    to: [view.right, y],
    stroke: "#64748b",
    width: 0.5
  };
}

function crossMarker(point, color, size = 0.6) {
  return [
    {
      type: "line",
      from: [point[0] - size, point[1] - size],
      to: [point[0] + size, point[1] + size],
      stroke: color,
      width: 0.35
    },
    {
      type: "line",
      from: [point[0] - size, point[1] + size],
      to: [point[0] + size, point[1] - size],
      stroke: color,
      width: 0.35
    }
  ];
}

function renderOverlays(points) {
  const graphics = [];
  for (const item of points) {
    if (!item || !Array.isArray(item.point) || typeof item.point[0] !== "number") {
      continue;
    }
    graphics.push(...crossMarker(item.point, item.color || "#ffffff"));
  }
  return graphics;
}

{
  sunCore: {
    type: 'circle',
    center: [0, 6],
    radius: 2.1,
    fill: '#fde047',
    stroke: '#f97316',
    width: 0.35
  };
  sunHalo: {
    type: 'circle',
    center: [0, 6],
    radius: 2.7,
    stroke: '#fcd34d',
    width: 0.2,
    fill: 'rgba(253, 224, 71, 0.32)'
  };
  hillShape: {
    type: 'polygon',
    points: [
      [-12, -4],
      [-7, -0.5],
      [-3, -1.2],
      [1, -0.2],
      [6, 1.1],
      [12, -3.8]
    ],
    fill: '#22c55e',
    stroke: '#166534',
    width: 0.35
  };
  sky: {
    type: 'rect',
    position: [-12, -12],
    size: [24, 24],
    fill: '#38bdf8'
  };
  ground: {
    type: 'rect',
    position: [-12, -4],
    size: [24, 8],
    fill: '#16a34a'
  };

  sun: [sunHalo, sunCore];
  hill: [hillShape];
  meadow: [sky, ground, sunHalo, sunCore, hillShape];

  return {
    sun,
    hill,
    meadow
  };
}

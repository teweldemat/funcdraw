{
  flipPoint: (point) => [point[0], -point[1]];
  mapPoints: (points) => points map flipPoint;

  background: {
    type: 'polygon',
    points: mapPoints([
      [-12, -3],
      [-8, -0.5],
      [-4, -1.2],
      [0, 0.3],
      [5, 1.6],
      [9, -0.4],
      [12, -2.8]
    ]),
    fill: '#22c55e',
    stroke: 'transparent'
  };
  foreground: {
    type: 'polygon',
    points: mapPoints([
      [-12, -4],
      [-7, -0.2],
      [-3, -1.3],
      [1, -0.1],
      [6, 1.0],
      [12, -3.6]
    ]),
    fill: '#16a34a',
    stroke: '#14532d',
    width: 0.35
  };
  eval [background, foreground];
}

{
  flipPoint: (point) => [point[0], -point[1]];
  rectPolygon: (position, size) => [
    flipPoint([position[0], position[1]]),
    flipPoint([position[0] + size[0], position[1]]),
    flipPoint([position[0] + size[0], position[1] + size[1]]),
    flipPoint([position[0], position[1] + size[1]])
  ];

  sky: {
    type: 'polygon',
    points: rectPolygon([-12, -12], [24, 24]),
    fill: '#38bdf8'
  };
  ground: {
    type: 'polygon',
    points: rectPolygon([-12, -4], [24, 8]),
    fill: '#166534'
  };
  // sun and hill are sibling expressions and can be referenced directly.
  eval [sky, ground, sun, hill];
}

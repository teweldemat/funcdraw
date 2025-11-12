{
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
    fill: '#166534'
  };
  // sun and hill are sibling expressions and can be referenced directly.
  eval [sky, ground, sun, hill];
}

{
  diagram: import("@funcdraw/diagram");

  source: diagram.box([-8, 0], [12, 6], {
    label: 'Energy Source',
    cornerRadius: 1.4,
    fill: '#e0f2fe',
    stroke: '#0369a1',
    strokeWidth: 0.3,
    labelSize: 1.2
  });

  target: diagram.box([8, 0], [12, 6], {
    label: 'Charging Hub',
    cornerRadius: 1.4,
    fill: '#dcfce7',
    stroke: '#15803d',
    strokeWidth: 0.3,
    labelSize: 1.2
  });

  eval [source.graphics, target.graphics];
}

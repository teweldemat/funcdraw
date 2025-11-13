{
  aurora: import('aurora-library');
  cartoon: import('@funcdraw/cat-cartoon');

  meadow: aurora.meadow([-20, -10], [40, 22], {
    groundHeight: 12,
    sun: { offsetX: 0.6, heightRatio: 0.7, radius: 3 },
    hill: { foregroundColor: '#15803d' }
  });

  plaza: {
    type: 'rect';
    position: [-18, -2.4];
    size: [36, 2.6];
    fill: '#e7e5e4';
    stroke: '#a8a29e';
    width: 0.18;
  };

  planterBed: {
    type: 'rect';
    position: [-18, -1.2];
    size: [36, 0.8];
    fill: '#14532d';
    stroke: 'transparent';
  };

  cottages: [
    cartoon.city.house('cottage', [-12, -1.0], 7.5).graphics,
    cartoon.city.house('cottage', [0, -1.1], 8.2).graphics,
    cartoon.city.house('cottage', [12, -1.0], 7.5).graphics
  ];

  accentTrees: [
    cartoon.landscape.tree('evergreen', [-17, -3.6], 6).graphics,
    cartoon.landscape.tree('evergreen', [17, -3.6], 6).graphics
  ];

  sculpture: {
    type: 'polygon';
    points: [[-1, -2.2], [0, -0.8], [1, -2.2]];
    fill: '#a855f7';
    stroke: '#6b21a8';
    width: 0.2;
  };

  fountainBasin: {
    type: 'ellipse';
    center: [0, -2.2];
    radiusX: 3.4;
    radiusY: 0.9;
    fill: '#bae6fd';
    stroke: '#0f172a';
    width: 0.2;
  };

  fountainWater: {
    type: 'line';
    from: [0, -2];
    to: [0, -0.6];
    stroke: '#38bdf8';
    width: 0.25;
  };

  eval [
    meadow,
    accentTrees,
    plaza,
    planterBed,
    fountainBasin,
    sculpture,
    fountainWater,
    cottages
  ];
}

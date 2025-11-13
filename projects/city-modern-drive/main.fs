{
  aurora: import("../../packages/aurora-library");
  cartoon: import("../../packages/cartoon");

  meadow: aurora.meadow([-20, -10], [40, 22], {
    groundHeight: 11,
    sun: { offsetX: 0.2, heightRatio: 0.65 },
    hill: { foregroundColor: '#15803d', backgroundColor: '#22c55e' }
  });

  driveway: {
    type: 'rect';
    position: [-18, -2.6];
    size: [36, 3];
    fill: '#d1d5db';
    stroke: '#94a3b8';
    width: 0.25;
  };

  glassGarden: {
    type: 'rect';
    position: [-18, -0.8];
    size: [12, 1.2];
    fill: '#064e3b';
    stroke: 'transparent';
  };

  modernHome: cartoon.city.house('modern', [9, -2], 12);
  coupe: cartoon.city.car([-6, -3], 11);

  artPanels:
    [
      {
        position: [-2, -1.5];
        size: [1.4, 3.4];
        color: '#fcd34d';
      },
      {
        position: [-0.2, -1.8];
        size: [1.4, 3.8];
        color: '#a855f7';
      }
    ] map (panel) => {
      eval {
        type: 'rect';
        position: panel.position;
        size: panel.size;
        fill: panel.color;
        stroke: '#0f172a';
        width: 0.18;
      };
    };

  eval [
    meadow,
    driveway,
    glassGarden,
    artPanels,
    coupe.graphics,
    modernHome.graphics
  ];
}

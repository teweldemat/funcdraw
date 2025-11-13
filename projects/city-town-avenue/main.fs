{
  aurora: import("../../packages/aurora-library");
  cartoon: import("../../packages/cartoon");

  meadow: aurora.meadow([-20, -10], [40, 22], {
    groundHeight: 11,
    sun: { offsetX: 0.4, heightRatio: 0.7 },
    hill: { backgroundColor: '#2dd4bf', foregroundColor: '#0f9f5c' }
  });

  boulevard: {
    type: 'rect';
    position: [-18, -3.4];
    size: [36, 3.2];
    fill: '#1f2937';
    stroke: '#0f172a';
    width: 0.2;
  };

  laneMarks:
    [-15, -9, -3, 3, 9, 15] map (x) => {
      eval {
        type: 'rect';
        position: [x - 1.2, -1.9];
        size: [2.4, 0.18];
        fill: '#fde047';
        stroke: 'transparent';
      };
    };

  parkStrip: {
    type: 'rect';
    position: [-18, -0.8];
    size: [36, 0.8];
    fill: '#065f46';
    stroke: 'transparent';
  };

  homes: [
    cartoon.city.house('townhome', [-12, -1.6], 6.4).graphics,
    cartoon.city.house('townhome', [-4, -1.9], 7.1).graphics,
    cartoon.city.house('townhome', [4, -1.9], 7.1).graphics,
    cartoon.city.house('townhome', [12, -1.6], 6.4).graphics
  ];

  streetCar: cartoon.city.car([0, -3.2], 12);

  eval [
    meadow,
    parkStrip,
    homes,
    boulevard,
    laneMarks,
    streetCar.graphics
  ];
}

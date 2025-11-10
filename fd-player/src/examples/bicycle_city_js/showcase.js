{
  const viewBounds = {
    minX: -60,
    maxX: 60,
    minY: -45,
    maxY: 45
  };

  const backgroundPanel = {
    type: 'rect',
    data: {
      position: [viewBounds.minX, viewBounds.minY],
      size: [viewBounds.maxX - viewBounds.minX, viewBounds.maxY - viewBounds.minY],
      fill: '#f1f5f9',
      stroke: '#cbd5f5',
      width: 0.2
    }
  };

  const groundLine = {
    type: 'line',
    data: {
      from: [viewBounds.minX, -12],
      to: [viewBounds.maxX, -12],
      stroke: '#94a3b8',
      width: 0.3
    }
  };

  const skySample = sky([-55, 15], 45, 24);
  const skylineSample = sky_line([-55, 12], 45, 8, 0);
  const groundSample = ground([-55, -30], 45, 18, 0);

  const birdSample = lib.bird([30, 26], 4.2, 0.25);
  const treeSample = lib.tree([-35, -12], 7.2);
  const houseSample = lib.house([-5, -12], 6.93, 'cabin');
  const wheelSample = lib.wheel([32, -12], 7, 2.6, 0.4);
  const gearSample = lib.gear([5, -32], 6, 16, 0.8);
  const bicycleSample = bicycle.bicycle([-15, -12], 9, 0.35, '#059669', '#34d399');
  const stickManSample = lib.stick_man(1.2, [10, -8], [8, -14], [12, -14], -0.05, [6, -1], [14, -2]);

  const scaleBar = {
    type: 'rect',
    data: {
      position: [-10, 0],
      size: [20, 1.2],
      fill: '#475569',
      stroke: '#0f172a',
      width: 0.2
    }
  };

  const verticalScaleBar = {
    type: 'rect',
    data: {
      position: [-0.6, -10],
      size: [1.2, 20],
      fill: '#475569',
      stroke: '#0f172a',
      width: 0.2
    }
  };

  return {
    graphics: [
      backgroundPanel,
      skySample,
      skylineSample,
      groundSample,
      groundLine,
      birdSample,
      treeSample,
      houseSample,
      bicycleSample.graphics,
      stickManSample,
      wheelSample,
      gearSample,
      scaleBar,
      verticalScaleBar
    ],
    viewBounds
  };
}

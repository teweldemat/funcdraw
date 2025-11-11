return (locationParam, boundsParam, flapParam) => {
  const base = locationParam ?? [0, 0];
  const bounds = boundsParam ?? { width: 20, height: 10 };
  const width = bounds.width ?? 20;
  const height = bounds.height ?? 10;
  const timeSeconds = flapParam ?? t ?? 0;

  const birdConfigs = [
    { offset: [0, 0], size: height * 0.08, flap: 0 },
    { offset: [width * 0.25, height * 0.2], size: height * 0.06, flap: 0.2 },
    { offset: [width * 0.6, -height * 0.15], size: height * 0.07, flap: 0.4 }
  ];

  return birdConfigs.map((config) => {
    const center = [base[0] + config.offset[0], base[1] + config.offset[1]];
    return lib.bird(center, config.size, timeSeconds + config.flap);
  });
};

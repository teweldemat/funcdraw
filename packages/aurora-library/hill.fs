(basePositionParam, sizeParam, optionsParam) => {
  flipPoint: (point) => [point[0], -point[1]];

  basePosition: basePositionParam ?? [-12, -4];
  size: sizeParam ?? [24, 8];
  options: optionsParam ?? {};

  backgroundColor: options.backgroundColor ?? '#22c55e';
  foregroundColor: options.foregroundColor ?? '#16a34a';
  strokeColor: options.strokeColor ?? '#14532d';
  strokeWidth: options.strokeWidth ?? 0.35;

  clamp01: (value) => math.max(0, math.min(1, value));

  transform: (point) => [
    basePosition[0] + clamp01(point[0]) * size[0],
    basePosition[1] + clamp01(point[1]) * size[1]
  ];

  farProfile: [
    [0.0, 0.25],
    [0.18, 0.55],
    [0.35, 0.42],
    [0.55, 0.7],
    [0.75, 0.6],
    [0.92, 0.38],
    [1.0, 0.22]
  ];

  nearProfile: [
    [0.0, 0.05],
    [0.2, 0.5],
    [0.4, 0.38],
    [0.58, 0.62],
    [0.82, 0.78],
    [1.0, 0.1]
  ];

  background: {
    type: 'polygon';
    points: farProfile map (point) => flipPoint(transform(point));
    fill: backgroundColor;
    stroke: 'transparent';
  };

  foreground: {
    type: 'polygon';
    points: nearProfile map (point) => flipPoint(transform(point));
    fill: foregroundColor;
    stroke: strokeColor;
    width: strokeWidth * (size[0] / 24);
  };

  eval [background, foreground];
}

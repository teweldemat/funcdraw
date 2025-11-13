(basePositionParam, sizeParam, optionsParam) => {
  flipPoint: (point) => [point[0], -point[1]];

  basePosition: basePositionParam ?? [-12, -12];
  size: sizeParam ?? [24, 24];
  options: optionsParam ?? {};

  totalWidth: size[0];
  totalHeight: size[1];

  defaultGround: totalHeight * (options.horizonRatio ?? 0.333);
  requestedGround: options.groundHeight ?? defaultGround;
  groundHeight: math.max(0.5, math.min(totalHeight - 0.5, requestedGround));
  skyHeight: totalHeight - groundHeight;

  skyColor: options.skyColor ?? '#38bdf8';
  groundColor: options.groundColor ?? '#166534';

  rectPolygon: (origin, rectSize) => [
    [origin[0], origin[1]],
    [origin[0] + rectSize[0], origin[1]],
    [origin[0] + rectSize[0], origin[1] + rectSize[1]],
    [origin[0], origin[1] + rectSize[1]]
  ] map flipPoint;

  groundOrigin: basePosition;
  groundSize: [totalWidth, groundHeight];
  skyOrigin: [basePosition[0], basePosition[1] + groundHeight];
  skySize: [totalWidth, skyHeight];

  sky: {
    type: 'polygon';
    points: rectPolygon(skyOrigin, skySize);
    fill: skyColor;
  };

  ground: {
    type: 'polygon';
    points: rectPolygon(groundOrigin, groundSize);
    fill: groundColor;
  };

  hillOptions: options.hill ?? {};
  hillHeight: hillOptions.height ?? (groundHeight * (hillOptions.heightRatio ?? 0.85));
  hillBasePosition: [basePosition[0], basePosition[1] + groundHeight];
  hillGraphics: hill(hillBasePosition, [totalWidth, hillHeight], hillOptions);

  sunOptions: options.sun ?? {};
  sunCenter: [
    basePosition[0] + totalWidth * (sunOptions.offsetX ?? 0.5),
    skyOrigin[1] + skyHeight * (sunOptions.heightRatio ?? 0.6)
  ];
  sunRadius: sunOptions.radius ?? (totalWidth * 0.08);
  sunGraphics: sun(sunCenter, sunRadius, sunOptions);

  eval [sky, ground, sunGraphics, hillGraphics];
}

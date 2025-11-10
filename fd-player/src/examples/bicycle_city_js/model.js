{
  const baseViewBounds = {
    minX: -60,
    maxX: 60,
    minY: -40,
    maxY: 40
  };

  const fsDiv = (numerator, denominator) =>
    Number.isInteger(numerator) && Number.isInteger(denominator)
      ? Math.trunc(numerator / denominator)
      : numerator / denominator;

  const baseViewWidth = baseViewBounds.maxX - baseViewBounds.minX;
  const baseViewHeight = baseViewBounds.maxY - baseViewBounds.minY;
  const baseCenterX = (baseViewBounds.minX + baseViewBounds.maxX) / 2;
  const baseCenterY = (baseViewBounds.minY + baseViewBounds.maxY) / 2;
  const baseSkyHeight = baseViewHeight / 2;

  const viewZoomStrength = 1.6;
  const nearZoomFactor = 0.55;
  const farZoomFactor = 1 + viewZoomStrength;

  const timeSeconds = t ?? 0;
  const skylineSpeed = 4;
  const groundSpeed = 8;

  const zoomStart = 5;
  const zoomEnd = 10;
  const zoomRange = zoomEnd - zoomStart;
  const zoomRaw = (timeSeconds - zoomStart) / zoomRange;
  const zoomProgress = zoomRaw < 0 ? 0 : zoomRaw > 1 ? 1 : zoomRaw;
  const viewZoomFactor = nearZoomFactor + zoomProgress * (farZoomFactor - nearZoomFactor);
  const skylineZoomStrength = 0.8;
  const skylineScaleFactor = 1 + zoomProgress * skylineZoomStrength;
  const birdZoomStrength = 1.2;

  const viewWidth = baseViewWidth * viewZoomFactor;
  const viewHeight = baseViewHeight * viewZoomFactor;
  const viewBounds = {
    minX: baseCenterX - viewWidth / 2,
    maxX: baseCenterX + viewWidth / 2,
    minY: baseCenterY - viewHeight / 2,
    maxY: baseCenterY + viewHeight / 2
  };

  const skyHeight = viewHeight / 2;
  const skyOrigin = [viewBounds.minX, viewBounds.minY + skyHeight];

  const wrapShift = (value, span) => value - Math.floor(value / span) * span;

  const skylineShift = -wrapShift(timeSeconds * skylineSpeed, skyHeight * 2);
  const groundShift = -wrapShift(timeSeconds * groundSpeed, viewWidth);
  const birdDriftSpeed = skylineSpeed * 0.5;
  const birdDriftShift = -wrapShift(timeSeconds * birdDriftSpeed, viewWidth);

  const skyLayer = sky(skyOrigin, viewWidth, skyHeight);

  const skylineHeightBase = baseSkyHeight * 0.35;
  const skylineHeight = skylineHeightBase * skylineScaleFactor;
  const skylineOriginY = skyOrigin[1];
  const skylineOrigin = [viewBounds.minX, skylineOriginY];
  const skylineLayer = sky_line(skylineOrigin, viewWidth, skylineHeight, skylineShift);

  const groundHeight = baseSkyHeight;
  const groundOrigin = [viewBounds.minX, skylineOriginY - groundHeight];
  const groundLayer = ground(groundOrigin, viewWidth, groundHeight, groundShift);

  const birdBoundsBaseWidth = baseViewWidth * 0.4;
  const birdBoundsBaseHeight = baseSkyHeight * 0.25;
  const birdBoundsWidth = birdBoundsBaseWidth * (1 + zoomProgress * birdZoomStrength);
  const birdBoundsHeight = birdBoundsBaseHeight * (1 + zoomProgress * (birdZoomStrength * 0.6));
  const birdZoomShift = viewWidth * 0.05 * zoomProgress;
  const birdOriginX = viewBounds.minX + viewWidth * 0.35 - birdZoomShift + birdDriftShift;
  const birdOriginY = skyOrigin[1] + skyHeight * 0.65;
  const flockBounds = { width: birdBoundsWidth, height: birdBoundsHeight };
  const flockOrigin = [birdOriginX, birdOriginY];
  const birdFlockLayer = bird_flock(flockOrigin, flockBounds, timeSeconds);

  const roadHeight = groundHeight * 0.35;
  const roadY = groundOrigin[1] + groundHeight * 0.35;
  const laneDividerY = roadY + roadHeight / 2;
  const leftLaneMidY = laneDividerY + roadHeight / 4;

  const wheelBase = 25;
  const wheelRadius = 9;
  const viewCenterX = (viewBounds.minX + viewBounds.maxX) / 2;
  const rearWheelX = viewCenterX - fsDiv(wheelBase, 2);
  const wheelCenterY = leftLaneMidY + wheelRadius;
  const wheelAngle = -(groundShift / wheelRadius);
  const primaryFrameColor = '#2563eb';
  const primaryAccentColor = '#93c5fd';
  const bicycleResult = bicycle.bicycle(
    [rearWheelX, wheelCenterY],
    wheelRadius,
    wheelAngle,
    primaryFrameColor,
    primaryAccentColor
  );
  const bicycleLayer = bicycleResult.graphics;
  const riderAttachments = bicycleResult.riderAttachments;

  const aheadBicycles = [
    { offset: 88, frameColor: '#0ea5e9', accentColor: '#7dd3fc' },
    { offset: 70, frameColor: '#f97316', accentColor: '#fed7aa' }
  ];
  const futureBicycleResults = aheadBicycles.map((config) =>
    bicycle.bicycle(
      [rearWheelX + config.offset, wheelCenterY],
      wheelRadius,
      wheelAngle,
      config.frameColor,
      config.accentColor
    )
  );
  const futureBicycleLayers = futureBicycleResults.map((result) => result.graphics);
  const futureBicycleGraphics = futureBicycleLayers.reduce((acc, layer) => acc.concat(layer), []);

  const riderScale = wheelRadius * 0.55;
  const riderLayer = riderAttachments
    ? lib.stick_man(
        riderScale,
        riderAttachments.seat,
        riderAttachments.leftFoot,
        riderAttachments.rightFoot,
        riderAttachments.torsoTilt,
        riderAttachments.leftHand,
        riderAttachments.rightHand
      )
    : [];

  const flatten = (value) => (Array.isArray(value) ? value : [value]);
  const graphics = [
    ...flatten(skyLayer),
    ...flatten(birdFlockLayer),
    ...flatten(skylineLayer),
    ...flatten(groundLayer),
    ...futureBicycleGraphics,
    ...flatten(bicycleLayer),
    ...flatten(riderLayer)
  ];

  return {
    graphics,
    viewBounds
  };
}

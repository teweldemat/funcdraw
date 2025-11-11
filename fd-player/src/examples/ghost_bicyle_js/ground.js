return (locationParam, widthParam, heightParam, shiftParam) => {
  const origin = locationParam ?? [0, 0];
  const rawShift = shiftParam ?? 0;
  const coverageWidth = widthParam ?? 120;
  const groundHeight = heightParam ?? 20;
  const tileWidth = groundHeight * 3;

  const normalizeShift = (value, span) => value - Math.floor(value / span) * span;

  const shift = normalizeShift(rawShift, tileWidth);
  const totalWidth = coverageWidth + tileWidth * 2;
  const repeatCount = Math.ceil(totalWidth / tileWidth) + 1;
  const startX = origin[0] + shift - tileWidth;
  const tilePositions = Array.from({ length: repeatCount }, (_, index) => startX + index * tileWidth);

  const buildGroundRect = (baseX) => ({
    type: 'rect',
    data: {
      position: [baseX, origin[1]],
      size: [tileWidth, groundHeight],
      fill: '#15803d',
      stroke: '#0f5132',
      width: 0.2
    }
  });

  const roadHeight = groundHeight * 0.35;
  const roadY = origin[1] + groundHeight * 0.35;

  const buildRoadRect = (baseX) => ({
    type: 'rect',
    data: {
      position: [baseX, roadY],
      size: [tileWidth, roadHeight],
      fill: '#1f2937',
      stroke: '#0f172a',
      width: 0.2
    }
  });

  const laneCenterY = roadY + roadHeight / 2;
  const dashCount = 8;
  const dashGapRatio = 0.5;
  const dashWidth = tileWidth / (dashCount * (1 + dashGapRatio));
  const dashGap = dashWidth * dashGapRatio;

  const buildLaneDashes = (baseX) =>
    Array.from({ length: dashCount }, (_, index) => {
      const dashX = baseX + index * (dashWidth + dashGap);
      return {
        type: 'rect',
        data: {
          position: [dashX, laneCenterY - 0.25],
          size: [dashWidth, 0.5],
          fill: '#f8fafc',
          stroke: '#f8fafc',
          width: 0.1
        }
      };
    });

  const groundRects = tilePositions.map(buildGroundRect);
  const roadRects = tilePositions.map(buildRoadRect);
  const laneSegments = tilePositions.map(buildLaneDashes).flat();

  return [...groundRects, ...roadRects, ...laneSegments];
};

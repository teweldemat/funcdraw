(configParam) => {
  defaultPoint: [0, 0];
  startSource: configParam.start ?? { point: defaultPoint };
  startPoint: startSource.point ?? defaultPoint;
  endSource: configParam.end ?? { point: startPoint };
  endPoint: endSource.point ?? startPoint;

  viaPoints: configParam.via ?? (configParam.points ?? []);
  pathPoints: ([startPoint] + viaPoints) + [endPoint];
  pointCount: length(pathPoints);

  safeIndex: (listValue, targetIndex) =>
    listValue[math.max(0, math.min(length(listValue) - 1, targetIndex))];

  terminalPoint: safeIndex(pathPoints, pointCount - 1);
  penultimatePoint:
    if (pointCount >= 2)
      then safeIndex(pathPoints, pointCount - 2)
      else startPoint;
  forwardPoint:
    if (pointCount >= 2)
      then safeIndex(pathPoints, 1)
      else [startPoint[0] + 1, startPoint[1]];

  styleParam: configParam.style ?? {};
  strokeColor: styleParam.stroke ?? '#1d4ed8';
  strokeWidth: styleParam.width ?? 0.35;
  dashPattern: styleParam.dash ?? null;

  arrowConfig: configParam.arrow ?? {};
  arrowEnabled: if (arrowConfig.enabled = false) then false else true;
  arrowStroke: arrowConfig.stroke ?? (arrowConfig.color ?? strokeColor);
  arrowFill: arrowConfig.fill ?? arrowStroke;
  arrowStrokeWidth: arrowConfig.width ?? (arrowConfig.strokeWidth ?? strokeWidth);
  arrowHeadInput: arrowConfig.headLength ?? (arrowConfig.length ?? 1.1);
  arrowBaseInput: arrowConfig.baseWidth ?? (arrowConfig.spread ?? arrowHeadInput * 0.6);

  startArrowFallback: { enabled: false };
  startArrowPrimary: configParam.startArrow;
  startArrowSecondary: configParam.arrowStart;
  startArrowParam:
    if (startArrowPrimary = null)
      then (
        if (startArrowSecondary = null)
          then startArrowFallback
          else startArrowSecondary
      )
      else startArrowPrimary;
  startArrowEnabled:
    if (startArrowParam.enabled = false)
      then false
      else true;
  startArrowStroke: if startArrowEnabled
      then startArrowParam.stroke ?? (startArrowParam.color ?? arrowStroke)
      else arrowStroke;
  startArrowFill: if startArrowEnabled
      then startArrowParam.fill ?? startArrowStroke
      else startArrowStroke;
  startArrowWidth: if startArrowEnabled
      then startArrowParam.width ?? (startArrowParam.strokeWidth ?? arrowStrokeWidth)
      else arrowStrokeWidth;
  startArrowHeadInput: if startArrowEnabled
      then startArrowParam.headLength ?? (startArrowParam.length ?? arrowHeadInput)
      else arrowHeadInput;
  startArrowBaseInput: if startArrowEnabled
      then startArrowParam.baseWidth ?? (startArrowParam.spread ?? startArrowHeadInput * 0.6)
      else arrowBaseInput;

  minX: pathPoints reduce (acc, point) => math.min(acc, point[0]) ~ startPoint[0];
  maxX: pathPoints reduce (acc, point) => math.max(acc, point[0]) ~ startPoint[0];
  minY: pathPoints reduce (acc, point) => math.min(acc, point[1]) ~ startPoint[1];
  maxY: pathPoints reduce (acc, point) => math.max(acc, point[1]) ~ startPoint[1];

  segmentCount: math.max(0, pointCount - 1);
  segmentGraphics:
    if (segmentCount <= 0)
      then []
      else range(0, segmentCount) map (segmentIndex) => {
        fromPoint: pathPoints[segmentIndex];
        toPoint: pathPoints[segmentIndex + 1];
        return {
          type: 'line';
          from: fromPoint;
          to: toPoint;
          stroke: strokeColor;
          width: strokeWidth;
          dash: dashPattern;
        };
      };

  makeArrowShape: (tipPoint, dirVector, headLengthInput, baseWidthInput, strokeValue, fillValue, widthValue) => {
    dirX: dirVector[0];
    dirY: dirVector[1];
    dirLength: math.sqrt(dirX * dirX + dirY * dirY);
    hasDirection: dirLength > 0.0001;
    normalizedDirection:
      if hasDirection
        then [dirX / dirLength, dirY / dirLength]
        else [1, 0];
    targetHead: math.max(0.1, headLengthInput);
    headLength:
      if hasDirection
        then math.min(targetHead, dirLength)
        else targetHead;
    baseWidth: math.max(0.1, baseWidthInput ?? headLength * 0.6);
    baseCenter: [
      tipPoint[0] - normalizedDirection[0] * headLength,
      tipPoint[1] - normalizedDirection[1] * headLength
    ];
    perp: [-normalizedDirection[1], normalizedDirection[0]];
    halfWidth: baseWidth / 2;
    leftPoint: [
      baseCenter[0] + perp[0] * halfWidth,
      baseCenter[1] + perp[1] * halfWidth
    ];
    rightPoint: [
      baseCenter[0] - perp[0] * halfWidth,
      baseCenter[1] - perp[1] * halfWidth
    ];
    return {
      type: 'polygon';
      points: [tipPoint, leftPoint, rightPoint];
      fill: fillValue;
      stroke: strokeValue;
      width: widthValue;
    };
  };

  endDirectionVector: [
    terminalPoint[0] - penultimatePoint[0],
    terminalPoint[1] - penultimatePoint[1]
  ];
  startDirectionVector: [
    startPoint[0] - forwardPoint[0],
    startPoint[1] - forwardPoint[1]
  ];

  endArrowGraphics:
    if arrowEnabled
      then [
        makeArrowShape(
          terminalPoint,
          endDirectionVector,
          arrowHeadInput,
          arrowBaseInput,
          arrowStroke,
          arrowFill,
          arrowStrokeWidth
        )
      ]
      else [];
  startArrowGraphics:
    if startArrowEnabled
      then [
        makeArrowShape(
          startPoint,
          startDirectionVector,
          startArrowHeadInput,
          startArrowBaseInput,
          startArrowStroke,
          startArrowFill,
          startArrowWidth
        )
      ]
      else [];

  eval {
    graphics: [segmentGraphics, startArrowGraphics, endArrowGraphics];
    width: math.abs(maxX - minX);
    height: math.abs(maxY - minY);
    above: maxY;
    below: -minY;
  };
}

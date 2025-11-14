(configParam) => {
  defaultPoint: [0, 0];
  startKvc: configParam.start ?? { point: defaultPoint };
  startPoint: startKvc.point ?? defaultPoint;
  endKvc: configParam.end ?? { point: startPoint };
  endPoint: endKvc.point ?? startPoint;

  styleParam: configParam.style ?? {};
  strokeColor: styleParam.stroke ?? (styleParam.color ?? '#f97316');
  fillColor: styleParam.fill ?? strokeColor;
  strokeWidth: styleParam.width ?? (styleParam.strokeWidth ?? 0.4);
  headLengthInput: styleParam.headLength ?? (styleParam.length ?? 1.4);
  baseWidthInput: styleParam.baseWidth ?? (styleParam.spread ?? headLengthInput * 0.7);

  dx: endPoint[0] - startPoint[0];
  dy: endPoint[1] - startPoint[1];
  distance: math.sqrt(dx * dx + dy * dy);
  hasDirection: distance > 0.0001;
  normalizedDirection:
    if hasDirection
      then [dx / distance, dy / distance]
      else [1, 0];

  targetLength: math.max(0.1, headLengthInput);
  headLength:
    if hasDirection
      then math.min(targetLength, distance)
      else targetLength;
  baseWidth: math.max(0.1, baseWidthInput);

  baseCenter: [
    endPoint[0] - normalizedDirection[0] * headLength,
    endPoint[1] - normalizedDirection[1] * headLength
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

  arrowShape: {
    type: 'polygon';
    points: [endPoint, leftPoint, rightPoint];
    fill: fillColor;
    stroke: strokeColor;
    width: strokeWidth;
  };

  eval {
    graphics: [arrowShape];
    width: math.abs(endPoint[0] - startPoint[0]);
    height: math.abs(endPoint[1] - startPoint[1]);
    above: math.max(startPoint[1], endPoint[1]);
    below: -math.min(startPoint[1], endPoint[1]);
  };
}

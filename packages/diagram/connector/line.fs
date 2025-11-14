(configParam) => {
  defaultPoint: [0, 0];
  startKvc: configParam.start ?? { point: defaultPoint };
  startPoint: startKvc.point ?? defaultPoint;
  endKvc: configParam.end ?? { point: startPoint };
  endPoint: endKvc.point ?? startPoint;

  styleParam: configParam.style ?? {};
  strokeColor: styleParam.stroke ?? '#f97316';
  strokeWidth: styleParam.width ?? 0.4;

  dx: endPoint[0] - startPoint[0];
  dy: endPoint[1] - startPoint[1];
  distance: math.sqrt(dx * dx + dy * dy);
  hasDirection: distance > 0.0001;
  normalizedDirection:
    if hasDirection
      then [dx / distance, dy / distance]
      else [1, 0];

  arrowConfig: configParam.arrow ?? {};
  arrowEnabled: if (arrowConfig.enabled = false) then false else true;
  arrowStroke: arrowConfig.stroke ?? (arrowConfig.color ?? strokeColor);
  arrowFill: arrowConfig.fill ?? arrowStroke;
  arrowStrokeWidth: arrowConfig.width ?? (arrowConfig.strokeWidth ?? strokeWidth);
  arrowHeadInput: arrowConfig.headLength ?? (arrowConfig.length ?? 1.4);
  arrowBaseInput: arrowConfig.baseWidth ?? (arrowConfig.spread ?? arrowHeadInput * 0.7);
  arrowTargetLength: math.max(0.1, arrowHeadInput);
  arrowHeadLength:
    if hasDirection
      then math.min(arrowTargetLength, distance)
      else arrowTargetLength;
  arrowBaseWidth: math.max(0.1, arrowBaseInput);

  arrowBaseCenter: [
    endPoint[0] - normalizedDirection[0] * arrowHeadLength,
    endPoint[1] - normalizedDirection[1] * arrowHeadLength
  ];
  arrowPerp: [-normalizedDirection[1], normalizedDirection[0]];
  arrowHalfWidth: arrowBaseWidth / 2;
  arrowLeft: [
    arrowBaseCenter[0] + arrowPerp[0] * arrowHalfWidth,
    arrowBaseCenter[1] + arrowPerp[1] * arrowHalfWidth
  ];
  arrowRight: [
    arrowBaseCenter[0] - arrowPerp[0] * arrowHalfWidth,
    arrowBaseCenter[1] - arrowPerp[1] * arrowHalfWidth
  ];

  arrowShape: {
    type: 'polygon';
    points: [endPoint, arrowLeft, arrowRight];
    fill: arrowFill;
    stroke: arrowStroke;
    width: arrowStrokeWidth;
  };

  arrowGraphics:
    if arrowEnabled
      then [arrowShape]
      else [];

  eval {
    graphics: [
      {
        type: 'line';
        from: startPoint;
        to: endPoint;
        stroke: strokeColor;
        width: strokeWidth;
      },
      arrowGraphics
    ];
    width: math.abs(endPoint[0] - startPoint[0]);
    height: math.abs(endPoint[1] - startPoint[1]);
    above: math.max(startPoint[1], endPoint[1]);
    below: -math.min(startPoint[1], endPoint[1]);
  };
}

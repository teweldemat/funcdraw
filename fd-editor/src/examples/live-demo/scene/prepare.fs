(configParam) => {
  config: configParam ?? {};
  viewData: view ?? {};

  stageTitle: config.stageTitle ?? 'Lets draw a line';
  lineStart: config.lineStart ?? { x: 0; y: 0 };
  lineEnd: config.lineEnd ?? { x: 10; y: 10 };
  maxExpressionLength: config.maxExpressionLength ?? 512;
  typeSpeed: config.typeSpeed ?? 16.8;
  pauseDuration: config.pauseDuration ?? 0.6;
  previewStroke: config.previewStroke ?? '#e2e8f0';
  previewWidth: config.previewWidth ?? 0.8;
  waitingText: config.waitingText ?? 'waiting for valid graphics';

  pointToString: lib.pointToString;
  lineStartText: pointToString(lineStart);
  lineEndText: pointToString(lineEnd);
  evalPrefixText: 'eval ';
  linePrefixText: "{ type: 'line'; from: " + lineStartText + "; to: ";
  lineSuffixText: '; }';
  literalLineText: linePrefixText + lineEndText + lineSuffixText;
  variableLineText: linePrefixText + '[x,y]' + lineSuffixText;
  cursorResetDuration: 0.8;

  stageResult: lib.stage ?? {};
  infoContainerDefault: { position: [0, 0]; size: [0, 0]; };
  infoContainer: stageResult.infoContainerDimensions ?? infoContainerDefault;
  previewContainer: stageResult.previewContainerDimensions ?? infoContainerDefault;
  entryContainer: stageResult.entryContainerDimensions ?? infoContainerDefault;
  viewMinY: viewData.bottom ?? -40;
  viewMaxY: viewData.top ?? 40;
  viewHeightValue: viewMaxY - viewMinY;
  stageGraphics: stageResult.stageGraphic ?? [];

  infoPosition: infoContainer.position ?? [0, 0];
  infoSize: infoContainer.size ?? [0, 0];
  infoTextLeft: infoPosition[0] + infoSize[0] * 0.04;
  infoTitleY: infoPosition[1] + infoSize[1] * 0.7;
  infoTitle: {
    type: 'text';
    position: [infoTextLeft, infoTitleY];
    text: stageTitle;
    color: '#e2e8f0';
    fontSize: viewHeightValue * 0.05;
    align: 'left';
  };

  previewPosition: previewContainer.position ?? [0, 0];
  previewSize: previewContainer.size ?? [0, 0];
  previewCenterPoint: {
    x: previewPosition[0] + previewSize[0] / 2;
    y: previewPosition[1] + previewSize[1] / 2;
  };
  maxExtent: math.max(
    math.abs(lineStart.x ?? 0),
    math.abs(lineStart.y ?? 0),
    math.abs(lineEnd.x ?? 0),
    math.abs(lineEnd.y ?? 0)
  );
  safeExtent: if (maxExtent <= 0) then 1 else maxExtent;
  previewScale: (previewSize[0] / 4) / safeExtent;
  previewFrom: lib.previewPoint(lineStart, previewCenterPoint, previewScale);
  previewTo: lib.previewPoint(lineEnd, previewCenterPoint, previewScale);
  previewLineGraphic: {
    type: 'line';
    from: [previewFrom.x ?? previewCenterPoint.x, previewFrom.y ?? previewCenterPoint.y];
    to: [previewTo.x ?? previewCenterPoint.x, previewTo.y ?? previewCenterPoint.y];
    stroke: previewStroke;
    width: previewWidth;
  };

  previewCenterX: previewCenterPoint.x;
  previewCenterY: previewCenterPoint.y;
  waitingMessage: {
    type: 'text';
    position: [previewCenterX, previewCenterY];
    text: waitingText;
    color: '#64748b';
    fontSize: viewHeightValue * 0.035;
    align: 'center';
  };

  entryPosition: entryContainer.position ?? [0, 0];
  entrySize: entryContainer.size ?? [0, 0];
  entryPaddingX: entrySize[0] * 0.04;
  entryPaddingTop: entrySize[1] * 0.035;
  entryPaddingBottom: entrySize[1] * 0.12;
  entryInputSize: [
    entrySize[0] - entryPaddingX * 2,
    entrySize[1] - entryPaddingTop - entryPaddingBottom
  ];
  entryInputPosition: [
    entryPosition[0] + entryPaddingX,
    entryPosition[1] + entryPaddingBottom
  ];

  eval {
    stageTitle;
    lineStart;
    lineEnd;
    maxExpressionLength;
    typeSpeed;
    pauseDuration;
    previewStroke;
    previewWidth;
    waitingText;
    lineStartText;
    literalLineText;
    variableLineText;
    linePrefixText;
    lineSuffixText;
    lineEndText;
    evalPrefixText;
    cursorResetDuration;
    stageGraphics;
    infoLayer: [infoTitle];
    viewHeightValue;
    previewLineGraphic;
    previewCenterPoint;
    previewScale;
    waitingMessage;
    entryInputPosition;
    entryInputSize;
    stageResult;
  };
}

sceneGenerator: (timeOffsetParam) => {
  prep: prepare ?? {};

  clamp: (valueParam, minParam, maxParam) => {
    value: valueParam ?? 0;
    minValue: minParam ?? 0;
    maxValue: maxParam ?? 1;
    eval if (value < minValue)
      then minValue
      else if (value > maxValue)
        then maxValue
        else value;
  };

  stageTitle: prep.stageTitle ?? 'Lets draw a line';
  typeSpeed: prep.typeSpeed ?? 0;
  pauseDuration: prep.pauseDuration ?? 0;
  maxExpressionLength: prep.maxExpressionLength ?? 512;
  cursorResetDuration: prep.cursorResetDuration ?? 0.8;
  evalPrefixText: prep.evalPrefixText ?? 'eval ';
  literalLineText: prep.literalLineText ?? '';
  variableLineText: prep.variableLineText ?? '';
  linePrefixText: prep.linePrefixText ?? '';
  lineSuffixText: prep.lineSuffixText ?? '; }';
  lineEndText: prep.lineEndText ?? '[10,10]';
  lineEndLength: length(lineEndText);
  linePrefixLength: length(linePrefixText);
  evalPrefixLength: length(evalPrefixText);
  newlineChar: '\n';
  shiftLines: 1;
  tetaLabelText: 'teta:';
  tetaInitialValueText: '0.1';
  tetaFirstValueText: 'math.pi';
  tetaSecondValueText: 'math.pi/2';
  tetaTailText: ';' + newlineChar;
  tetaLine: tetaLabelText + tetaInitialValueText + tetaTailText;
  tetaValueCursorIndex: length(tetaLabelText);
  tetaInitialValueLength: length(tetaInitialValueText);
  tetaFirstValueLength: length(tetaFirstValueText);
  tetaSecondValueLength: length(tetaSecondValueText);
  insertBreaks:
    range(0, shiftLines) reduce (acc, _) => acc + [{ kind: 'type'; text: newlineChar; speed: typeSpeed; }] ~ [];
  shiftBlockText:
    range(0, shiftLines) reduce (acc, _) => acc + newlineChar ~ '';
  radiusLine: 'r:10;' + newlineChar;
  xLine: 'x:r*math.cos(teta);' + newlineChar;
  yLine: 'y:r*math.sin(teta);';
  definitionSteps: [
    { kind: 'type'; text: tetaLine; speed: typeSpeed; },
    { kind: 'type'; text: radiusLine; speed: typeSpeed; },
    { kind: 'type'; text: xLine; speed: typeSpeed; },
    { kind: 'type'; text: yLine; speed: typeSpeed; }
  ];
  definitionsBlock: tetaLine + radiusLine + xLine + yLine;
  definitionsLength: length(definitionsBlock);
  shiftBlockLength: length(shiftBlockText);
  lineLiteralStartIndexBase: definitionsLength + shiftBlockLength;
  lineLiteralStartIndex:
    clamp(lineLiteralStartIndexBase, 0, definitionsLength + shiftBlockLength + length(literalLineText) + evalPrefixLength);
  lineEndCursorIndexBase: definitionsLength + shiftBlockLength + linePrefixLength;
  lineEndCursorIndex:
    clamp(lineEndCursorIndexBase, 0, definitionsLength + shiftBlockLength + length(literalLineText));

  scriptPrefix: [
    { kind: 'type'; text: literalLineText; speed: typeSpeed; markReady: 1; },
    { kind: 'wait'; duration: pauseDuration; },
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; }
  ];
  lineParamSteps: [
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; },
    { kind: 'moveCursor'; position: lineEndCursorIndex; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: lineEndLength; speed: typeSpeed; },
    { kind: 'type'; text: '[x,y]'; speed: typeSpeed; },
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; }
  ];
  addEvalPrefixSteps: [
    { kind: 'moveCursor'; position: lineLiteralStartIndex; duration: cursorResetDuration; },
    { kind: 'type'; text: evalPrefixText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; },
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; }
  ];
  changeTetaToPiSteps: [
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; },
    { kind: 'moveCursor'; position: tetaValueCursorIndex; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: tetaInitialValueLength; speed: typeSpeed; },
    { kind: 'type'; text: tetaFirstValueText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];
  changeTetaToHalfPiSteps: [
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; },
    { kind: 'moveCursor'; position: tetaValueCursorIndex; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: tetaFirstValueLength; speed: typeSpeed; },
    { kind: 'type'; text: tetaSecondValueText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];
  changeTetaSteps: changeTetaToPiSteps + changeTetaToHalfPiSteps;
  scriptSteps:
    scriptPrefix
    + insertBreaks
    + [{ kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; }]
    + definitionSteps
    + lineParamSteps
    + addEvalPrefixSteps
    + changeTetaSteps;

  stepDuration: (stepParam) => {
    step: stepParam ?? {};
    kind: step.kind ?? 'wait';
    eval if (kind = 'type')
      then {
        textValue: step.text ?? '';
        speedValue: math.max(step.speed ?? 0, 0);
        charCount: length(textValue);
        eval if (speedValue <= 0)
          then 0
          else charCount / speedValue;
      }
      else if (kind = 'delete')
        then {
          countValue: math.max(step.count ?? 0, 0);
          speedValue: math.max(step.speed ?? 0, 0);
          eval if (speedValue <= 0)
            then 0
            else countValue / speedValue;
        }
        else math.max(step.duration ?? 0, 0);
  };
  scriptDuration:
    scriptSteps reduce (acc, step) => acc + stepDuration(step) ~ 0;
  holdDuration: 5;
  sceneDurationRaw: scriptDuration + holdDuration;
  sceneDuration: if (sceneDurationRaw < holdDuration) then holdDuration else sceneDurationRaw;

  sceneTimeRaw: timeOffsetParam ?? 0;
  sceneTimePositive: if (sceneTimeRaw < 0) then 0 else sceneTimeRaw;
  sceneTime: clamp(sceneTimePositive, 0, sceneDuration);
  scriptPlaybackTime:
    if (sceneTime <= scriptDuration)
      then sceneTime
      else scriptDuration;
  timeOffset: scriptPlaybackTime;
  scriptState: lib.textScript(scriptSteps, timeOffset);
  typedTextValue: scriptState.text ?? '';
  cursorIndex: scriptState.cursorIndex ?? length(typedTextValue);
  literalLineReadyFlag: scriptState.readyFlag ?? 0;
  totalTypedChars: length(typedTextValue);
  tailExpectation: shiftBlockText + evalPrefixText + variableLineText;
  tailLength: length(tailExpectation);
  tailStartRaw: totalTypedChars - tailLength;
  tailStart: clamp(tailStartRaw, 0, totalTypedChars);
  tailAvailableLength: totalTypedChars - tailStart;
  tailLengthClamped: clamp(tailLength, 0, tailAvailableLength);
  tailSample: substring(typedTextValue, tailStart, tailLengthClamped);
  variableLineReady:
    if (tailLengthClamped = tailLength)
      then if (tailSample = tailExpectation) then 1 else 0
      else 0;
  hasEvalPrefix:
    if (totalTypedChars >= evalPrefixLength)
      then if (substring(typedTextValue, 0, evalPrefixLength) = evalPrefixText) then 1 else 0
      else 0;
  previewReadyFlag: variableLineReady;
  limitedChars: clamp(totalTypedChars, 0, maxExpressionLength);
  visibleTextValue: substring(typedTextValue, 0, limitedChars);
  visibleCursorIndex: clamp(cursorIndex, 0, limitedChars);
  tetaSampleStart: clamp(tetaValueCursorIndex, 0, totalTypedChars);
  tetaValueSampleHalfPi: substring(typedTextValue, tetaSampleStart, tetaSecondValueLength);
  tetaValueSamplePi: substring(typedTextValue, tetaSampleStart, tetaFirstValueLength);
  tetaMatchesHalfPi: if (tetaValueSampleHalfPi = tetaSecondValueText) then 1 else 0;
  tetaMatchesPi: if (tetaValueSamplePi = tetaFirstValueText) then 1 else 0;
  tetaValue:
    if (tetaMatchesHalfPi = 1)
      then math.pi / 2
      else if (tetaMatchesPi = 1)
        then math.pi
        else 0.1;

  stageGraphics: prep.stageGraphics ?? [];
  infoLayerBase: prep.infoLayer ?? [];
  stageResultData: prep.stageResult ?? {};
  infoContainerDefault: { position: [0, 0]; size: [0, 0]; };
  infoContainer: stageResultData.infoContainerDimensions ?? infoContainerDefault;
  infoPosition: infoContainer.position ?? [0, 0];
  infoSize: infoContainer.size ?? [0, 0];
  infoTextLeft: infoPosition[0] + infoSize[0] * 0.04;
  infoTitleY: infoPosition[1] + infoSize[1] * 0.7;
  subtitleOffset: infoSize[1] * 0.2;
  infoSubtitleY: infoTitleY - subtitleOffset;
  subtitleGraphic: {
    type: 'text';
    position: [infoTextLeft, infoSubtitleY];
    text: 'Lets paramterize the expression';
    color: '#94a3b8';
    fontSize: prep.viewHeightValue * 0.035;
    align: 'left';
  };
  parameterSubtitleThreshold: length(literalLineText) + shiftBlockLength + length(tetaLabelText);
  parameterSubtitleFlag: if (totalTypedChars > parameterSubtitleThreshold) then 1 else 0;
  infoLayer:
    if (parameterSubtitleFlag = 1)
      then infoLayerBase + [subtitleGraphic]
      else infoLayerBase;
  lineStartPoint: prep.lineStart ?? { x: 0; y: 0 };
  lineEndPoint: prep.lineEnd ?? { x: 10; y: 10 };
  radiusValue: 10;
  computedLineEnd: {
    x: radiusValue * math.cos(tetaValue);
    y: radiusValue * math.sin(tetaValue);
  };
  previewLineEndDefault: lineEndPoint;
  previewLineEnd:
    if (variableLineReady)
      then computedLineEnd
      else previewLineEndDefault;
  previewCenterPoint: prep.previewCenterPoint ?? { x: 0; y: 0 };
  previewScale: prep.previewScale ?? 1;
  previewFromPoint: lib.previewPoint(lineStartPoint, previewCenterPoint, previewScale);
  previewToPoint: lib.previewPoint(previewLineEnd, previewCenterPoint, previewScale);
  previewLineGraphic: {
    type: 'line';
    from: [previewFromPoint.x ?? previewCenterPoint.x, previewFromPoint.y ?? previewCenterPoint.y];
    to: [previewToPoint.x ?? previewCenterPoint.x, previewToPoint.y ?? previewCenterPoint.y];
    stroke: prep.previewStroke ?? '#e2e8f0';
    width: prep.previewWidth ?? 0.8;
  };
  previewLiteralToPoint: lib.previewPoint(previewLineEndDefault, previewCenterPoint, previewScale);
  previewLiteralGraphic: {
    type: 'line';
    from: [previewFromPoint.x ?? previewCenterPoint.x, previewFromPoint.y ?? previewCenterPoint.y];
    to: [previewLiteralToPoint.x ?? previewCenterPoint.x, previewLiteralToPoint.y ?? previewCenterPoint.y];
    stroke: prep.previewStroke ?? '#e2e8f0';
    width: prep.previewWidth ?? 0.8;
  };
  previewActiveGraphic:
    if (variableLineReady = 1)
      then previewLineGraphic
      else previewLiteralGraphic;
  waitingMessageDefault: {
    type: 'text';
    position: [0, 0];
    text: 'waiting...';
    color: '#64748b';
    fontSize: 10;
    align: 'center';
  };
  waitingMessage: prep.waitingMessage ?? waitingMessageDefault;
  previewReadyFlagCombined: math.min(1, variableLineReady + literalLineReadyFlag);
  previewReadyLayer:
    range(0, previewReadyFlagCombined) reduce (acc, _) => acc + [previewActiveGraphic] ~ [];
  previewWaitingLayer:
    range(0, 1 - previewReadyFlagCombined) reduce (acc, _) => acc + [waitingMessage] ~ [];
  previewContent: previewReadyLayer + previewWaitingLayer;

  entryInputPosition: prep.entryInputPosition ?? [0, 0];
  entryInputSize: prep.entryInputSize ?? [0, 0];
  entryGraphics: lib.textEntry(entryInputPosition, entryInputSize, visibleTextValue, visibleCursorIndex);

  graphics: stageGraphics + infoLayer + previewContent + entryGraphics;

  eval {
    title: stageTitle;
    graphics;
    duration: sceneDuration;
  };
};

eval sceneGenerator;

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

  stageTitle: prep.stageTitle ?? 'Lets now draw 10 lines';
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

  rangePrefixText: 'range(0,10) map (i)=>{';
  rangePrefixLineText: rangePrefixText + newlineChar;
  rangePrefixLength: length(rangePrefixLineText);
  closingBraceText: newlineChar + '}';
  closingBraceLength: length(closingBraceText);

  tetaLabelText: 'teta:';
  tetaInitialValueText: 'math.pi/2';
  tetaLoopValueText: '2*i*math.pi/10';
  tetaTailText: ';' + newlineChar;
  tetaInitialLineText: tetaLabelText + tetaInitialValueText + tetaTailText;
  tetaValueCursorIndex: length(tetaLabelText);
  tetaInitialValueLength: length(tetaInitialValueText);
  tetaLoopValueLength: length(tetaLoopValueText);

  radiusLine: 'r:10;' + newlineChar;
  xLine: 'x:r*math.cos(teta);' + newlineChar;
  yLine: 'y:r*math.sin(teta);';
  definitionSteps: [
    { kind: 'type'; text: tetaInitialLineText; speed: typeSpeed; },
    { kind: 'type'; text: radiusLine; speed: typeSpeed; },
    { kind: 'type'; text: xLine; speed: typeSpeed; },
    { kind: 'type'; text: yLine; speed: typeSpeed; }
  ];
  definitionsBlock: tetaInitialLineText + radiusLine + xLine + yLine;
  definitionsLength: length(definitionsBlock);
  shiftBlockLength: length(shiftBlockText);

  lineEndCursorIndexBase: definitionsLength + shiftBlockLength + evalPrefixLength + linePrefixLength;
  lineEndCursorIndex:
    clamp(lineEndCursorIndexBase, 0, definitionsLength + shiftBlockLength + evalPrefixLength + length(literalLineText));

  lineEditSteps: [
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; },
    { kind: 'moveCursor'; position: lineEndCursorIndex; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: lineEndLength; speed: typeSpeed; },
    { kind: 'type'; text: '[x,y]'; speed: typeSpeed; },
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; }
  ];

  insertBreaks:
    range(0, shiftLines) reduce (acc, _) => acc + [{ kind: 'type'; text: newlineChar; speed: typeSpeed; }] ~ [];
  shiftBlockText:
    range(0, shiftLines) reduce (acc, _) => acc + newlineChar ~ '';

  scriptPrefix: [
    { kind: 'type'; text: evalPrefixText; speed: typeSpeed; },
    { kind: 'type'; text: literalLineText; speed: typeSpeed; markReady: 1; },
    { kind: 'wait'; duration: pauseDuration; },
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; }
  ];

  addRangePrefixSteps: [
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; },
    { kind: 'type'; text: rangePrefixLineText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  tetaLineStartIndex: rangePrefixLength;
  loopTetaCursorIndex: tetaLineStartIndex + tetaValueCursorIndex;
  updateTetaForLoopSteps: [
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; },
    { kind: 'moveCursor'; position: loopTetaCursorIndex; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: tetaInitialValueLength; speed: typeSpeed; },
    { kind: 'type'; text: tetaLoopValueText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  addClosingBraceSteps: [
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; },
    { kind: 'type'; text: closingBraceText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  baseScriptSteps:
    scriptPrefix
    + insertBreaks
    + [{ kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; }]
    + definitionSteps
    + lineEditSteps;
  scriptSteps:
    baseScriptSteps
    + addRangePrefixSteps
    + updateTetaForLoopSteps
    + addClosingBraceSteps;

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
        else if (kind = 'moveCursor' or kind = 'wait')
          then math.max(step.duration ?? 0, 0)
          else 0;
  };
  baseScriptDuration:
    baseScriptSteps reduce (acc, step) => acc + stepDuration(step) ~ 0;
  fullScriptDuration:
    scriptSteps reduce (acc, step) => acc + stepDuration(step) ~ 0;
  visibleScriptDurationRaw: fullScriptDuration - baseScriptDuration;
  visibleScriptDuration:
    if (visibleScriptDurationRaw < 0)
      then 0
      else visibleScriptDurationRaw;
  holdDuration: 5;
  sceneDuration: visibleScriptDuration + holdDuration;

  timeOffsetRaw: timeOffsetParam ?? 0;
  sceneTimePositive: if (timeOffsetRaw < 0) then 0 else timeOffsetRaw;
  sceneTime: clamp(sceneTimePositive, 0, sceneDuration);
  scriptPlaybackTime:
    if (sceneTime <= visibleScriptDuration)
      then sceneTime
      else visibleScriptDuration;
  timeOffset: baseScriptDuration + scriptPlaybackTime;
  scriptState: lib.textScript(scriptSteps, timeOffset);
  typedTextValue: scriptState.text ?? '';
  cursorIndex: scriptState.cursorIndex ?? length(typedTextValue);
  totalTypedChars: length(typedTextValue);

  limitedChars: clamp(totalTypedChars, 0, maxExpressionLength);
  visibleTextValue: substring(typedTextValue, 0, limitedChars);
  visibleCursorIndex: clamp(cursorIndex, 0, limitedChars);

  tailExpectation: shiftBlockText + evalPrefixText + variableLineText;
  tailWithClosingExpectation: tailExpectation + closingBraceText;
  tailLength: length(tailExpectation);
  tailWithClosingLength: length(tailWithClosingExpectation);

  tailSampleStart: clamp(totalTypedChars - tailLength, 0, totalTypedChars);
  tailSampleLength: clamp(tailLength, 0, totalTypedChars - tailSampleStart);
  tailSample: substring(typedTextValue, tailSampleStart, tailSampleLength);

  tailWithClosingStart: clamp(totalTypedChars - tailWithClosingLength, 0, totalTypedChars);
  tailWithClosingSampleLength: clamp(tailWithClosingLength, 0, totalTypedChars - tailWithClosingStart);
  tailWithClosingSample: substring(typedTextValue, tailWithClosingStart, tailWithClosingSampleLength);

  variableLineReady:
    if (tailSampleLength = tailLength)
      then if (tailSample = tailExpectation)
        then 1
        else if (tailWithClosingSampleLength = tailWithClosingLength)
          then if (tailWithClosingSample = tailWithClosingExpectation) then 1 else 0
          else 0
      else 0;

  hasRangePrefix:
    if (totalTypedChars >= rangePrefixLength)
      then if (substring(typedTextValue, 0, rangePrefixLength) = rangePrefixLineText) then 1 else 0
      else 0;

  closingSampleStart: clamp(totalTypedChars - closingBraceLength, 0, totalTypedChars);
  closingSampleLength: clamp(closingBraceLength, 0, totalTypedChars - closingSampleStart);
  hasClosingBrace:
    if (closingSampleLength = closingBraceLength)
      then if (substring(typedTextValue, closingSampleStart, closingSampleLength) = closingBraceText) then 1 else 0
      else 0;

  rangeWrapperReady:
    if (hasRangePrefix = 1 and hasClosingBrace = 1) then 1 else 0;

  tetaSamplePrefixOffset: if (hasRangePrefix = 1) then rangePrefixLength else 0;
  tetaSampleStart: clamp(tetaSamplePrefixOffset + tetaValueCursorIndex, 0, totalTypedChars);
  tetaSampleLengthInitial: clamp(tetaInitialValueLength, 0, totalTypedChars - tetaSampleStart);
  tetaSampleInitial: substring(typedTextValue, tetaSampleStart, tetaSampleLengthInitial);
  tetaSampleLengthLoop: clamp(tetaLoopValueLength, 0, totalTypedChars - tetaSampleStart);
  tetaSampleLoop: substring(typedTextValue, tetaSampleStart, tetaSampleLengthLoop);

  tetaMatchesInitial: if (tetaSampleLengthInitial = tetaInitialValueLength and tetaSampleInitial = tetaInitialValueText) then 1 else 0;
  tetaMatchesLoop: if (tetaSampleLengthLoop = tetaLoopValueLength and tetaSampleLoop = tetaLoopValueText) then 1 else 0;

  singleLineTetaValue:
    if (tetaMatchesInitial = 1)
      then math.pi / 2
      else if (tetaMatchesLoop = 1)
        then math.pi / 2
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
  walaSubtitle: {
    type: 'text';
    position: [infoTextLeft, infoSubtitleY];
    text: 'Wala';
    color: '#fbbf24';
    fontSize: prep.viewHeightValue * 0.04;
    align: 'left';
  };
  infoLayer:
    if (rangeWrapperReady = 1)
      then infoLayerBase + [walaSubtitle]
      else infoLayerBase;
  lineStartPoint: prep.lineStart ?? { x: 0; y: 0 };
  lineEndPoint: prep.lineEnd ?? { x: 10; y: 10 };
  radiusValue: 10;

  computedLineEndSingle: {
    x: radiusValue * math.cos(singleLineTetaValue);
    y: radiusValue * math.sin(singleLineTetaValue);
  };
  previewLineEndSingle:
    if (variableLineReady)
      then computedLineEndSingle
      else lineEndPoint;

  previewCenterPoint: prep.previewCenterPoint ?? { x: 0; y: 0 };
  previewScale: prep.previewScale ?? 1;
  previewFromPoint: lib.previewPoint(lineStartPoint, previewCenterPoint, previewScale);
  previewToPointSingle: lib.previewPoint(previewLineEndSingle, previewCenterPoint, previewScale);
  basePreviewStroke: prep.previewStroke ?? '#e2e8f0';
  basePreviewWidth: prep.previewWidth ?? 0.8;

  previewLineGraphic: {
    type: 'line';
    from: [previewFromPoint.x ?? previewCenterPoint.x, previewFromPoint.y ?? previewCenterPoint.y];
    to: [previewToPointSingle.x ?? previewCenterPoint.x, previewToPointSingle.y ?? previewCenterPoint.y];
    stroke: basePreviewStroke;
    width: basePreviewWidth;
  };

  multiLineCount: 10;
  multiLineState:
    range(0, multiLineCount) reduce (acc, indexValue) => {
      angle: (2 * indexValue * math.pi) / multiLineCount;
      loopEnd: {
        x: radiusValue * math.cos(angle);
        y: radiusValue * math.sin(angle);
      };
      previewLoopEnd: lib.previewPoint(loopEnd, previewCenterPoint, previewScale);
      multiLineGraphic: {
        type: 'line';
        from: [previewFromPoint.x ?? previewCenterPoint.x, previewFromPoint.y ?? previewCenterPoint.y];
        to: [previewLoopEnd.x ?? previewCenterPoint.x, previewLoopEnd.y ?? previewCenterPoint.y];
        stroke: basePreviewStroke;
        width: basePreviewWidth;
      };
      eval {
        lines: acc.lines + [multiLineGraphic];
      };
    } ~ { lines: [] };
  multiLineGraphics: multiLineState.lines ?? [];

  previewLines:
    if (rangeWrapperReady = 1)
      then multiLineGraphics
      else [previewLineGraphic];

  waitingMessageDefault: {
    type: 'text';
    position: [0, 0];
    text: 'waiting...';
    color: '#64748b';
    fontSize: 10;
    align: 'center';
  };
  waitingMessage: prep.waitingMessage ?? waitingMessageDefault;

  previewReadyLayer:
    range(0, variableLineReady) reduce (acc, _) => acc + previewLines ~ [];
  previewWaitingLayer:
    range(0, 1 - variableLineReady) reduce (acc, _) => acc + [waitingMessage] ~ [];
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

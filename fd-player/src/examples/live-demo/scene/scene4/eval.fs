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

  stageTitle: prep.stageTitle ?? 'Lets make it spin, because why not?';
  typeSpeed: prep.typeSpeed ?? 0;
  pauseDuration: prep.pauseDuration ?? 0;
  maxExpressionLength: prep.maxExpressionLength ?? 512;
  cursorResetDuration: prep.cursorResetDuration ?? 0.8;
  newlineChar: '\n';

  rLineText: 'r:10;' + newlineChar;
  spinLineText: 'spin:t*math.pi/4;' + newlineChar;
  sticksLineText: 'sticks: range(0,10) map (i)=>{' + newlineChar;
  tetaBaseLineText: 'teta:2*i*math.pi/10;' + newlineChar;
  tetaSpinLineText: 'teta:2*i*math.pi/10 + spin;' + newlineChar;
  xLineText: 'x:r*math.cos(teta);' + newlineChar;
  yLineText: 'y:r*math.sin(teta);' + newlineChar;
  evalLineText: "eval { type: 'line'; from: [0,0]; to: [x,y]; }" + newlineChar;
  closingLineText: '};' + newlineChar;
  circleLineText: "ri: { type: 'circle'; center: [0,0]; radius: r; };" + newlineChar;
  evalArrayLineText: 'eval [sticks, ri];' + newlineChar;

  baseExpressionText:
    rLineText
    + sticksLineText
    + tetaBaseLineText
    + xLineText
    + yLineText
    + evalLineText
    + closingLineText
    + circleLineText
    + evalArrayLineText;

  scriptPrefix: [
    { kind: 'type'; text: baseExpressionText; speed: typeSpeed; markReady: 1; },
    { kind: 'wait'; duration: pauseDuration; }
  ];
  baseScriptSteps: scriptPrefix;

  spinInsertIndex: length(rLineText);
  insertSpinSteps: [
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; },
    { kind: 'moveCursor'; position: spinInsertIndex; duration: cursorResetDuration; },
    { kind: 'type'; text: spinLineText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  sticksLineLength: length(sticksLineText);
  tetaLineStartAfterSpin: spinInsertIndex + length(spinLineText) + sticksLineLength;
  updateTetaSteps: [
    { kind: 'moveCursor'; position: tetaLineStartAfterSpin; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: length(tetaBaseLineText); speed: typeSpeed; },
    { kind: 'type'; text: tetaSpinLineText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  scriptSteps:
    baseScriptSteps
    + insertSpinSteps
    + updateTetaSteps;

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

  sceneTimeRaw: timeOffsetParam ?? 0;
  sceneTimePositive: if (sceneTimeRaw < 0) then 0 else sceneTimeRaw;
  sceneTime: clamp(sceneTimePositive, 0, sceneDuration);
  scriptPlaybackTime:
    if (sceneTime <= visibleScriptDuration)
      then sceneTime
      else visibleScriptDuration;
  timeOffset: baseScriptDuration + scriptPlaybackTime;
  scriptState: lib.textScript(scriptSteps, timeOffset);
  typedTextValue: scriptState.text ?? '';
  cursorIndex: scriptState.cursorIndex ?? length(typedTextValue);
  literalReadyFlagRaw: scriptState.readyFlag ?? 0;
  literalReadyFlag:
    if (timeOffset >= baseScriptDuration)
      then 1
      else literalReadyFlagRaw;
  totalTypedChars: length(typedTextValue);

  limitedChars: clamp(totalTypedChars, 0, maxExpressionLength);
  visibleTextValue: substring(typedTextValue, 0, limitedChars);
  visibleCursorIndex: clamp(cursorIndex, 0, limitedChars);

  spinLineLength: length(spinLineText);
  spinSampleAvailable: math.max(totalTypedChars - spinInsertIndex, 0);
  spinSampleLength: clamp(spinLineLength, 0, spinSampleAvailable);
  spinSample:
    if (spinSampleLength > 0)
      then substring(typedTextValue, spinInsertIndex, spinSampleLength)
      else '';
  spinLineReadyFlag:
    if (spinSampleLength = spinLineLength and spinSample = spinLineText)
      then 1
      else 0;

  tetaSpinLineLength: length(tetaSpinLineText);
  tetaSampleAvailable: math.max(totalTypedChars - tetaLineStartAfterSpin, 0);
  tetaSampleLength: clamp(tetaSpinLineLength, 0, tetaSampleAvailable);
  tetaSample:
    if (tetaSampleLength > 0)
      then substring(typedTextValue, tetaLineStartAfterSpin, tetaSampleLength)
      else '';
  tetaLineReadyFlag:
    if (tetaSampleLength = tetaSpinLineLength and tetaSample = tetaSpinLineText)
      then 1
      else 0;

  finalReadyFlag:
    if (spinLineReadyFlag = 1 and tetaLineReadyFlag = 1)
      then 1
      else 0;

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
  spinSubtitle: {
    type: 'text';
    position: [infoTextLeft, infoSubtitleY];
    text: 'Why not spin it?';
    color: '#fbbf24';
    fontSize: prep.viewHeightValue * 0.04;
    align: 'left';
  };
  infoLayer:
    if (finalReadyFlag = 1)
      then infoLayerBase + [spinSubtitle]
      else infoLayerBase;

  lineStartPoint: prep.lineStart ?? { x: 0; y: 0 };
  radiusValue: 10;
  multiLineCount: 10;
  previewCenterPoint: prep.previewCenterPoint ?? { x: 0; y: 0 };
  previewScale: prep.previewScale ?? 1;
  previewFromPoint: lib.previewPoint(lineStartPoint, previewCenterPoint, previewScale);
  basePreviewStroke: prep.previewStroke ?? '#e2e8f0';
  basePreviewWidth: prep.previewWidth ?? 0.8;

  spinSpeed: math.pi / 4;
  spinAngleRaw: spinSpeed * sceneTimePositive;
  activeSpinAngle: spinAngleRaw * finalReadyFlag;

  multiLineGraphics:
    range(0, multiLineCount) map (indexValue) => {
      baseAngle: (2 * indexValue * math.pi) / multiLineCount;
      angle: baseAngle + activeSpinAngle;
      loopEnd: {
        x: radiusValue * math.cos(angle);
        y: radiusValue * math.sin(angle);
      };
      previewLoopEnd: lib.previewPoint(loopEnd, previewCenterPoint, previewScale);
      eval {
        type: 'line';
        from: [previewFromPoint.x ?? previewCenterPoint.x, previewFromPoint.y ?? previewCenterPoint.y];
        to: [previewLoopEnd.x ?? previewCenterPoint.x, previewLoopEnd.y ?? previewCenterPoint.y];
        stroke: basePreviewStroke;
        width: basePreviewWidth;
      };
    };

  circlePreviewRadius: radiusValue;
  circlePreviewPoint: lib.previewPoint({ x: 0; y: 0 }, previewCenterPoint, previewScale);
  circlePreviewScale: math.abs(previewScale ?? 1);
  circlePreviewGraphic: {
    type: 'circle';
    center: [circlePreviewPoint.x ?? previewCenterPoint.x, circlePreviewPoint.y ?? previewCenterPoint.y];
    radius: circlePreviewRadius * circlePreviewScale;
    stroke: basePreviewStroke;
    width: basePreviewWidth;
    fill: 'transparent';
  };

  waitingMessageDefault: {
    type: 'text';
    position: [0, 0];
    text: 'waiting...';
    color: '#64748b';
    fontSize: 10;
    align: 'center';
  };
  waitingMessage: prep.waitingMessage ?? waitingMessageDefault;

  previewLinesReady: math.min(1, literalReadyFlag);
  previewReadyLayer: multiLineGraphics + [circlePreviewGraphic];
  previewWaitingLayer:
    range(0, 1 - previewLinesReady) reduce (acc, _) => acc + [waitingMessage] ~ [];
  previewContent:
    if (previewLinesReady = 1)
      then previewReadyLayer
      else previewWaitingLayer;

  entryGraphics: lib.textEntry(prep.entryInputPosition ?? [0, 0], prep.entryInputSize ?? [0, 0], visibleTextValue, visibleCursorIndex);

  graphics: stageGraphics + infoLayer + previewContent + entryGraphics;

  eval {
    title: stageTitle;
    graphics;
    duration: sceneDuration;
  };
};

eval sceneGenerator;

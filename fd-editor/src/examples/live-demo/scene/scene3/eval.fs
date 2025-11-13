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

  stageTitle: prep.stageTitle ?? 'Reinforce the sticks';
  typeSpeed: prep.typeSpeed ?? 0;
  pauseDuration: prep.pauseDuration ?? 0;
  maxExpressionLength: prep.maxExpressionLength ?? 512;
  cursorResetDuration: prep.cursorResetDuration ?? 0.8;
  newlineChar: '\n';

  rangeLineText: 'range(0,10) map (i)=>{' + newlineChar;
  tetaLineText: 'teta:2*i*math.pi/10;' + newlineChar;
  radiusLineText: 'r:10;' + newlineChar;
  xLineText: 'x:r*math.cos(teta);' + newlineChar;
  yLineText: 'y:r*math.sin(teta);' + newlineChar;
  evalLineText: "eval { type: 'line'; from: [0,0]; to: [x,y]; }" + newlineChar;
  closingLineText: '}';
  baseExpressionText:
    rangeLineText + tetaLineText + radiusLineText + xLineText + yLineText + evalLineText + closingLineText;

  baseExpressionLength: length(baseExpressionText);
  sticksPrefixText: 'sticks: ';
  sticksPrefixLength: length(sticksPrefixText);
  radiusLineStartIndex: length(rangeLineText + tetaLineText);
  radiusLineLength: length(radiusLineText);

  circleLineText: "ri: { type: 'circle'; center: [0,0]; radius: r; };" + newlineChar;
  evalArrayLineText: 'eval [sticks, ri];' + newlineChar;
  circleAppendText: ';' + newlineChar + circleLineText;
  finalTailText: circleAppendText + evalArrayLineText;
  finalTailLength: length(finalTailText);

  scriptPrefix: [
    { kind: 'type'; text: baseExpressionText; speed: typeSpeed; markReady: 1; },
    { kind: 'wait'; duration: pauseDuration; }
  ];
  baseScriptSteps: scriptPrefix;

  extractRadiusSteps: [
    { kind: 'moveCursor'; position: radiusLineStartIndex; duration: cursorResetDuration; },
    { kind: 'delete'; direction: 'forward'; count: radiusLineLength; speed: typeSpeed; },
    { kind: 'moveCursor'; position: 'start'; duration: cursorResetDuration; },
    { kind: 'type'; text: radiusLineText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  tagExpressionSteps: [
    { kind: 'moveCursor'; position: radiusLineLength; duration: cursorResetDuration; },
    { kind: 'type'; text: sticksPrefixText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  circleSteps: [
    { kind: 'moveCursor'; position: 'end'; duration: cursorResetDuration; },
    { kind: 'type'; text: circleAppendText; speed: typeSpeed; },
    { kind: 'wait'; duration: pauseDuration; }
  ];

  evalArraySteps: [
    { kind: 'type'; text: evalArrayLineText; speed: typeSpeed; }
  ];

  scriptSteps:
    baseScriptSteps
    + extractRadiusSteps
    + tagExpressionSteps
    + circleSteps
    + evalArraySteps;

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
  totalTypedChars: length(typedTextValue);

  limitedChars: clamp(totalTypedChars, 0, maxExpressionLength);
  visibleTextValue: substring(typedTextValue, 0, limitedChars);
  visibleCursorIndex: clamp(cursorIndex, 0, limitedChars);

  tailStartRaw: totalTypedChars - finalTailLength;
  tailStart: clamp(tailStartRaw, 0, totalTypedChars);
  tailAvailable: totalTypedChars - tailStart;
  tailLengthClamped: clamp(finalTailLength, 0, tailAvailable);
  tailSample: substring(typedTextValue, tailStart, tailLengthClamped);
  tailMatchesFinal:
    if (tailLengthClamped = finalTailLength)
      then if (tailSample = finalTailText) then 1 else 0
      else 0;
  finalReadyFlag: tailMatchesFinal;

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
  betterSubtitle: {
    type: 'text';
    position: [infoTextLeft, infoSubtitleY];
    text: "Now it's better";
    color: '#fbbf24';
    fontSize: prep.viewHeightValue * 0.04;
    align: 'left';
  };
  subtitleLayer:
    if (finalReadyFlag = 1)
      then [betterSubtitle]
      else [];
  infoLayer: infoLayerBase + subtitleLayer;

  lineStartPoint: prep.lineStart ?? { x: 0; y: 0 };
  radiusValue: 10;
  multiLineCount: 10;
  previewCenterPoint: prep.previewCenterPoint ?? { x: 0; y: 0 };
  previewScale: prep.previewScale ?? 1;
  previewFromPoint: lib.previewPoint(lineStartPoint, previewCenterPoint, previewScale);
  basePreviewStroke: prep.previewStroke ?? '#e2e8f0';
  basePreviewWidth: prep.previewWidth ?? 0.8;

  multiLineGraphics:
    range(0, multiLineCount) map (indexValue) => {
      angle: (2 * indexValue * math.pi) / multiLineCount;
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

  literalReadyFlag:
    if (timeOffset >= baseScriptDuration)
      then 1
      else literalReadyFlagRaw;

  previewLinesReady: math.min(1, literalReadyFlag);
  previewCircleReady: math.min(1, finalReadyFlag);

  previewReadyLayer:
    if (previewLinesReady = 0)
      then []
      else if (previewCircleReady = 1)
        then multiLineGraphics + [circlePreviewGraphic]
        else multiLineGraphics;
  previewWaitingLayer:
    range(0, 1 - math.min(1, previewLinesReady)) reduce (acc, _) => acc + [waitingMessage] ~ [];
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

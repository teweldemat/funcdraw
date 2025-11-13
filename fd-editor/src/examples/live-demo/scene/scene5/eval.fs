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

  stageTitle: prep.stageTitle ?? 'Enjoy!';
  maxExpressionLength: prep.maxExpressionLength ?? 512;

  expressionText:
    'r:10;\n'
    + 'spin:t*math.pi/4;\n'
    + 'sticks: range(0,10) map (i)=>{\n'
    + 'teta:2*i*math.pi/10 + spin;\n'
    + 'x:r*math.cos(teta);\n'
    + 'y:r*math.sin(teta);\n'
    + "eval { type: 'line'; from: [0,0]; to: [x,y]; };\n"
    + '};\n'
    + "ri: { type: 'circle'; center: [0,0]; radius: r; };\n"
    + 'eval [sticks, ri];\n';

  sceneTimeRaw: timeOffsetParam ?? 0;
  sceneTimePositive: if (sceneTimeRaw < 0) then 0 else sceneTimeRaw;

  limitedChars: clamp(length(expressionText), 0, maxExpressionLength);
  visibleTextValue: substring(expressionText, 0, limitedChars);

  stageGraphics: prep.stageGraphics ?? [];
  infoLayerBase: prep.infoLayer ?? [];
  stageResultData: prep.stageResult ?? {};
  infoContainerDefault: { position: [0, 0]; size: [0, 0]; };
  previewContainer: stageResultData.previewContainerDimensions ?? infoContainerDefault;
  entryContainer: stageResultData.entryContainerDimensions ?? infoContainerDefault;

  lineStartPoint: prep.lineStart ?? { x: 0; y: 0 };
  radiusValue: 10;
  multiLineCount: 10;
  previewCenterPoint: prep.previewCenterPoint ?? { x: 0; y: 0 };
  previewScale: prep.previewScale ?? 1;
  previewFromPoint: lib.previewPoint(lineStartPoint, previewCenterPoint, previewScale);
  basePreviewStroke: prep.previewStroke ?? '#e2e8f0';
  basePreviewWidth: prep.previewWidth ?? 0.8;

  spinSpeed: math.pi / 4;
  spinAngle: spinSpeed * sceneTimePositive;

  multiLineGraphics:
    range(0, multiLineCount) map (indexValue) => {
      baseAngle: (2 * indexValue * math.pi) / multiLineCount;
      angle: baseAngle + spinAngle;
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

  heroLineOneText: 'That is it!';
  heroLineTwoText: 'Start vibe coding your art.';
  overlayPaddingRatio: 0.04;
  stageBackgroundRect: stageGraphics[0] ?? {};
  stageBoundsPosition: stageBackgroundRect.position ?? [prep.stageResult?.previewContainerDimensions?.position?.[0] ?? -60, prep.stageResult?.previewContainerDimensions?.position?.[1] ?? -40];
  stageBoundsSize: stageBackgroundRect.size ?? [prep.stageResult?.previewContainerDimensions?.size?.[0] ?? 120, prep.stageResult?.previewContainerDimensions?.size?.[1] ?? 80];
  overlayPaddingX: stageBoundsSize[0] * overlayPaddingRatio;
  overlayPaddingY: stageBoundsSize[1] * overlayPaddingRatio;
  overlayPosition: [
    stageBoundsPosition[0] + overlayPaddingX,
    stageBoundsPosition[1] + overlayPaddingY
  ];
  overlaySize: [
    stageBoundsSize[0] - overlayPaddingX * 2,
    stageBoundsSize[1] - overlayPaddingY * 2
  ];
  overlayRect: {
    type: 'rect';
    position: overlayPosition;
    size: overlaySize;
    fill: 'rgba(2,6,23,0.72)';
    stroke: 'rgba(148,163,184,0.4)';
    width: 0.4;
  };
  overlayCenterX: overlayPosition[0] + overlaySize[0] / 2;
  overlayLineHeight: prep.viewHeightValue * 0.065;
  heroLineOneY: overlayPosition[1] + overlaySize[1] * 0.55;
  heroLineDelay: 1.2;
  heroTypeSpeed: 18;
  heroLineTwoLength: length(heroLineTwoText);
  heroElapsed: sceneTimePositive - heroLineDelay;
  heroTypedCharsRaw: math.floor(heroElapsed * heroTypeSpeed);
  heroTypedChars: clamp(heroTypedCharsRaw, 0, heroLineTwoLength);
  heroTypedText: substring(heroLineTwoText, 0, heroTypedChars);
  heroEntryWidth: overlaySize[0] * 0.7;
  heroEntryHeight: overlayLineHeight * 1.8;
  heroEntryPosition: [
    overlayCenterX - heroEntryWidth / 2,
    heroLineOneY - overlayLineHeight * 0.2 - heroEntryHeight
  ];
  heroEntrySize: [heroEntryWidth, heroEntryHeight];
  heroEntryGraphics:
    lib.textEntry(heroEntryPosition, heroEntrySize, heroTypedText, heroTypedChars);
  heroLineOne: {
    type: 'text';
    position: [overlayCenterX, heroLineOneY];
    text: heroLineOneText;
    color: '#f8fafc';
    fontSize: overlayLineHeight * 1.05;
    align: 'center';
  };

  entryGraphics:
    lib.textEntry(
      prep.entryInputPosition ?? [0, 0],
      prep.entryInputSize ?? [0, 0],
      visibleTextValue,
      length(visibleTextValue)
    );

  graphics:
    stageGraphics
    + infoLayerBase
    + multiLineGraphics
    + [circlePreviewGraphic]
    + entryGraphics
    + [overlayRect, heroLineOne]
    + heroEntryGraphics;

  eval {
    title: stageTitle;
    graphics;
    duration: 12;
  };
};

eval sceneGenerator;

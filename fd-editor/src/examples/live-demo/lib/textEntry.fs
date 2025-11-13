(locationParam, sizeParam, textParam, cursorParam) => {
  location: locationParam ?? [0, 0];
  sizeValue: sizeParam ?? [48, 12];
  textValue: textParam ?? '';
  cursorValueRaw: cursorParam ?? 0;

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

  blockWidth: sizeValue[0] ?? 48;
  blockHeight: sizeValue[1] ?? 12;
  paddingX: math.max(1.5, blockWidth * 0.02);
  paddingY: math.max(1.0, blockHeight * 0.025);
  targetLines: 10;
  lineHeightFactor: 1.2;
  usableHeight: math.max(blockHeight - paddingY * 2, blockHeight * 0.1);
  fontSizeRaw: usableHeight / (targetLines * lineHeightFactor);
  fontSize: math.max(3.5, fontSizeRaw);
  baseX: location[0] + paddingX;

  measurement: fd.measuretext(textValue, fontSize);
  avgCharWidth: measurement.avgCharWidth ?? math.max(1.5, fontSize * 0.58);
  measuredLineHeight: measurement.lineHeight ?? (fontSize * lineHeightFactor);
  measuredAscent: measurement.ascent ?? (fontSize * 0.78);
  measuredDescent: measurement.descent ?? (measuredLineHeight - measuredAscent);
  cursorAdvance: math.max(0.5, avgCharWidth);

  textTopY: location[1] + blockHeight - paddingY;
  baseY: textTopY - measuredAscent;
  cursorUpperBound: math.max(length(textValue), 0);
  cursorIndexValue: clamp(math.floor(cursorValueRaw ?? 0), 0, cursorUpperBound);

  newlineChar: '\n';
  splitInitial: {
    lines: [];
    currentLine: '';
    currentStart: 0;
  };
  splitState:
    range(0, length(textValue)) reduce (acc, index) => {
      currentChar: substring(textValue, index, 1);
      isBreak: currentChar = newlineChar;
      completedLine: {
        text: acc.currentLine ?? '';
        start: acc.currentStart ?? 0;
      };
      eval if (isBreak)
        then {
          lines: acc.lines + [completedLine];
          currentLine: '';
          currentStart: index + 1;
        }
        else {
          lines: acc.lines;
          currentLine: (acc.currentLine ?? '') + currentChar;
          currentStart: acc.currentStart ?? 0;
        };
    } ~ splitInitial;

  finalLineRecord: {
    text: splitState.currentLine ?? '';
    start: splitState.currentStart ?? 0;
  };
  linesInfoRaw: splitState.lines + [finalLineRecord];

  cursorLineLookup:
    linesInfoRaw reduce (acc, entry) => {
      indexValue: acc.index ?? 0;
      entryStart: entry.start ?? 0;
      shouldSelect: cursorIndexValue >= entryStart;
      eval {
        line: if (shouldSelect) then indexValue else acc.line ?? 0;
        start: if (shouldSelect) then entryStart else acc.start ?? 0;
        index: indexValue + 1;
      };
    } ~ { line: 0; start: 0; index: 0 };

  lineGraphicsState:
    linesInfoRaw reduce (acc, entry) => {
      indexValue: acc.index ?? 0;
      lineBaseline: baseY - measuredLineHeight * indexValue;
      textGraphic: {
        type: 'text';
        position: [baseX, lineBaseline];
        text: entry.text ?? '';
        color: textColor;
        fontSize;
        align: 'left';
      };
      eval {
        graphics: acc.graphics + [textGraphic];
        index: indexValue + 1;
      };
    } ~ { graphics: []; index: 0 };

  cursorLineIndex: cursorLineLookup.line ?? 0;
  cursorLineStart: cursorLineLookup.start ?? 0;
  columnIndexRaw: cursorIndexValue - cursorLineStart;
  columnIndex: math.max(columnIndexRaw, 0);
  cursorLineData:
    linesInfoRaw reduce (acc, entry) => {
      indexValue: acc.index ?? 0;
      foundEntry: acc.entry ?? { text: ''; start: 0 };
      eval if (indexValue = cursorLineIndex)
        then {
          index: indexValue + 1;
          entry: entry;
        }
        else {
          index: indexValue + 1;
          entry: foundEntry;
        };
    } ~ { index: 0; entry: { text: ''; start: 0 } };
  cursorLineTextValue: cursorLineData.entry.text ?? '';
  cursorLineLength: length(cursorLineTextValue);
  cursorColumnLimited: clamp(columnIndex, 0, cursorLineLength);
  cursorSegment: substring(cursorLineTextValue, 0, cursorColumnLimited);
  cursorSegmentMeasurement: fd.measuretext(cursorSegment, fontSize) ?? {};
  cursorSegmentWidth: cursorSegmentMeasurement.width ?? (cursorAdvance * cursorColumnLimited);
  cursorX: baseX + cursorSegmentWidth;
  cursorBaseline: baseY - measuredLineHeight * cursorLineIndex;
  cursorTop: cursorBaseline + measuredAscent;
  cursorBottom: cursorBaseline - measuredDescent;

  textColor: '#e2e8f0';
  cursorColor: '#f8fafc';
  trackColor: 'rgba(15,23,42,0.55)';
  trackStroke: 'transparent';

  textTrack: {
    type: 'rect';
    position: [location[0], location[1]];
    size: [blockWidth, blockHeight];
    fill: trackColor;
    stroke: trackStroke;
    width: 0;
  };

  cursorLine: {
    type: 'line';
    from: [cursorX, cursorTop];
    to: [cursorX, cursorBottom];
    stroke: cursorColor;
    width: math.max(0.6, fontSize * 0.08);
  };

  eval [textTrack] + (lineGraphicsState.graphics ?? []) + [cursorLine];
};

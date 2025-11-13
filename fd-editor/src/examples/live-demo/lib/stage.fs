{
  viewWidth: view.maxX - view.minX;
  viewHeight: view.maxY - view.minY;

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

  background: {
    type: 'rect';
    position: [view.minX, view.minY];
    size: [viewWidth, viewHeight];
    fill: '#020617';
    stroke: '#0f172a';
    width: 0.8;
  };

  sectionLeft: view.minX + viewWidth * 0.04;
  sectionWidth: viewWidth * 0.92;
  topHeight: viewHeight * 0.18;
  verticalGap: viewHeight * 0.02;
  columnGap: sectionWidth * 0.03;
  entryWidthRatio: 0.65;
  entryWidth: sectionWidth * entryWidthRatio;
  previewWidthRaw: sectionWidth - entryWidth - columnGap;
  previewWidth: clamp(previewWidthRaw, sectionWidth * 0.2, sectionWidth);
  contentHeight: viewHeight * 0.62;

  infoOriginY: view.maxY - topHeight - verticalGap;
  contentOriginY: infoOriginY - verticalGap - contentHeight;

  infoRectSize: [sectionWidth, topHeight];
  infoRectPosition: [sectionLeft, infoOriginY];
  entryRectSize: [entryWidth, contentHeight];
  entryRectPosition: [sectionLeft, contentOriginY];
  previewSquareSize: math.min(previewWidth, contentHeight);
  previewRectSize: [previewSquareSize, previewSquareSize];
  previewRectPosition: [
    sectionLeft + entryWidth + columnGap,
    contentOriginY + (contentHeight - previewSquareSize) / 2
  ];

  infoRect: {
    type: 'rect';
    position: infoRectPosition;
    size: infoRectSize;
    fill: 'rgba(15,23,42,0.75)';
    stroke: 'rgba(59,130,246,0.35)';
    width: 0.6;
  };

  previewRect: {
    type: 'rect';
    position: previewRectPosition;
    size: previewRectSize;
    fill: 'rgba(15,23,42,0.8)';
    stroke: 'rgba(148,163,184,0.3)';
    width: 0.6;
  };

  entryRect: {
    type: 'rect';
    position: entryRectPosition;
    size: entryRectSize;
    fill: 'rgba(15,23,42,0.85)';
    stroke: 'rgba(59,130,246,0.25)';
    width: 0.6;
  };

  stageGraphic: [background, infoRect, previewRect, entryRect];

  eval {
    stageGraphic;
    infoContainerDimensions: {
      position: infoRectPosition;
      size: infoRectSize;
    };
    previewContainerDimensions: {
      position: previewRectPosition;
      size: previewRectSize;
    };
    entryContainerDimensions: {
      position: entryRectPosition;
      size: entryRectSize;
    };
  };
}

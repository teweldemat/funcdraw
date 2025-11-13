(baseLocationParam, widthParam, optionsParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  width: widthParam ?? 8;
  options: optionsParam ?? {};
  scale: width / 8;

  facadeHeight: 8.2 * scale;
  stoopHeight: 0.9 * scale;
  parapetHeight: 0.6 * scale;

  baseY: baseLocation[1];
  stoopTop: baseY - stoopHeight;
  facadeTop: stoopTop - facadeHeight;
  parapetTop: facadeTop - parapetHeight;
  leftEdge: baseLocation[0] - width / 2;
  rightEdge: baseLocation[0] + width / 2;

  facadeColor: options.facadeColor ?? '#f97316';
  accentColor: options.accentColor ?? '#c2410c';
  windowColor: options.windowColor ?? '#cffafe';
  frameColor: options.frameColor ?? '#0f172a';
  doorColor: options.doorColor ?? '#4c0519';
  wallColor: options.wallColor ?? '#fde68a';

  showShadow: options.showShadow ?? false;
  wallBackdropColor: options.wallBackdropColor ?? '#fee2b6';
  wallBackdropPadding: options.wallBackdropPadding ?? 0.35;
  shadow: {
    type: 'ellipse';
    center: [baseLocation[0], baseY - stoopHeight * 0.3];
    radiusX: width * 0.55;
    radiusY: stoopHeight * 0.7;
    fill: 'rgba(15, 23, 42, 0.18)';
    stroke: 'transparent';
  };
  shadowLayer: if showShadow then [shadow] else [];

  wallTop: parapetTop - parapetHeight;
  wallBottom: baseY;
  wallBackdrop: {
    type: 'rect';
    position: [leftEdge - wallBackdropPadding * scale, wallTop - wallBackdropPadding * scale];
    size: [width + 2 * wallBackdropPadding * scale, (wallBottom - wallTop) + 2 * wallBackdropPadding * scale];
    fill: wallBackdropColor;
    stroke: 'transparent';
  };

  overallTopY: parapetTop;
  structureHeight: baseY - overallTopY;

  stoop: {
    type: 'rect';
    position: [leftEdge + 0.5 * scale, stoopTop];
    size: [width - scale, stoopHeight];
    fill: '#e2e8f0';
    stroke: frameColor;
    width: 0.18 * scale;
  };

  facade: {
    type: 'rect';
    position: [leftEdge + 0.3 * scale, facadeTop];
    size: [width - 0.6 * scale, facadeHeight];
    fill: facadeColor;
    stroke: frameColor;
    width: 0.18 * scale;
  };

  wallPanel: {
    type: 'rect';
    position: [leftEdge + 0.38 * scale, facadeTop + 0.6 * scale];
    size: [width - 0.76 * scale, facadeHeight - 1.2 * scale];
    fill: wallColor;
    stroke: 'transparent';
  };

  accentColumn: {
    type: 'rect';
    position: [baseLocation[0] - width * 0.12, facadeTop + 0.5 * scale];
    size: [width * 0.24, facadeHeight - 1.2 * scale];
    fill: accentColor;
    opacity: 0.35;
    stroke: 'transparent';
  };

  parapet: {
    type: 'rect';
    position: [leftEdge + 0.15 * scale, parapetTop];
    size: [width - 0.3 * scale, parapetHeight];
    fill: frameColor;
    stroke: 'transparent';
  };

  makeWindow: (columnOffset, level) => {
    levels: [0, 1, 2];
    levelCount: length(levels);
    heightDivisor: if (levelCount = 0) then 1 else levelCount;
    levelHeight: facadeHeight / heightDivisor;
    topOffset: facadeTop + 0.8 * scale + level * levelHeight;
    windowWidth: width * 0.18;
    windowHeight: levelHeight * 0.55;
    eval {
      type: 'rect';
      position: [
        baseLocation[0] + columnOffset - windowWidth / 2,
        topOffset
      ];
      size: [windowWidth, windowHeight];
      fill: windowColor;
      stroke: frameColor;
      width: 0.12 * scale;
    };
  };

  columnOffset: width * 0.25;
  windowLevels: [0, 1, 2];
  leftWindows: windowLevels map (level) => makeWindow(-columnOffset, level);
  rightWindows: windowLevels map (level) => makeWindow(columnOffset, level);

  balcony: {
    type: 'rect';
    position: [baseLocation[0] - width * 0.28, facadeTop + facadeHeight * 0.38];
    size: [width * 0.56, 0.2 * scale];
    fill: frameColor;
    opacity: 0.75;
  };

  doorWidth: width * 0.22;
  doorHeight: facadeHeight * 0.45;
  door: {
    type: 'rect';
    position: [baseLocation[0] - doorWidth / 2, stoopTop - doorHeight];
    size: [doorWidth, doorHeight];
    fill: doorColor;
    stroke: frameColor;
    width: 0.2 * scale;
  };

  doorKnob: {
    type: 'circle';
    center: [baseLocation[0] + doorWidth * 0.25, stoopTop - doorHeight / 2];
    radius: 0.12 * scale;
    fill: '#fde68a';
    stroke: frameColor;
    width: 0.08 * scale;
  };

  eval {
    graphics: [
      wallBackdrop,
      shadowLayer,
      stoop,
      facade,
      wallPanel,
      accentColumn,
      parapet,
      balcony,
      door,
      doorKnob,
      leftWindows,
      rightWindows
    ];
    width: width + scale;
    height: structureHeight + stoopHeight;
    above: structureHeight;
    below: stoopHeight;
  };
}

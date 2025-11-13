(baseLocationParam, widthParam, optionsParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  width: widthParam ?? 12;
  options: optionsParam ?? {};
  scale: width / 12;

  wingHeight: 4.2 * scale;
  atriumHeight: 6.2 * scale;
  plinthHeight: 0.9 * scale;

  baseY: baseLocation[1];
  plinthTop: baseY - plinthHeight;
  leftWingTop: plinthTop - wingHeight;
  rightWingTop: plinthTop - atriumHeight;
  leftEdge: baseLocation[0] - width / 2;
  rightEdge: baseLocation[0] + width / 2;

  baseColor: options.baseColor ?? '#e2e8f0';
  accentColor: options.accentColor ?? '#0ea5e9';
  glassColor: options.glassColor ?? '#bae6fd';
  frameColor: options.frameColor ?? '#0f172a';
  plankColor: options.plankColor ?? '#fcd34d';
  wallColor: options.wallColor ?? '#f8fafc';

  showShadow: options.showShadow ?? false;
  mainWallColor: options.mainWallColor ?? '#e5e7eb';
  wallBackdropPadding: options.wallBackdropPadding ?? 0.4;

  structureTop:
    if (leftWingTop < rightWingTop)
      then leftWingTop
      else rightWingTop;
  structureBottom: baseY;
  wallHeight: structureBottom - structureTop;

  mainWall: {
    type: 'rect';
    position: [leftEdge - wallBackdropPadding * scale, structureTop - wallBackdropPadding * scale];
    size: [width + 2 * wallBackdropPadding * scale, wallHeight + 2 * wallBackdropPadding * scale];
    fill: mainWallColor;
    stroke: 'transparent';
  };

  shadow: {
    type: 'ellipse';
    center: [baseLocation[0], baseY - plinthHeight * 0.4];
    radiusX: width * 0.65;
    radiusY: plinthHeight * 0.8;
    fill: 'rgba(15, 23, 42, 0.15)';
    stroke: 'transparent';
  };
  shadowLayer: if showShadow then [shadow] else [];

  plinth: {
    type: 'rect';
    position: [leftEdge, plinthTop];
    size: [width, plinthHeight];
    fill: '#cbd5f5';
    stroke: frameColor;
    width: 0.15 * scale;
  };

  leftWing: {
    type: 'rect';
    position: [leftEdge, leftWingTop];
    size: [width * 0.45, wingHeight];
    fill: plankColor;
    stroke: frameColor;
    width: 0.2 * scale;
  };

  rightWing: {
    type: 'rect';
    position: [leftEdge + width * 0.45, rightWingTop];
    size: [width * 0.55, atriumHeight];
    fill: baseColor;
    stroke: frameColor;
    width: 0.2 * scale;
  };

  atriumWall: {
    type: 'rect';
    position: [leftEdge + width * 0.47, rightWingTop + 0.3 * scale];
    size: [width * 0.5, atriumHeight - 0.6 * scale];
    fill: wallColor;
    stroke: 'transparent';
  };

  lightStrip: {
    type: 'rect';
    position: [leftEdge + width * 0.02, leftWingTop + wingHeight * 0.55];
    size: [width * 0.41, wingHeight * 0.18];
    fill: accentColor;
    opacity: 0.4;
  };

  makeGlassPanel: (x, y, panelWidth, panelHeight) => {
    eval {
      type: 'rect';
      position: [x, y];
      size: [panelWidth, panelHeight];
      fill: glassColor;
      stroke: accentColor;
      width: 0.12 * scale;
    };
  };

  panelTop: rightWingTop + 0.4 * scale;
  panelWidth: width * 0.14;
  panelGap: width * 0.03;
  panelHeight: atriumHeight - 0.8 * scale;
  panelStartX: leftEdge + width * 0.5;
  atriumPanels:
    [0, 1, 2] map (index) =>
      makeGlassPanel(
        panelStartX + index * (panelWidth + panelGap),
        panelTop,
        panelWidth,
        panelHeight
      );

  clerestory: {
    type: 'rect';
    position: [leftEdge + width * 0.02, leftWingTop - 0.6 * scale];
    size: [width * 0.78, 0.5 * scale];
    fill: frameColor;
    opacity: 0.8;
  };

  terrace: {
    type: 'rect';
    position: [baseLocation[0] - width * 0.35, leftWingTop - 1.4 * scale];
    size: [width * 0.7, 0.3 * scale];
    fill: accentColor;
    opacity: 0.3;
  };

  door: {
    type: 'rect';
    position: [leftEdge + width * 0.55, plinthTop - atriumHeight * 0.45];
    size: [width * 0.14, atriumHeight * 0.45];
    fill: '#0f172a';
    stroke: accentColor;
    width: 0.15 * scale;
  };

  handle: {
    type: 'rect';
    position: [leftEdge + width * 0.64, plinthTop - atriumHeight * 0.2];
    size: [width * 0.01, atriumHeight * 0.08];
    fill: '#f8fafc';
  };

  planter: {
    type: 'rect';
    position: [leftEdge + width * 0.05, plinthTop - 0.6 * scale];
    size: [width * 0.18, 0.5 * scale];
    fill: '#14532d';
    stroke: 'transparent';
  };

  eval {
    graphics: [
      mainWall,
      shadowLayer,
      plinth,
      leftWing,
      rightWing,
      atriumWall,
      lightStrip,
      atriumPanels,
      clerestory,
      terrace,
      door,
      handle,
      planter
    ];
    width: width + 2 * scale;
    height: wallHeight + plinthHeight;
    above: wallHeight;
    below: plinthHeight;
  };
}

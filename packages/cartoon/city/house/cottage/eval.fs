(baseLocationParam, widthParam, optionsParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  width: widthParam ?? 10;
  options: optionsParam ?? {};
  scale: width / 10;

  facadeHeight: 5.2 * scale;
  roofHeight: 2.6 * scale;
  foundationHeight: 0.7 * scale;

  baseY: baseLocation[1];
  facadeBottom: baseY - foundationHeight;
  facadeTop: facadeBottom - facadeHeight;
  ridgeY: facadeTop - roofHeight;
  leftEdge: baseLocation[0] - width / 2;
  rightEdge: baseLocation[0] + width / 2;

  facadeColor: options.facadeColor ?? '#fcd34d';
  roofColor: options.roofColor ?? '#b45309';
  trimColor: options.trimColor ?? '#78350f';
  doorColor: options.doorColor ?? '#92400e';
  windowColor: options.windowColor ?? '#e0f2fe';

  showShadow: options.showShadow ?? false;
  wallBackdropColor: options.wallBackdropColor ?? '#fde68a';
  wallBackdropPadding: options.wallBackdropPadding ?? 0.4;

  shadow: {
    type: 'ellipse';
    center: [baseLocation[0], baseY - foundationHeight * 0.4];
    radiusX: width * 0.55;
    radiusY: foundationHeight * 0.6;
    fill: 'rgba(15, 23, 42, 0.18)';
    stroke: 'transparent';
  };
  shadowLayer: if showShadow then [shadow] else [];

  wallTop: ridgeY;
  wallBottom: baseY;
  wallBackdrop: {
    type: 'rect';
    position: [leftEdge - wallBackdropPadding * scale, facadeTop - wallBackdropPadding * scale];
    size: [width + 2 * wallBackdropPadding * scale, (wallBottom - facadeTop) + 1.5 * wallBackdropPadding * scale];
    fill: wallBackdropColor;
    stroke: 'transparent';
  };

  structureHeight: baseY - ridgeY;
  totalAbove: structureHeight + chimneyHeight;
  totalHeight: totalAbove + foundationHeight;

  foundation: {
    type: 'rect';
    position: [leftEdge, facadeBottom];
    size: [width, foundationHeight];
    fill: '#cbd5f5';
    stroke: trimColor;
    width: 0.18 * scale;
  };

  facade: {
    type: 'rect';
    position: [leftEdge + 0.2 * scale, facadeTop];
    size: [width - 0.4 * scale, facadeHeight];
    fill: facadeColor;
    stroke: trimColor;
    width: 0.25 * scale;
  };

  roof: {
    type: 'polygon';
    points: [
      [leftEdge - 0.3 * scale, facadeTop],
      [baseLocation[0], ridgeY],
      [rightEdge + 0.3 * scale, facadeTop]
    ];
    fill: roofColor;
    stroke: '#7c2d12';
    width: 0.3 * scale;
  };

  trim: {
    type: 'rect';
    position: [leftEdge + 0.15 * scale, facadeTop + 0.4 * scale];
    size: [width - 0.3 * scale, 0.25 * scale];
    fill: '#fde68a';
    stroke: 'transparent';
  };

  doorWidth: width * 0.22;
  doorHeight: facadeHeight * 0.65;
  door: {
    type: 'rect';
    position: [baseLocation[0] - doorWidth / 2, facadeBottom - doorHeight];
    size: [doorWidth, doorHeight];
    fill: doorColor;
    stroke: trimColor;
    width: 0.25 * scale;
    radius: 0.4 * scale;
  };

  doorKnob: {
    type: 'circle';
    center: [baseLocation[0] + doorWidth * 0.2, facadeBottom - doorHeight / 2];
    radius: 0.15 * scale;
    fill: '#fef3c7';
    stroke: trimColor;
    width: 0.12 * scale;
  };

  windowSize: width * 0.18;
  windowTop: facadeTop + facadeHeight * 0.35;
  windowSpacing: width * 0.2;

  leftWindow: {
    type: 'rect';
    position: [baseLocation[0] - windowSpacing - windowSize, windowTop - windowSize];
    size: [windowSize, windowSize];
    fill: windowColor;
    stroke: '#bae6fd';
    width: 0.15 * scale;
  };

  rightWindow: {
    type: 'rect';
    position: [baseLocation[0] + windowSpacing, windowTop - windowSize];
    size: [windowSize, windowSize];
    fill: windowColor;
    stroke: '#bae6fd';
    width: 0.15 * scale;
  };

  crossbar: (window) => {
    half: window.size[1] / 2;
    eval {
      type: 'rect';
      position: [window.position[0], window.position[1] + half - 0.08 * scale];
      size: [window.size[0], 0.16 * scale];
      fill: '#c4b5fd';
      stroke: 'transparent';
    };
  };

  windowFrames: [crossbar(leftWindow), crossbar(rightWindow)];

  atticWindow: {
    type: 'circle';
    center: [baseLocation[0], facadeTop + 0.7 * scale];
    radius: 0.6 * scale;
    fill: windowColor;
    stroke: trimColor;
    width: 0.18 * scale;
  };

  chimneyHeight: roofHeight * 0.9;
  chimney: {
    type: 'rect';
    position: [baseLocation[0] + width * 0.18, ridgeY - chimneyHeight];
    size: [0.9 * scale, chimneyHeight];
    fill: '#f97316';
    stroke: '#7c2d12';
    width: 0.2 * scale;
  };

  eval {
    graphics: [
      wallBackdrop,
      shadowLayer,
      foundation,
      facade,
      trim,
      roof,
      chimney,
      door,
      doorKnob,
      leftWindow,
      rightWindow,
      windowFrames,
      atticWindow
    ];
    width: width + scale;
    height: totalHeight;
    above: totalAbove;
    below: foundationHeight;
  };
}

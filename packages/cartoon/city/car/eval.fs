(baseLocationParam, lengthParam, optionsParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  length: lengthParam ?? 11;
  options: optionsParam ?? {};
  wheelModule: import("../machine/parts/wheel");

  scale: length / 11;
  wheelRadius: 0.9 * scale;
  wheelSpread: length * 0.32;
  bodyHeight: 2.6 * scale;
  cabinHeight: 1.8 * scale;
  ground: baseLocation[1];
  bodyBottom: ground - wheelRadius * 1.8;
  bodyTop: bodyBottom - bodyHeight;
  cabinBottom: bodyTop + 0.2 * scale;
  cabinTop: cabinBottom - cabinHeight;
  leftEdge: baseLocation[0] - length / 2;
  rightEdge: baseLocation[0] + length / 2;

  bodyColor: options.bodyColor ?? '#ef4444';
  accentColor: options.accentColor ?? '#b91c1c';
  cabinColor: options.cabinColor ?? '#fee2e2';
  windowColor: options.windowColor ?? '#e0f2fe';
  trimColor: options.trimColor ?? '#0f172a';

  shadow: {
    type: 'ellipse';
    center: [baseLocation[0], ground - wheelRadius * 0.2];
    radiusX: length * 0.55;
    radiusY: wheelRadius * 0.55;
    fill: 'rgba(15, 23, 42, 0.15)';
    stroke: 'transparent';
  };

  lowerBody: {
    type: 'rect';
    position: [leftEdge, bodyTop];
    size: [length, bodyHeight];
    fill: bodyColor;
    stroke: trimColor;
    width: 0.25 * scale;
    radius: 0.4 * scale;
  };

  accentStripe: {
    type: 'rect';
    position: [leftEdge + 0.2 * scale, bodyTop + bodyHeight * 0.58];
    size: [length - 0.4 * scale, bodyHeight * 0.32];
    fill: accentColor;
    stroke: 'transparent';
  };

  cabinLeft: baseLocation[0] - length * 0.22;
  cabinRight: baseLocation[0] + length * 0.25;
  roofSlopeFront: cabinRight + length * 0.04;

  cabin: {
    type: 'polygon';
    points: [
      [cabinLeft, cabinBottom],
      [cabinLeft + length * 0.08, cabinTop],
      [cabinRight, cabinTop],
      [roofSlopeFront, cabinBottom],
      [cabinLeft, cabinBottom]
    ];
    fill: cabinColor;
    stroke: trimColor;
    width: 0.2 * scale;
  };

  windowWidth: length * 0.18;
  windowHeight: cabinHeight * 0.65;
  windowTop: cabinTop + cabinHeight * 0.15;

  rearWindow: {
    type: 'rect';
    position: [cabinLeft + length * 0.04, windowTop - windowHeight];
    size: [windowWidth, windowHeight];
    fill: windowColor;
    stroke: '#bae6fd';
    width: 0.15 * scale;
  };

  frontWindow: {
    type: 'rect';
    position: [cabinRight - windowWidth - length * 0.08, windowTop - windowHeight];
    size: [windowWidth, windowHeight];
    fill: windowColor;
    stroke: '#bae6fd';
    width: 0.15 * scale;
  };

  windshield: {
    type: 'polygon';
    points: [
      [cabinRight, cabinTop],
      [roofSlopeFront, cabinBottom],
      [roofSlopeFront + length * 0.02, cabinBottom],
      [cabinRight + length * 0.02, cabinTop]
    ];
    fill: windowColor;
    stroke: '#bae6fd';
    width: 0.12 * scale;
  };

  grill: {
    type: 'rect';
    position: [rightEdge - 0.8 * scale, bodyTop + bodyHeight * 0.45];
    size: [0.6 * scale, bodyHeight * 0.25];
    fill: '#fde68a';
    stroke: trimColor;
    width: 0.15 * scale;
  };

  tailLight: {
    type: 'rect';
    position: [leftEdge + 0.2 * scale, bodyTop + bodyHeight * 0.4];
    size: [0.4 * scale, bodyHeight * 0.35];
    fill: '#fb7185';
    stroke: trimColor;
    width: 0.15 * scale;
  };

  doorHandle: {
    type: 'rect';
    position: [baseLocation[0] + length * 0.05, bodyTop + bodyHeight * 0.35];
    size: [0.8 * scale, 0.15 * scale];
    fill: '#f8fafc';
    stroke: trimColor;
    width: 0.05 * scale;
  };

  doorSeam: {
    type: 'rect';
    position: [baseLocation[0] + length * 0.02, bodyTop + 0.1 * scale];
    size: [0.12 * scale, bodyHeight - 0.2 * scale];
    fill: '#0f172a';
    opacity: 0.25;
  };

  frontWheelCenter: [baseLocation[0] + wheelSpread, ground - wheelRadius];
  rearWheelCenter: [baseLocation[0] - wheelSpread, ground - wheelRadius];

  rearWheel: wheelModule(rearWheelCenter, wheelRadius, { rimColor: '#f8fafc' });
  frontWheel: wheelModule(frontWheelCenter, wheelRadius, { rimColor: '#f8fafc' });

  aboveExtent: ground - cabinTop;
  belowExtent: 2 * wheelRadius;
  totalHeight: aboveExtent + belowExtent;

  eval {
    graphics: [
      shadow,
      lowerBody,
      accentStripe,
      cabin,
      rearWindow,
      frontWindow,
      windshield,
      grill,
      tailLight,
      doorHandle,
      doorSeam,
      rearWheel,
      frontWheel
    ];
    width: length + 2 * scale;
    height: totalHeight;
    above: aboveExtent;
    below: belowExtent;
  };
}

(baseLocationParam, scaleParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  scale: scaleParam ?? 1;
  width: 1.2 * scale;
  height: 4.4 * scale;
  eval {
    type: 'rect';
    position: [baseLocation[0] - width / 2, baseLocation[1] - height];
    size: [width, height];
    fill: '#92400e';
    stroke: '#78350f';
    width: 0.25 * scale;
  };
}

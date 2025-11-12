(baseLocationParam, scaleParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  scale: scaleParam ?? 1;
  eval {
    type: 'ellipse';
    center: [baseLocation[0], baseLocation[1] - 0.2 * scale];
    radiusX: 3.2 * scale;
    radiusY: 0.9 * scale;
    fill: 'rgba(15, 23, 42, 0.25)';
    stroke: 'transparent';
  };
}

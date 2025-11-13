(baseLocationParam, heightParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  height: heightParam ?? 8;
  scale: height / 8;
  ornaments: [
    { offset: [-1.4, -3.0]; color: '#fbbf24' },
    { offset: [1.2, -3.6]; color: '#fb7185' },
    { offset: [0.2, -4.8]; color: '#60a5fa' }
  ];

  translatePoint: (offset) => [
    baseLocation[0] + offset[0] * scale,
    baseLocation[1] + offset[1] * scale
  ];

  ornamentShapes:
    ornaments map (entry) => {
      eval {
        type: 'circle';
        center: translatePoint(entry.offset);
        radius: 0.3 * scale;
        fill: entry.color;
        stroke: '#0f172a';
        width: 0.15 * scale;
      };
    };

  eval {
    graphics: [
      shadow(baseLocation, scale),
      trunk(baseLocation, scale),
      foliage(baseLocation, scale),
      ornamentShapes
    ];
    width: 6.5 * scale;
    height: height;
    above: height;
    below: 0;
  };
}

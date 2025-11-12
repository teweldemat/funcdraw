(baseLocationParam, scaleParam) => {
  baseLocation: baseLocationParam ?? [0, 0];
  scale: scaleParam ?? 1;
  tiers: [
    { width: 6.5; height: 2.4; y: -2.4; color: '#166534' },
    { width: 5.2; height: 2.1; y: -4.0; color: '#15803d' },
    { width: 3.8; height: 2.0; y: -5.4; color: '#22c55e' }
  ];

  buildTier: (tier) => {
    half: (tier.width * scale) / 2;
    eval {
      type: 'polygon';
      points: [
        [baseLocation[0], baseLocation[1] + (tier.y - tier.height) * scale],
        [baseLocation[0] - half, baseLocation[1] + tier.y * scale],
        [baseLocation[0] + half, baseLocation[1] + tier.y * scale]
      ];
      fill: tier.color;
      stroke: '#14532d';
      width: 0.25 * scale;
    };
  };

  eval tiers map buildTier;
}

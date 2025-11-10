// cloud({ center: [0, 0], width: 80 })
return (options) => {
  const center = options?.center ?? [0, 0];
  const targetWidth = options?.width ?? 12;
  const baseWidth = 12;
  const sizeScale = targetWidth / baseWidth;
  const baseRadius = 3.8 * sizeScale;
  const puffPalette = ['#f8fafc', '#f1f5f9', '#e2e8f0', '#cbd5f5'];
  const puffLayout = [
    { dx: -6.2, dy: 0.3, radiusScale: 1.05, shadeIndex: 2 },
    { dx: -3.4, dy: 1.3, radiusScale: 1.35, shadeIndex: 0 },
    { dx: -0.6, dy: 0.5, radiusScale: 1.55, shadeIndex: 1 },
    { dx: 2.8, dy: 1.1, radiusScale: 1.3, shadeIndex: 0 },
    { dx: 5.6, dy: 0.2, radiusScale: 1.0, shadeIndex: 3 }
  ];

  const shadow = {
    type: 'circle',
    data: {
      center: [center[0] + 2 * sizeScale, center[1] - 2.2 * sizeScale],
      radius: baseRadius * 1.5,
      fill: 'rgba(148,163,184,0.25)',
      stroke: 'transparent',
      width: 0
    }
  };

  const softGlow = {
    type: 'circle',
    data: {
      center: [center[0] + 0.5 * sizeScale, center[1] + 0.4 * sizeScale],
      radius: baseRadius * 1.8,
      fill: 'rgba(241,245,249,0.3)',
      stroke: 'transparent',
      width: 0
    }
  };

  const puffs = puffLayout.map((puff) => {
    const safeIndex = puff.shadeIndex < puffPalette.length ? puff.shadeIndex : puffPalette.length - 1;
    const shade = puffPalette[safeIndex];
    return {
      type: 'circle',
      data: {
        center: [center[0] + puff.dx * sizeScale, center[1] + puff.dy * sizeScale],
        radius: baseRadius * puff.radiusScale,
        fill: shade,
        stroke: 'transparent',
        width: 0
      }
    };
  });

  const highlight = {
    type: 'circle',
    data: {
      center: [center[0] - 1.8 * sizeScale, center[1] + 1.4 * sizeScale],
      radius: baseRadius * 0.8,
      fill: 'rgba(248,250,252,0.6)',
      stroke: 'transparent',
      width: 0
    }
  };

  return [shadow, softGlow, puffs, highlight];
};

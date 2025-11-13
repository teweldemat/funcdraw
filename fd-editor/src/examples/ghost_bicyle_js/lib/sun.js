// sun({ center: [0, 0], radius: 20 })
return (options) => {
  const center = options?.center ?? [0, 0];
  const radius = options?.radius ?? 20;
  const strokeWidth = radius * 0.058;
  const glowRadius = radius * 1.35;
  const rayCount = 12;
  const rayInnerRadius = radius * 1.15;
  const rayOuterRadius = radius * 1.5;

  const glow = {
    type: 'circle',
    center,
    radius: glowRadius,
    fill: 'rgba(253,224,71,0.25)',
    stroke: 'rgba(250,204,21,0.35)',
    width: strokeWidth * 0.5
  };

  const rays = Array.from({ length: rayCount }, (_, index) => {
    const angle = (index / rayCount) * Math.PI * 2;
    const isPrimary = index % 2 === 0;
    const rayWidth = strokeWidth * (isPrimary ? 0.7 : 0.45);
    const rayLength = isPrimary ? rayOuterRadius : rayOuterRadius * 0.75;
    const start = [
      center[0] + Math.cos(angle) * rayInnerRadius,
      center[1] + Math.sin(angle) * rayInnerRadius
    ];
    const end = [
      center[0] + Math.cos(angle) * rayLength,
      center[1] + Math.sin(angle) * rayLength
    ];
    const color = isPrimary ? '#fde047' : '#fef9c3';
    return {
      type: 'line',
      from: start,
      to: end,
      stroke: color,
      width: rayWidth
    };
  });

  const core = {
    type: 'circle',
    center,
    radius,
    fill: '#fde047',
    stroke: '#facc15',
    width: strokeWidth
  };

  return [glow, rays, core];
};

// gear([0, 0], 4, 12, 0)
return (center, radius, teeth, angleAdvance) => {
  const toothSize = 0.7;
  const rawTeeth = typeof teeth === 'number' && Number.isFinite(teeth) ? teeth : 1;
  const totalTeeth = Math.max(1, Math.ceil(rawTeeth));
  const advance = angleAdvance ?? 0;
  const halfStep = Math.PI / rawTeeth;
  const teethLines = Array.from({ length: totalTeeth }, (_, index) => {
    const angle = index * ((2 * Math.PI) / rawTeeth) + advance;
    const base1 = [
      center[0] + Math.sin(angle - halfStep) * radius,
      center[1] + Math.cos(angle - halfStep) * radius
    ];
    const base2 = [
      center[0] + Math.sin(angle + halfStep) * radius,
      center[1] + Math.cos(angle + halfStep) * radius
    ];
    const apex = [
      center[0] + Math.sin(angle) * (radius + toothSize),
      center[1] + Math.cos(angle) * (radius + toothSize)
    ];
    return [
      { type: 'line', data: { from: base1, to: base2, stroke: '#38bdf8', width: 0.3 } },
      { type: 'line', data: { from: base2, to: apex, stroke: '#38bdf8', width: 0.3 } },
      { type: 'line', data: { from: apex, to: base1, stroke: '#38bdf8', width: 0.3 } }
    ];
  });
  return [
    teethLines,
    {
      type: 'circle',
      data: {
        center,
        radius,
        stroke: '#334155',
        width: 1,
        fill: '#334155'
      }
    }
  ];
};

// wheel([0, 0], 10, 2, 0)
return (center, outerRadius, innerRadius, angle) => {
  const spokeCount = 12;
  const baseAngle = angle ?? 0;
  const lines = Array.from({ length: spokeCount }, (_, index) => {
    const theta = baseAngle + index * ((Math.PI * 2) / spokeCount);
    return {
      type: 'line',
      data: {
        from: [
          center[0] + Math.sin(theta) * innerRadius,
          center[1] + Math.cos(theta) * innerRadius
        ],
        to: [
          center[0] + Math.sin(theta) * outerRadius,
          center[1] + Math.cos(theta) * outerRadius
        ],
        stroke: '#38bdf8',
        width: 0.3
      }
    };
  });
  return [
    lines,
    {
      type: 'circle',
      data: {
        center,
        radius: outerRadius,
        stroke: '#334155',
        width: 1,
        fill: null
      }
    },
    {
      type: 'circle',
      data: {
        center,
        radius: innerRadius,
        stroke: '#38bdf8',
        width: 1,
        fill: null
      }
    }
  ];
};

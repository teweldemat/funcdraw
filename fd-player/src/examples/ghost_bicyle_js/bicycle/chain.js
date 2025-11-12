return (start, end, segments, displacement) => {
  const dx = (end[0] - start[0]) / segments;
  const dy = (end[1] - start[1]) / segments;
  const length = Math.hypot(end[0] - start[0], end[1] - start[1]) || 0.0001;
  const unitsPerLength = segments / length;
  const dispUnits = displacement * unitsPerLength;
  const segmentFraction = 0.2;

  const wrap = (value) => ((value % segments) + segments) % segments;
  const point = (t) => [start[0] + dx * wrap(t), start[1] + dy * wrap(t)];

  return Array.from({ length: segments }, (_, index) => {
    const a = index + dispUnits;
    const b = a + segmentFraction;
    const aw = wrap(a);
    const bw = wrap(b);
    if (bw < aw) {
      return [];
    }
    return {
      type: 'line',
      from: point(a),
      to: point(b),
      stroke: '#fbbf24',
      width: 0.3
    };
  });
};

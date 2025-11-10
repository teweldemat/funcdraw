return (locationParam, widthParam, heightParam, shiftParam) => {
  const base = locationParam ?? [0, 0];
  const width = widthParam ?? 60;
  const height = heightParam ?? 12;
  const rawShift = shiftParam ?? 0;

  const layout = [
    { kind: 'tree', offset: 0.0, sizeRatio: 0.85 },
    { kind: 'house', houseKind: 'house', offset: 1.0, sizeRatio: 1.05 },
    { kind: 'house', houseKind: 'building', offset: 2.1, sizeRatio: 1.45 },
    { kind: 'house', houseKind: 'tower', offset: 3.25, sizeRatio: 1.6 },
    { kind: 'tree', offset: 4.3, sizeRatio: 1.0 }
  ];

  const layoutEndOffset = 4.3;
  const layoutSpan = (layoutEndOffset + 1) * height;
  const tileSpan = layoutSpan > 0 ? layoutSpan : height * 5;

  const normalizeShift = (value, span) => value - Math.floor(value / span) * span;
  const shift = normalizeShift(rawShift, tileSpan);
  const totalWidth = width + tileSpan * 2;
  const repeatCount = Math.ceil(totalWidth / tileSpan) + 1;

  const skylineElements = [];
  for (let tileIndex = 0; tileIndex < repeatCount; tileIndex += 1) {
    const tileX = base[0] + shift - tileSpan + tileIndex * tileSpan;
    for (const entry of layout) {
      const anchorX = tileX + height * entry.offset;
      const anchor = [anchorX, base[1]];
      const size = height * entry.sizeRatio;
      if (entry.kind === 'tree') {
        skylineElements.push(lib.tree(anchor, size));
      } else {
        const houseKind = entry.houseKind ?? 'house';
        skylineElements.push(lib.house(anchor, size, houseKind));
      }
    }
  }

  return skylineElements;
};

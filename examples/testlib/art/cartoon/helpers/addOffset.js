function addOffset(point, offset) {
  return [
    (point?.[0] ?? 0) + (offset?.[0] ?? 0),
    (point?.[1] ?? 0) + (offset?.[1] ?? 0)
  ];
}

return addOffset;

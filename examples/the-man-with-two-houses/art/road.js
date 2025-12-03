function road(options = {}) {
  const left = Number.isFinite(options.left) ? options.left : -400;
  const right = Number.isFinite(options.right) ? options.right : 400;
  const y = Number.isFinite(options.y) ? options.y : 0;
  const stroke = typeof options.stroke === 'string' ? options.stroke : '#94a3b8';
  const width = Number.isFinite(options.width) ? options.width : 0.5;

  return {
    type: 'line',
    from: [left, y],
    to: [right, y],
    stroke,
    width
  };
}

return road;

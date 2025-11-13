(pointParam) => {
  point: pointParam ?? { x: 0; y: 0 };
  px: point.x ?? 0;
  py: point.y ?? 0;
  eval '[' + px + ',' + py + ']';
};

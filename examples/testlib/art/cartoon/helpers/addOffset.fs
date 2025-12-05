(point, offset)=> {
  pointX:if point = null then 0 else if point[0] = null then 0 else point[0];
  pointY:if point = null then 0 else if point[1] = null then 0 else point[1];
  offsetX:if offset = null then 0 else if offset[0] = null then 0 else offset[0];
  offsetY:if offset = null then 0 else if offset[1] = null then 0 else offset[1];

  eval [pointX + offsetX, pointY + offsetY];
};

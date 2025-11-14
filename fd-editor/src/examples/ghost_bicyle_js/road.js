return (groundLineY, bounds) => {
  const roadPadding = 120;
  const roadWidth = 1.8;
  const roadOffsetBelowGround = 0.4;
  const roadLength = bounds.right - bounds.left + roadPadding * 2;
  const roadStartX = bounds.left - roadPadding;
  const roadY = groundLineY - roadOffsetBelowGround - roadWidth;

  const asphalt = {
    type: 'rect',
    position: [roadStartX, roadY],
    size: [roadLength, roadWidth],
    fill: '#111827',
    stroke: '#0f172a',
    width: 0.4
  };

  const markerLength = 8;
  const markerGap = 6;
  const markerHeight = 0.2;
  const markerStep = markerLength + markerGap;
  const markerVisibleWidth = bounds.right - bounds.left + roadPadding * 2;
  const markerCount = Math.ceil(markerVisibleWidth / markerStep) + 2;
  const markerFirstX = Math.floor((bounds.left - roadPadding) / markerStep) * markerStep;
  const laneMarkers = Array.from({ length: markerCount }, (_, index) => {
    const startX = markerFirstX + index * markerStep;
    return {
      type: 'rect',
      position: [startX, roadY + roadWidth / 2 - markerHeight / 2],
      size: [markerLength, markerHeight],
      fill: '#fef08a',
      stroke: '#fde047',
      width: 0.15
    };
  });

  return [asphalt, laneMarkers];
};

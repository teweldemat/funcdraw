(groundLineY, bounds) => {
  roadPadding: 120;
  roadWidth: 1.8;
  roadOffsetBelowGround: 0.4;
  roadLength: bounds.right - bounds.left + roadPadding * 2;
  roadStartX: bounds.left - roadPadding;
  roadY: groundLineY - roadOffsetBelowGround - roadWidth;

  asphalt: {
    type: 'rect';
    position: [roadStartX, roadY];
    size: [roadLength, roadWidth];
    fill: '#111827';
    stroke: '#0f172a';
    width: 0.4;
  };

  markerLength: 8;
  markerGap: 6;
  markerHeight: 0.2;
  markerStep: markerLength + markerGap;
  markerVisibleWidth: bounds.right - bounds.left + roadPadding * 2;
  markerCount: math.ceil(markerVisibleWidth / markerStep) + 2;
  markerFirstX: math.floor((bounds.left - roadPadding) / markerStep) * markerStep;
  laneMarkers: range(0, markerCount) map (index) => {
    startX: markerFirstX + index * markerStep;
    eval {
      type: 'rect';
      position: [startX, roadY + roadWidth / 2 - markerHeight / 2];
      size: [markerLength, markerHeight];
      fill: '#fef08a';
      stroke: '#fde047';
      width: 0.15;
    };
  };

  eval [asphalt, laneMarkers];
};

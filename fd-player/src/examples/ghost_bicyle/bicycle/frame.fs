(rear, front, gear, height, frameColorParam, accentColorParam) => {
  // geometry parameters
  slant: 0.28 * height;
  rearSlant: 0.3 * height; // rear column leans slightly forward
  steerRise: 0.2 * height;  // shortened steering column
  barGap: 2.2;

  // front slant projection helper
  xAtY:(y)=> front[0] - slant * ((y - front[1]) / height);

  // rear slant projection helper
  rearXAtY:(y)=> rear[0] + rearSlant * ((y - rear[1]) / height);

  // top coordinates
  rTopY: rear[1] + height;
  rTop: [rearXAtY(rTopY), rTopY];
  rTopLower: [rearXAtY(rTopY - barGap), rTopY - barGap];

  fTopY: front[1] + height + steerRise;
  fTop: [xAtY(fTopY), fTopY];

  fJoin: [xAtY(rTopY), rTopY];
  fJoinLower: [xAtY(rTopY - barGap), rTopY - barGap];

  frameColor: frameColorParam ?? '#9ca3af';
  accentColor: accentColorParam ?? '#6b7280';

  // seat
  seatLen: 4.5;
  seatThickness: 1.2;
  seatHalf: seatLen / 2;
  seatDrop: height * 0.08;
  seatRect: {
    type: 'rect';
    data: {
      position: [rTop[0] - seatHalf, rTop[1]];
      size: [seatLen, seatThickness];
      fill: accentColor;
      stroke: accentColor;
      width: 0.0;
    };
  };
  slantedTopLower: [rTopLower[0], rTopLower[1] - seatDrop];

  // steering handle
  handleLen: 2.6;
  handleRise: 0.8;
  handleEnd: [fTop[0] + handleLen, fTop[1] + handleRise];

  eval [
    // slanted rear column
    { type: 'line', data: { from: rear, to: rTop, stroke: frameColor, width: 0.6 } },

    // front slanted steering column
    { type: 'line', data: { from: front, to: fTop, stroke: frameColor, width: 0.6 } },

    // top horizontal bar
    { type: 'line', data: { from: slantedTopLower, to: fJoinLower, stroke: frameColor, width: 0.6 } },

    // lower triangle (gear to both ends of lower bar)
    { type: 'line', data: { from: gear, to: slantedTopLower, stroke: frameColor, width: 0.6 } },
    { type: 'line', data: { from: gear, to: fJoinLower, stroke: frameColor, width: 0.6 } },

    // lower horizontal bar
    { type: 'line', data: { from: gear, to: rear, stroke: frameColor, width: 0.6 } },

    // seat (horizontal, thicker T cap)
    seatRect,

    // steering handle
    { type: 'line', data: { from: fTop, to: handleEnd, stroke: accentColor, width: 0.5 } }
  ];
}

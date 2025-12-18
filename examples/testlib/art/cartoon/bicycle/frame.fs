(rear, front, gear, height, frameColorParam, accentColorParam) =>
{
  slant: 0.28 * height;
  rearSlant: 0.3 * height;
  steerRise: 0.2 * height;
  barGap: 2.2;

  xAtY: (y) => front[0] - slant * ((y - front[1]) / height);
  rearXAtY: (y) => rear[0] + rearSlant * ((y - rear[1]) / height);

  rTopY: rear[1] + height;
  rTop: [rearXAtY(rTopY), rTopY];
  rTopLower: [rearXAtY(rTopY - barGap), rTopY - barGap];

  fTopY: front[1] + height + steerRise;
  fTop: [xAtY(fTopY), fTopY];

  fJoinLower: [xAtY(rTopY - barGap), rTopY - barGap];

  frameColor: frameColorParam ?? "#9ca3af";
  accentColor: accentColorParam ?? "#6b7280";

  seatLen: 4.5;
  seatThickness: 1.2;
  seatHalf: seatLen / 2;
  seatDrop: height * 0.08;
  seatRect:
  {
    type: "rect";
    position: [rTop[0] - seatHalf, rTop[1]];
    size: [seatLen, seatThickness];
    fill: accentColor;
    stroke: accentColor;
    width: 0;
  };
  slantedTopLower: [rTopLower[0], rTopLower[1] - seatDrop];

  handleLen: 2.6;
  handleRise: 0.8;
  handleEnd: [fTop[0] + handleLen, fTop[1] + handleRise];

  seatCenter: [rTop[0], rTop[1] + seatThickness * 0.5];

  graphics:
  [
    { type: "line"; from: rear; to: rTop; stroke: frameColor; width: 0.6; },
    { type: "line"; from: front; to: fTop; stroke: frameColor; width: 0.6; },
    { type: "line"; from: slantedTopLower; to: fJoinLower; stroke: frameColor; width: 0.6; },
    { type: "line"; from: gear; to: slantedTopLower; stroke: frameColor; width: 0.6; },
    { type: "line"; from: gear; to: fJoinLower; stroke: frameColor; width: 0.6; },
    { type: "line"; from: gear; to: rear; stroke: frameColor; width: 0.6; },
    seatRect,
    { type: "line"; from: fTop; to: handleEnd; stroke: accentColor; width: 0.5; }
  ];

  eval
  {
    graphics;
    seat: seatCenter;
    handlebar: { from: fTop; to: handleEnd; };
  };
}

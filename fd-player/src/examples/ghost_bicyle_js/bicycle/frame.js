return (rear, front, gear, height, frameColorParam, accentColorParam) => {
  const slant = 0.28 * height;
  const rearSlant = 0.3 * height;
  const steerRise = 0.2 * height;
  const barGap = 2.2;

  const xAtY = (y) => front[0] - slant * ((y - front[1]) / height);
  const rearXAtY = (y) => rear[0] + rearSlant * ((y - rear[1]) / height);

  const rTopY = rear[1] + height;
  const rTop = [rearXAtY(rTopY), rTopY];
  const rTopLower = [rearXAtY(rTopY - barGap), rTopY - barGap];

  const fTopY = front[1] + height + steerRise;
  const fTop = [xAtY(fTopY), fTopY];

  const fJoinLower = [xAtY(rTopY - barGap), rTopY - barGap];

  const frameColor = frameColorParam ?? '#9ca3af';
  const accentColor = accentColorParam ?? '#6b7280';

  const seatLen = 4.5;
  const seatHalf = seatLen / 2;
  const seatThickness = 1.2;
  const seatDrop = height * 0.08;
  const seatRect = {
    type: 'rect',
    data: {
      position: [rTop[0] - seatHalf, rTop[1]],
      size: [seatLen, seatThickness],
      fill: accentColor,
      stroke: accentColor,
      width: 0
    }
  };
  const slantedTopLower = [rTopLower[0], rTopLower[1] - seatDrop];

  const handleLen = 2.6;
  const handleRise = 0.8;
  const handleEnd = [fTop[0] + handleLen, fTop[1] + handleRise];

  const line = (from, to, stroke, width = 0.6) => ({ type: 'line', data: { from, to, stroke, width } });

  return [
    line(rear, rTop, frameColor),
    line(front, fTop, frameColor),
    line(slantedTopLower, fJoinLower, frameColor),
    line(gear, slantedTopLower, frameColor),
    line(gear, fJoinLower, frameColor),
    line(gear, rear, frameColor),
    seatRect,
    line(fTop, handleEnd, accentColor, 0.5)
  ];
};

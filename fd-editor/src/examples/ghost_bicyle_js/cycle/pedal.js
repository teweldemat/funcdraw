return (center, angle, length, isLeft) => {
  const baseRadius = 1;
  const baseWidth = 1.2;
  const tipWidth = 0.6;
  const footWidth = 3;
  const footThickness = 0.6;
  const jointRadius = 0.4;

  const dx = Math.sin(angle);
  const dy = Math.cos(angle);
  const px = Math.cos(angle);
  const py = -Math.sin(angle);

  const baseCenter = [center[0] + dx * baseRadius, center[1] + dy * baseRadius];
  const tipCenter = [center[0] + dx * (length - footThickness / 2), center[1] + dy * (length - footThickness / 2)];
  const footCenter = tipCenter;

  const bw = baseWidth / 2;
  const tw = tipWidth / 2;

  const baseLeft = [baseCenter[0] + px * bw, baseCenter[1] + py * bw];
  const baseRight = [baseCenter[0] - px * bw, baseCenter[1] - py * bw];
  const tipLeft = [tipCenter[0] + px * tw, tipCenter[1] + py * tw];
  const tipRight = [tipCenter[0] - px * tw, tipCenter[1] - py * tw];

  const fw = footWidth / 2;
  const fh = footThickness / 2;
  const footTL = [footCenter[0] - fw, footCenter[1] - fh];
  const footTR = [footCenter[0] + fw, footCenter[1] - fh];
  const footBR = [footCenter[0] + fw, footCenter[1] + fh];
  const footBL = [footCenter[0] - fw, footCenter[1] + fh];

  const baseCircle = {
    type: 'circle',
    center,
    radius: baseRadius,
    stroke: '#60a5fa',
    width: 0.4,
    fill: '#60a5fa'
  };

  const armPolygon = {
    type: 'polygon',
    points: [baseLeft, baseRight, tipRight, tipLeft],
    stroke: '#60a5fa',
    width: 0,
    fill: '#60a5fa'
  };

  const armOutline = {
    type: 'polygon',
    points: [baseLeft, baseRight, tipRight, tipLeft, baseLeft],
    stroke: '#3b82f6',
    width: 0.2
  };

  const footRest = {
    type: 'polygon',
    points: [footTL, footTR, footBR, footBL],
    stroke: '#60a5fa',
    width: 0,
    fill: '#60a5fa'
  };

  const footOutline = {
    type: 'polygon',
    points: [footTL, footTR, footBR, footBL, footTL],
    stroke: '#3b82f6',
    width: 0.2
  };

  const jointCircle = {
    type: 'circle',
    center: tipCenter,
    radius: jointRadius,
    stroke: '#3b82f6',
    width: 0.3,
    fill: '#93c5fd'
  };

  return isLeft
    ? [baseCircle, jointCircle, footRest, footOutline, armPolygon, armOutline]
    : [baseCircle, armPolygon, armOutline, footRest, footOutline, jointCircle];
};

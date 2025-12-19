(center, angle, length, isLeft) =>
{
  baseRadius: 1.0;
  baseWidth: 1.2;
  tipWidth: 0.6;
  footWidth: 3.0;
  footThickness: 0.6;
  jointRadius: 0.4;

  dx: math.Sin(angle);
  dy: math.Cos(angle);
  px: math.Cos(angle);
  py: -math.Sin(angle);

  baseCenter: [center[0] + dx * baseRadius, center[1] + dy * baseRadius];
  tipCenter:
  [
    center[0] + dx * (length - footThickness / 2),
    center[1] + dy * (length - footThickness / 2)
  ];
  footCenter: tipCenter;

  bw: baseWidth / 2;
  tw: tipWidth / 2;

  baseLeft: [baseCenter[0] + px * bw, baseCenter[1] + py * bw];
  baseRight: [baseCenter[0] - px * bw, baseCenter[1] - py * bw];
  tipLeft: [tipCenter[0] + px * tw, tipCenter[1] + py * tw];
  tipRight: [tipCenter[0] - px * tw, tipCenter[1] - py * tw];

  fw: footWidth / 2;
  fh: footThickness / 2;
  footTL: [footCenter[0] - fw, footCenter[1] - fh];
  footTR: [footCenter[0] + fw, footCenter[1] - fh];
  footBR: [footCenter[0] + fw, footCenter[1] + fh];
  footBL: [footCenter[0] - fw, footCenter[1] + fh];

  baseCircle:
  {
    type: "circle";
    center;
    radius: baseRadius;
    stroke: "#60a5fa";
    width: 0.4;
    fill: "#60a5fa";
  };

  armPolygon:
  {
    type: "polygon";
    points: [baseLeft, baseRight, tipRight, tipLeft];
    stroke: "#60a5fa";
    width: 0;
    fill: "#60a5fa";
  };

  armOutline:
  {
    type: "polygon";
    points: [baseLeft, baseRight, tipRight, tipLeft, baseLeft];
    stroke: "#3b82f6";
    width: 0.2;
  };

  footRest:
  {
    type: "polygon";
    points: [footTL, footTR, footBR, footBL];
    stroke: "#60a5fa";
    width: 0;
    fill: "#60a5fa";
  };

  footOutline:
  {
    type: "polygon";
    points: [footTL, footTR, footBR, footBL, footTL];
    stroke: "#3b82f6";
    width: 0.2;
  };

  jointCircle:
  {
    type: "circle";
    center: tipCenter;
    radius: jointRadius;
    stroke: "#3b82f6";
    width: 0.3;
    fill: "#93c5fd";
  };

  eval
    if isLeft then
      [baseCircle, jointCircle, footRest, footOutline, armPolygon, armOutline]
    else
      [baseCircle, armPolygon, armOutline, footRest, footOutline, jointCircle];
}


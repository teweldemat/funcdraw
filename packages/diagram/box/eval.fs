
(centerParam, sizeParam, optionsParam) => {
  center: centerParam ?? [0, 0];
  resolvedSize: sizeParam ?? [10, 6];
  width: math.max(1, resolvedSize[0] ?? 10);
  height: math.max(1, resolvedSize[1] ?? 6);
  options: optionsParam ?? {};

  clamp: (value, minValue, maxValue) => math.max(minValue, math.min(maxValue, value));

  radiusInput: options.cornerRadius ?? math.min(width, height) * 0.2;
  radius: clamp(radiusInput, 0, math.min(width, height) / 2);
  diagonalStep:
    if (radius <= 0)
      then 0
      else radius / math.sqrt(2);

  fillColor: options.fill ?? '#f8fafc';
  strokeColor: options.stroke ?? '#1e293b';
  strokeWidth: options.strokeWidth ?? 0.25;

  label: options.label ?? null;
  labelColor: options.labelColor ?? strokeColor;
  labelSize: options.labelSize ?? math.min(width, height) * 0.45;
  labelOffset: options.labelOffset ?? [0, 0];
  labelOffsetValid: length(labelOffset) >= 2;
  labelPosition: if labelOffsetValid
      then [center[0] + labelOffset[0], center[1] + labelOffset[1]]
      else center;

  left: center[0] - width / 2;
  top: center[1] - height / 2;
  right: left + width;
  bottom: top + height;
  attachments: {
    left: { point: [left, center[1]] };
    right: { point: [right, center[1]] };
    top: { point: [center[0], top] };
    bottom: { point: [center[0], bottom] };
  };

  polygonPoints:
    if (radius <= 0.001)
      then [
        [left, top],
        [right, top],
        [right, bottom],
        [left, bottom]
      ]
      else [
        [left + radius, top],
        [right - radius, top],
        [right - radius + diagonalStep, top + radius - diagonalStep],
        [right, top + radius],
        [right, bottom - radius],
        [right - radius + diagonalStep, bottom - radius + diagonalStep],
        [right - radius, bottom],
        [left + radius, bottom],
        [left + radius - diagonalStep, bottom - radius + diagonalStep],
        [left, bottom - radius],
        [left, top + radius],
        [left + radius - diagonalStep, top + radius - diagonalStep]
      ];

  boxShape: {
    type: 'polygon';
    points: polygonPoints;
    fill: fillColor;
    stroke: strokeColor;
    width: strokeWidth;
  };

  labelShape:
    if (label = null)
      then []
      else [
        {
          type: 'text';
          position: labelPosition;
          text: label;
          color: labelColor;
          fontSize: labelSize;
          align: 'center';
        }
      ];

  eval {
    graphics: [boxShape, labelShape];
    attachments: attachments;
    width: width;
    height: height;
    above: height / 2;
    below: height / 2;
  };
}

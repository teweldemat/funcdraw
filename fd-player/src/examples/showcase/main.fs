// Showcase playground: tweak the parameters below and rerender to see the effects.
{
  viewBounds: {
    minX: -20;
    maxX: 20;
    minY: -15;
    maxY: 15;
  };

  // Try changing lineCount to see how the range-map spacing behaves.
  lineCount: 8;
  padding: 3;
  horizontalSpan: viewBounds.maxX - viewBounds.minX - padding * 2;
  verticalRange: viewBounds.maxY - viewBounds.minY - padding * 2;
  lineWidth: 0.35;

  // Lines are distributed with a range-map approach using index/total.
  baseColor: '#38bdf8';
  accentColor: '#f97316';

  // Range-map example: distribute horizontal lines between the padded bounds.
  horizontalLines:
    range(0, lineCount) map (index) => {
      ratio: if (lineCount <= 1) then 0 else (index * 1.0) / (lineCount - 1);
      y: viewBounds.minY + padding + ratio * verticalRange;
      strength: 0.2 + 0.8 * ratio;
      eval {
        type: 'line';
        data: {
          from: [viewBounds.minX + padding, y];
          to: [viewBounds.maxX - padding, y];
          stroke: 'rgba(56,189,248,' + strength + ')';
          width: lineWidth;
        };
      };
    };

  // A simple axis-aligned rectangle; change rectSize or rectCenter.
  rectCenter: [0, 0];
  rectSize: [horizontalSpan * 0.4, verticalRange * 0.3];
  exampleRect: {
    type: 'rect';
    data: {
      position: [rectCenter[0] - rectSize[0] / 2, rectCenter[1] - rectSize[1] / 2];
      size: rectSize;
      fill: 'rgba(15,118,110,0.15)';
      stroke: '#0f766e';
      width: 0.4;
    };
  };

  // A circle showing how radius responds to a slider-style variable.
  circleRadius: 3.5;
  exampleCircle: {
    type: 'circle';
    data: {
      center: [viewBounds.maxX - padding - circleRadius * 1.5, viewBounds.maxY - padding - circleRadius * 1.5];
      radius: circleRadius;
      fill: 'rgba(249,115,22,0.2)';
      stroke: accentColor;
      width: 0.5;
    };
  };

  // Triangular marker pointing out the origin.
  originMarker: {
    type: 'polygon';
    data: {
      points: [[-0.6, -0.6], [0.6, -0.6], [0, 1]];
      fill: baseColor;
      stroke: '#0f172a';
      width: 0.25;
    };
  };

  graphics: horizontalLines + [exampleRect, exampleCircle, originMarker];

  eval graphics;
}

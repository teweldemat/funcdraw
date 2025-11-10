(contextParam) => {
  context: contextParam ?? {};
  accent: '#f97316';
  headerParts: common.header(context, 'Precision Without Guesswork', 'Code keeps vector graphics exact.', accent);

  bulletLines: [
    'Snap typography, curves, and scales with math, not eyeballing.',
    'Version control every pixel with the rest of your codebase.',
    'Share scenes as text so reviews and diffs stay meaningful.'
  ];

  cardOrigin: context.cardOrigin ?? [-54, -29];
  contentPaddingX: context.contentPaddingX ?? 6;
  bulletStart: context.bulletStartY ?? (cardOrigin[1] + (context.cardHeight ?? 58) - 12);
  bulletSpacing: context.bulletSpacing ?? 4.5;
  dotRadius: context.dotRadius ?? 0.6;
  bulletFont: context.bulletFont ?? 10;
  dotX: cardOrigin[0] + contentPaddingX;
  textX: dotX + contentPaddingX * 0.4;

  bulletEntries:
    bulletLines map (line, index) => {
      y: bulletStart - index * bulletSpacing;
      dot: {
        type: 'circle';
        data: {
          center: [dotX, y];
          radius: dotRadius;
          fill: accent;
          stroke: 'transparent';
          width: 0;
        };
      };
      text: {
        type: 'text';
        data: {
          position: [textX, y];
          text: line;
          color: '#e2e8f0';
          fontSize: bulletFont;
          align: 'left';
        };
      };
      return [dot, text];
    };
  bulletParts: bulletEntries reduce (acc, entry) => acc + entry ~ [];

  chartBaseX: cardOrigin[0] + cardWidth - contentPaddingX * 3;
  chartBaseY: cardOrigin[1] + cardHeight * 0.45;
  barWidth: cardWidth * 0.025;
  bars: [0.4, 0.65, 0.85, 0.55] map (ratio, idx) => {
    height: cardHeight * 0.18 * ratio;
    return {
      type: 'rect';
      data: {
        position: [chartBaseX + idx * (barWidth + 6), chartBaseY - height];
        size: [barWidth, height];
        fill: 'rgba(249,115,22,' + (0.3 + ratio * 0.4) + ')';
        stroke: accent;
        width: 0.4;
      };
    };
  };

  footerParts: common.footer(context, accent);

  return headerParts + bulletParts + bars + footerParts;
}

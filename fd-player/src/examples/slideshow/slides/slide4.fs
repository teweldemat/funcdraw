(contextParam) => {
  context: contextParam ?? {};
  accent: '#22c55e';
  headerParts: common.header(context, 'Skip the GUI Labyrinth', 'Complex graphics, zero panel spelunking.', accent);

  bulletLines: [
    'Stop hunting through imperfect toolbars and hidden handles.',
    'Declarative code documents every creative decision.',
    'Generate rich slides, charts, and guides from a single script.'
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
      eval [dot, text];
    };
  bulletParts: bulletEntries reduce (acc, entry) => acc + entry ~ [];

  pathStart: [cardOrigin[0] + contentPaddingX, cardOrigin[1] + cardHeight * 0.25];
  pathMid: [pathStart[0] + cardWidth * 0.25, pathStart[1] - cardHeight * 0.08];
  pathEnd: [pathStart[0] + cardWidth * 0.55, pathStart[1] + cardHeight * 0.05];

  guideLine: {
    type: 'line';
    data: {
      from: pathStart;
      to: pathMid;
      stroke: accent;
      width: 1.2;
    };
  };
  guideLine2: {
    type: 'line';
    data: {
      from: pathMid;
      to: pathEnd;
      stroke: accent;
      width: 1.2;
    };
  };

  arrowHead: {
    type: 'polygon';
    data: {
      points: [
        pathEnd,
        [pathEnd[0] - 10, pathEnd[1] + 6],
        [pathEnd[0] - 4, pathEnd[1] - 8]
      ];
      fill: accent;
      stroke: 'transparent';
      width: 0;
    };
  };

  footerParts: common.footer(context, accent);

  eval headerParts + bulletParts + [guideLine, guideLine2, arrowHead] + footerParts;
}

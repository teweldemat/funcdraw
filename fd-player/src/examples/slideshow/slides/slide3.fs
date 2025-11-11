(contextParam) => {
  context: contextParam ?? {};
  accent: '#a855f7';
  headerParts: common.header(context, 'Fits Right Into Your AI Workflow', 'Let automation sketch; FuncDraw keeps it reliable.', accent);

  bulletLines: [
    'Drop AI-generated snippets into reusable FuncDraw modules.',
    'Deterministic functions instantly verify what the model produced.',
    'Codify layout rules so human intent survives endless iterations.'
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

  nodeCenterX: cardOrigin[0] + cardWidth - contentPaddingX * 2.5;
  nodeCenterY: cardOrigin[1] + cardHeight * 0.4;
  nodeRadius: cardWidth * 0.01;
  nodes: [
    [0, 0],
    [-40, 30],
    [50, 20],
    [-30, -35],
    [40, -25]
  ];
  nodeDots:
    nodes map (offset) => {
      eval {
        type: 'circle';
        data: {
          center: [nodeCenterX + offset[0], nodeCenterY + offset[1]];
          radius: nodeRadius;
          fill: accent;
          stroke: 'transparent';
          width: 0;
        };
      };
    };

  connections: [
    [0, 1],
    [0, 2],
    [0, 3],
    [0, 4],
    [1, 3],
    [2, 4]
  ] map (pair) => {
    from: nodeDots[pair[0]].data.center;
    to: nodeDots[pair[1]].data.center;
    eval {
      type: 'line';
      data: {
        from;
        to;
        stroke: 'rgba(168,85,247,0.5)';
        width: 0.8;
      };
    };
  };

  footerParts: common.footer(context, accent);

  eval headerParts + bulletParts + connections + nodeDots + footerParts;
}

(contextParam) => {
  context: contextParam ?? {};
  accent: '#38bdf8';
  headerParts: common.header(context, 'Welcome to FuncDraw', 'Functions become finished scenes.', accent);

  bulletLines: [
    'Compose primitives with equations instead of anchors.',
    'Parameterize everything so one tweak updates the entire illustration.',
    'Treat art, motion, and layout as reusable building blocks.'
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
        center: [dotX, y];
        radius: dotRadius;
        fill: accent;
        stroke: 'transparent';
        width: 0;
      };
      text: {
        type: 'text';
        position: [textX, y];
        text: line;
        color: '#e2e8f0';
        fontSize: bulletFont;
        align: 'left';
      };
      eval [dot, text];
    };
  bulletParts: bulletEntries reduce (acc, entry) => acc + entry ~ [];

  decorativeSun: {
    type: 'circle';
    center: [cardOrigin[0] + cardWidth - contentPaddingX * 2, cardOrigin[1] + cardHeight - contentPaddingTop * 1.2];
    radius: cardWidth * 0.08;
    fill: 'rgba(56,189,248,0.12)';
    stroke: accent;
    width: 0.8;
  };

  orbitRing: {
    type: 'circle';
    center: decorativeSun.center;
    radius: cardWidth * 0.12;
    fill: 'transparent';
    stroke: 'rgba(56,189,248,0.3)';
    width: 0.6;
  };

  footerParts: common.footer(context, accent);

  eval headerParts + bulletParts + [decorativeSun, orbitRing] + footerParts;
}

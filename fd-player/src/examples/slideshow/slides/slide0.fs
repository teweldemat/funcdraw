(contextParam) => {
  context: contextParam ?? {};
  accent: '#38bdf8';
  headerParts: common.header(context, 'FuncDraw Slide Deck', 'Design in functions, render as art.', accent);

  cardOrigin: context.cardOrigin ?? [0, 0];
  contentPaddingX: context.contentPaddingX ?? 6;
  cardWidth: context.cardWidth ?? 640;
  cardHeight: context.cardHeight ?? 480;

  titleBlock: {
    type: 'text';
    data: {
      position: [cardOrigin[0] + contentPaddingX, cardOrigin[1] + cardHeight * 0.6];
      text: 'FuncDraw';
      color: '#f8fafc';
      fontSize: cardHeight * 0.16;
      align: 'left';
    };
  };

  subtitleBlock: {
    type: 'text';
    data: {
      position: [cardOrigin[0] + contentPaddingX, cardOrigin[1] + cardHeight * 0.52];
      text: 'Code-first graphics for the AI era';
      color: '#cbd5f5';
      fontSize: cardHeight * 0.06;
      align: 'left';
    };
  };

  tagline: {
    type: 'text';
    data: {
      position: [cardOrigin[0] + contentPaddingX, cardOrigin[1] + cardHeight * 0.42];
      text: 'Slides, guides, and scenes—all from functions.';
      color: '#94a3b8';
      fontSize: cardHeight * 0.04;
      align: 'left';
    };
  };

  wavePath: {
    type: 'polygon';
    data: {
      points: [
        [cardOrigin[0] + contentPaddingX, cardOrigin[1] + cardHeight * 0.3],
        [cardOrigin[0] + cardWidth * 0.35, cardOrigin[1] + cardHeight * 0.35],
        [cardOrigin[0] + cardWidth * 0.55, cardOrigin[1] + cardHeight * 0.25],
        [cardOrigin[0] + cardWidth * 0.8, cardOrigin[1] + cardHeight * 0.32],
        [cardOrigin[0] + cardWidth - contentPaddingX, cardOrigin[1] + cardHeight * 0.28]
      ];
      fill: 'transparent';
      stroke: 'rgba(56,189,248,0.5)';
      width: 2;
    };
  };

  footerParts: common.footer(context, accent);

  eval headerParts + [titleBlock, subtitleBlock, tagline, wavePath] + footerParts;
}

(contextParam, titleParam, subtitleParam, accentParam) => {
  context: contextParam ?? {};
  title: titleParam ?? '';
  subtitle: subtitleParam ?? '';
  accent: accentParam ?? '#38bdf8';

  viewBounds: context.viewBounds ?? {
    minX: -60;
    maxX: 60;
    minY: -35;
    maxY: 35;
  };
  cardOrigin: context.cardOrigin ?? [viewBounds.minX + 6, viewBounds.minY + 6];
  cardWidth: context.cardWidth ?? (viewBounds.maxX - viewBounds.minX - 12);
  cardHeight: context.cardHeight ?? (viewBounds.maxY - viewBounds.minY - 12);
  contentPaddingX: context.contentPaddingX ?? 6;
  contentPaddingTop: context.contentPaddingTop ?? 10;
  backgroundColor: context.backgroundColor ?? '#0f172a';
  cardFill: context.cardFill ?? 'rgba(15,118,110,0.08)';
  cardStroke: context.cardStroke ?? 'rgba(255,255,255,0.08)';

  background: {
    type: 'rect';
    data: {
      position: [viewBounds.minX, viewBounds.minY];
      size: [viewBounds.maxX - viewBounds.minX, viewBounds.maxY - viewBounds.minY];
      fill: backgroundColor;
      stroke: '#1e293b';
      width: 0.6;
    };
  };

  spotlight: {
    type: 'rect';
    data: {
      position: cardOrigin;
      size: [cardWidth, cardHeight];
      fill: cardFill;
      stroke: cardStroke;
      width: 0.4;
    };
  };

  accentBar: {
    type: 'rect';
    data: {
      position: [cardOrigin[0], cardOrigin[1] + cardHeight - 1.8];
      size: [cardWidth, 1.8];
      fill: accent;
      stroke: accent;
      width: 0;
    };
  };

  titleFont: context.titleFont ?? 18;
  subtitleFont: context.subtitleFont ?? 12;
  titleSpacing: context.titleSpacing ?? 12;
  subtitleSpacing: context.subtitleSpacing ?? 8;
  titleBaseY: context.titleBaseY ?? (cardOrigin[1] + cardHeight - contentPaddingTop);
  subtitleBaseY: context.subtitleBaseY ?? (titleBaseY - subtitleSpacing);

  titleText: {
    type: 'text';
    data: {
      position: [cardOrigin[0] + contentPaddingX, titleBaseY];
      text: title;
      color: '#f8fafc';
      fontSize: titleFont;
      align: 'left';
    };
  };

  subtitleText: {
    type: 'text';
    data: {
      position: [cardOrigin[0] + contentPaddingX, subtitleBaseY];
      text: subtitle;
      color: '#cbd5f5';
      fontSize: subtitleFont;
      align: 'left';
    };
  };

  return [background, spotlight, accentBar, titleText, subtitleText];
}

(contextParam, accentParam) => {
  context: contextParam ?? {};
  accent: accentParam ?? '#38bdf8';

  viewBounds: context.viewBounds ?? {
    left: -60;
    right: 60;
    bottom: -35;
    top: 35;
  };
  cardOrigin: context.cardOrigin ?? [viewBounds.left + 6, viewBounds.bottom + 6];
  cardWidth: context.cardWidth ?? (viewBounds.right - viewBounds.left - 12);
  progressRatio: context.progressRatio ?? 0;
  slideNumber: context.slideNumber ?? 1;
  slideCount: context.slideCount ?? 1;

  labelFont: context.footerFont ?? 9;

  track: {
    type: 'rect';
    position: [cardOrigin[0] + 2, cardOrigin[1] + 1.5];
    size: [cardWidth - 4, 0.8];
    fill: 'rgba(148,163,184,0.25)';
    stroke: 'transparent';
    width: 0;
  };

  fillWidth: math.max(2, (cardWidth - 4) * progressRatio);
  fillBar: {
    type: 'rect';
    position: [cardOrigin[0] + 2, cardOrigin[1] + 1.5];
    size: [fillWidth, 0.8];
    fill: accent;
    stroke: 'transparent';
    width: 0;
  };

  label: {
    type: 'text';
    position: [cardOrigin[0] + cardWidth - 4, cardOrigin[1] + 3];
    text: 'Slide ' + slideNumber + '/' + slideCount;
    color: '#94a3b8';
    fontSize: labelFont;
    align: 'right';
  };

  eval [track, fillBar, label];
}

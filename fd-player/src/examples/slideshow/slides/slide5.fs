(contextParam) => {
  context: contextParam ?? {};
  accent: '#facc15';
  headerParts: common.header(context, 'Enjoy!', 'Thanks for exploring FuncDraw.', accent);

  cardOrigin: context.cardOrigin ?? [0, 0];
  cardWidth: context.cardWidth ?? 640;
  cardHeight: context.cardHeight ?? 480;
  contentPaddingX: context.contentPaddingX ?? 6;
  timeSeconds: context.timeSeconds ?? 0;

  wheelCenter: [cardOrigin[0] + cardWidth * 0.75, cardOrigin[1] + cardHeight * 0.32];
  wheelOuter: cardWidth * 0.12;
  wheelInner: wheelOuter * 0.35;
  wheelRaw: lib.wheel(wheelCenter, wheelOuter, wheelInner, timeSeconds);
  wheelLines: wheelRaw[0] ?? [];
  wheelRim: wheelRaw[1] ?? null;
  wheelHub: wheelRaw[2] ?? null;
  wheelParts: wheelLines + (if (wheelRim = null) then [] else [wheelRim]) + (if (wheelHub = null) then [] else [wheelHub]);

  birdBaseY: cardOrigin[1] + cardHeight * 0.55;
  birdBaseX: cardOrigin[0] + cardWidth * 0.6;
  birdScale: cardWidth * 0.015;
  birdOffsets: [
    { x: 0; y: 0; shift: 0 },
    { x: 35; y: 20; shift: 0.15 },
    { x: 60; y: -15; shift: 0.3 },
    { x: 90; y: 10; shift: 0.45 }
  ];
  birdGroups:
    birdOffsets map (entry) =>
      lib.bird(
        [birdBaseX + entry.x, birdBaseY + entry.y],
        birdScale,
        timeSeconds + entry.shift
      );
  birdParts:
    birdGroups reduce (acc, group) => acc + group ~ [];

  closingText: {
    type: 'text';
    data: {
      position: [cardOrigin[0] + cardWidth / 2, cardOrigin[1] + cardHeight * 0.38];
      text: 'Enjoy!';
      color: '#fde047';
      fontSize: cardHeight * 0.18;
      align: 'center';
    };
  };

  footerParts: common.footer(context, accent);

  return headerParts + [closingText] + wheelParts + birdParts + footerParts;
}

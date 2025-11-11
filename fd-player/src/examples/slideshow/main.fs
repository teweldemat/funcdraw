{
  viewBounds: {
    minX: 0;
    maxX: 800;
    minY: 0;
    maxY: 600;
  };

  viewWidth: viewBounds.maxX - viewBounds.minX;
  viewHeight: viewBounds.maxY - viewBounds.minY;
  cardPaddingX: viewWidth * 0.1;
  cardPaddingY: viewHeight * 0.1;
  cardWidth: viewWidth - cardPaddingX * 2;
  cardHeight: viewHeight - cardPaddingY * 2;
  cardOrigin: [viewBounds.minX + cardPaddingX, viewBounds.minY + cardPaddingY];
  contentPaddingX: cardWidth * 0.05;
  contentPaddingTop: cardHeight * 0.08;
  contentPaddingBottom: cardHeight * 0.1;
  titleSpacing: cardHeight * 0.045;
  subtitleSpacing: cardHeight * 0.055;
  titleBaseY: cardOrigin[1] + cardHeight - contentPaddingTop;
  subtitleBaseY: titleBaseY - subtitleSpacing;
  bulletSpacing: cardHeight * 0.055;
  bulletStartY: subtitleBaseY - bulletSpacing;
  slideCount: 6;

  clamp: (valueParam, minParam, maxParam) => {
    value: valueParam ?? 0;
    minValue: minParam ?? 0;
    maxValue: maxParam ?? 1;
    eval if (value < minValue)
      then minValue
      else if (value > maxValue)
        then maxValue
        else value;
  };

  slideDuration: 6;
  timeSeconds: t ?? 0;
  slideCycle: math.floor(timeSeconds / slideDuration);
  clampedIndex: clamp(slideCycle, 0, slideCount - 1);
  slideNumber: clampedIndex + 1;
  elapsed: timeSeconds - slideCycle * slideDuration;
  progressRatio: clamp(elapsed / slideDuration, 0, 1);

  context: {
    viewBounds;
    cardOrigin;
    cardWidth;
    cardHeight;
    bulletStartY;
    bulletSpacing;
    contentPaddingX;
    contentPaddingTop;
    contentPaddingBottom;
    titleSpacing;
    subtitleSpacing;
    titleBaseY;
    subtitleBaseY;
    titleFont: cardHeight * 0.055;
    subtitleFont: cardHeight * 0.032;
    bulletFont: cardHeight * 0.022;
    dotRadius: cardHeight * 0.0035;
    progressRatio;
    slideNumber;
    slideCount;
    timeSeconds;
    backgroundColor: '#0f172a';
    cardFill: 'rgba(15,118,110,0.08)';
    cardStroke: 'rgba(255,255,255,0.08)';
  };

  activeSlide:
    if (clampedIndex = 0)
      then slides.slide0
      else if (clampedIndex = 1)
        then slides.slide1
        else if (clampedIndex = 2)
          then slides.slide2
          else if (clampedIndex = 3)
            then slides.slide3
            else if (clampedIndex = 4)
              then slides.slide4
              else slides.slide5;

  result:
    if (activeSlide = null)
      then []
      else activeSlide(context);

  eval result;
}

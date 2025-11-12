(expressionParam, maxLengthParam, typeSpeedParam, timeParam) => {
  expressionText: expressionParam ?? '';
  maxExpressionLength: maxLengthParam ?? 0;
  typeSpeed: typeSpeedParam ?? 0;
  timeOffset: timeParam ?? 0;

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

  charCountRaw: length(expressionText);
  charCount: clamp(charCountRaw, 0, maxExpressionLength);
  typingDuration: if (typeSpeed > 0) then charCount / typeSpeed else 0;
  effectiveTime:
    if (typingDuration <= 0)
      then 0
      else clamp(timeOffset, 0, typingDuration);
  typedCharsRaw: math.floor(effectiveTime * typeSpeed);
  typedChars: clamp(typedCharsRaw, 0, charCount);
  typedTextValue: substring(expressionText, 0, typedChars);
  readyFlagRaw: typedChars - charCount + 1;
  readyFlag: clamp(readyFlagRaw, 0, 1);

  eval {
    charCount;
    typedChars;
    typedText: typedTextValue;
    typingDuration;
    readyFlag;
  };
};

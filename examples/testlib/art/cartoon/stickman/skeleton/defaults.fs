{
  torsoWidth:6;
  torsoHeight:11;
  legUpper:5.2;
  legLower:4.8;
  legTotal:legUpper + legLower;
  armUpper:torsoHeight * 0.45;
  armLower:legTotal * 0.4;
  shoulderExtension:torsoWidth * 0.15;
  handOffset:torsoWidth / 2 + shoulderExtension;
  handDrop:torsoHeight * 0.85 - (armUpper + armLower);
  legOffset:torsoWidth * 0.25;
  footThickness:0.5;
  positionY:legTotal + footThickness;
  frontBackFootLineLength:torsoWidth * 0.12;
  ikEpsilon:0.000001;

  defaultMeasurements:{
    torso:{ width:torsoWidth; height:torsoHeight; shoulderExtension:shoulderExtension; direction:"front" };
    head:{ verticalExtent:4.5; angle:90; direction:"front" };
    hands:{
      left:{ upperLength:armUpper; lowerLength:armLower; effectorCoordinate:[-handOffset, handDrop]; positiveBend:false };
      right:{ upperLength:armUpper; lowerLength:armLower; effectorCoordinate:[handOffset, handDrop]; positiveBend:true };
    };
    legs:{
      left:{ upperLength:legUpper; lowerLength:legLower; effectorCoordinate:[-legOffset, -legTotal]; positiveBend:true };
      right:{ upperLength:legUpper; lowerLength:legLower; effectorCoordinate:[legOffset, -legTotal]; positiveBend:true };
    };
  };
}

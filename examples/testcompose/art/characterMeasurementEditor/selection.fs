{
  singleSliderRange:
  {
    head: { min: 1; max: 6; };
    neck: { min: 0; max: 5; };
    body: { min: 8; max: 24; };
    shoulders: { min: 0; max: 6; };
    thighs: { min: 0; max: 4; };
  };

  limbRange:
  {
    min: 2;
    max: 20;
  };

  describe: (selectedPart, measurements) =>
  {
    hasSecondary:
      selectedPart == "leftArm" or
      selectedPart == "rightArm" or
      selectedPart == "leftLeg" or
      selectedPart == "rightLeg";

    primaryValue:
      if selectedPart == "head" then measurements.headRadius
      else if selectedPart == "neck" then measurements.neckLength
      else if selectedPart == "body" then measurements.height
      else if selectedPart == "shoulders" then measurements.shoulderWidth
      else if selectedPart == "thighs" then measurements.thighWidth
      else if selectedPart == "leftArm" then measurements.leftHand.upper
      else if selectedPart == "rightArm" then measurements.rightHand.upper
      else if selectedPart == "leftLeg" then measurements.leftLeg.upper
      else if selectedPart == "rightLeg" then measurements.rightLeg.upper
      else error("expected selectedPart");

    secondaryValue:
      if selectedPart == "leftArm" then measurements.leftHand.lower
      else if selectedPart == "rightArm" then measurements.rightHand.lower
      else if selectedPart == "leftLeg" then measurements.leftLeg.lower
      else if selectedPart == "rightLeg" then measurements.rightLeg.lower
      else null;

    primaryRange:
      if hasSecondary then limbRange
      else if selectedPart == "head" then singleSliderRange.head
      else if selectedPart == "neck" then singleSliderRange.neck
      else if selectedPart == "body" then singleSliderRange.body
      else if selectedPart == "shoulders" then singleSliderRange.shoulders
      else if selectedPart == "thighs" then singleSliderRange.thighs
      else error("expected selectedPart with range");

    secondaryRange: if hasSecondary then limbRange else null;

    primaryLabel:
      if selectedPart == "head" then "headRadius: " + primaryValue
      else if selectedPart == "neck" then "neckLength: " + primaryValue
      else if selectedPart == "body" then "height: " + primaryValue
      else if selectedPart == "shoulders" then "shoulderWidth: " + primaryValue
      else if selectedPart == "thighs" then "thighWidth: " + primaryValue
      else if selectedPart == "leftArm" then "left arm upper: " + primaryValue
      else if selectedPart == "rightArm" then "right arm upper: " + primaryValue
      else if selectedPart == "leftLeg" then "left leg upper: " + primaryValue
      else if selectedPart == "rightLeg" then "right leg upper: " + primaryValue
      else error("expected selectedPart label");

    secondaryLabel:
      if selectedPart == "leftArm" then "left arm lower: " + secondaryValue
      else if selectedPart == "rightArm" then "right arm lower: " + secondaryValue
      else if selectedPart == "leftLeg" then "left leg lower: " + secondaryValue
      else if selectedPart == "rightLeg" then "right leg lower: " + secondaryValue
      else null;

    bendFlipped:
      if !hasSecondary then false
      else if selectedPart == "leftArm" then measurements.leftHand.sign != -1
      else if selectedPart == "rightArm" then measurements.rightHand.sign != 1
      else if selectedPart == "leftLeg" then measurements.leftLeg.sign != -1
      else if selectedPart == "rightLeg" then measurements.rightLeg.sign != 1
      else error("expected limb selectedPart");

    eval
    {
      hasSecondary;
      primaryValue;
      secondaryValue;
      primaryRange;
      secondaryRange;
      primaryLabel;
      secondaryLabel;
      bendFlipped;
    };
  };
}

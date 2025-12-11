(anchor, measurements, movingFeet, targetFeetCoordinate, progress) =>
{
  base: defaultMeasurements + measurements;
  merged:
  {
    leftLeg: defaultMeasurements.leftLeg + base.leftLeg;
    rightLeg: defaultMeasurements.rightLeg + base.rightLeg;
  };
  m: base + merged;

  moving: if movingFeet == "left" then m.leftLeg else m.rightLeg;
  stepLift: (moving.upper + moving.lower) * 0.2;

  startWorld: [anchor[0] + moving.end[0], anchor[1] + moving.end[1]];
  targetWorld: targetFeetCoordinate;
  dx: targetWorld[0] - startWorld[0];
  dy: targetWorld[1] - startWorld[1];

  nextX: startWorld[0] + dx * progress;
  nextYLinear: startWorld[1] + dy * progress;
  nextYLifted: nextYLinear + math.Sin(progress * math.Pi) * stepLift;

  anchorShift: [dx * progress * 0.5, 0];
  shiftedAnchor: [anchor[0] + anchorShift[0], anchor[1] + anchorShift[1]];

  nextFoot: [nextX - shiftedAnchor[0], nextYLifted - shiftedAnchor[1]];

  baseLeftWorld: [anchor[0] + m.leftLeg.end[0], anchor[1] + m.leftLeg.end[1]];
  baseRightWorld: [anchor[0] + m.rightLeg.end[0], anchor[1] + m.rightLeg.end[1]];

  groundedLeft: [baseLeftWorld[0] - shiftedAnchor[0], baseLeftWorld[1] - shiftedAnchor[1]];
  groundedRight: [baseRightWorld[0] - shiftedAnchor[0], baseRightWorld[1] - shiftedAnchor[1]];

  updatedLeftLeg: if movingFeet == "left" then
    { end: nextFoot; upper: m.leftLeg.upper; lower: m.leftLeg.lower; sign: m.leftLeg.sign; }
  else
    { end: groundedLeft; upper: m.leftLeg.upper; lower: m.leftLeg.lower; sign: m.leftLeg.sign; };

  updatedRightLeg: if movingFeet == "right" then
    { end: nextFoot; upper: m.rightLeg.upper; lower: m.rightLeg.lower; sign: m.rightLeg.sign; }
  else
    { end: groundedRight; upper: m.rightLeg.upper; lower: m.rightLeg.lower; sign: m.rightLeg.sign; };

  legDiffX: updatedLeftLeg.end[0] - updatedRightLeg.end[0];

  handSwingScale: 0.6;
  leftHandX: -handSwingScale * legDiffX;
  rightHandX: handSwingScale * legDiffX;

  baseLeftY: m.leftHand.end[1];
  baseRightY: m.rightHand.end[1];

  stepPhase: (m.handPhaseOffset + progress) * math.Pi;
  verticalAmp: stepLift * 0.25;
  handLift: verticalAmp * math.Sin(stepPhase * 2);

  updatedLeftHand:
  {
    end: [leftHandX, baseLeftY + handLift];
    upper: m.leftHand.upper;
    lower: m.leftHand.lower;
    sign: m.leftHand.sign;
  };
  updatedRightHand:
  {
    end: [rightHandX, baseRightY + handLift];
    upper: m.rightHand.upper;
    lower: m.rightHand.lower;
    sign: m.rightHand.sign;
  };

  eval m + {
    anchor: shiftedAnchor;
    leftLeg: updatedLeftLeg;
    rightLeg: updatedRightLeg;
    leftHand: updatedLeftHand;
    rightHand: updatedRightHand;
  };
}
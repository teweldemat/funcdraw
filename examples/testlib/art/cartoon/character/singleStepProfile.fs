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

  armLengthLeft: m.leftHand.upper + m.leftHand.lower;
  armLengthRight: m.rightHand.upper + m.rightHand.lower;

  baseLeftX: m.leftHand.end[0];
  baseRightX: m.rightHand.end[0];
  baseLeftY: m.leftHand.end[1];
  baseRightY: m.rightHand.end[1];

  baseSpan: math.Abs(baseRightX - baseLeftX);
  handSwing: baseSpan * 0.8;

  phase: (m.handPhaseOffset + progress) * math.Pi;
  swing: math.Sin(phase);
  liftPhase: math.Sin(phase * 2);
  handLift: handSwing * 0.15 * liftPhase;

  updatedLeftHand:
  {
    end: [-handSwing * swing, baseLeftY + handLift];
    upper: m.leftHand.upper;
    lower: m.leftHand.lower;
    sign: m.leftHand.sign;
  };
  updatedRightHand:
  {
    end: [handSwing * swing, baseRightY + handLift];
    upper: m.rightHand.upper;
    lower: m.rightHand.lower;
    sign: m.rightHand.sign;
  };

  updatedLeftLeg: if movingFeet == "left" then
    { end: nextFoot; upper: m.leftLeg.upper; lower: m.leftLeg.lower; sign: m.leftLeg.sign; }
  else
    { end: groundedLeft; upper: m.leftLeg.upper; lower: m.leftLeg.lower; sign: m.leftLeg.sign; };

  updatedRightLeg: if movingFeet == "right" then
    { end: nextFoot; upper: m.rightLeg.upper; lower: m.rightLeg.lower; sign: m.rightLeg.sign; }
  else
    { end: groundedRight; upper: m.rightLeg.upper; lower: m.rightLeg.lower; sign: m.rightLeg.sign; };

  eval m + { anchor: shiftedAnchor; leftLeg: updatedLeftLeg; rightLeg: updatedRightLeg; leftHand: updatedLeftHand; rightHand: updatedRightHand; };
}
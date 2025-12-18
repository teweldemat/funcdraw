(position, measurements, verticalDistance, strideLength, progress, zoomFactor) =>
{
  base: defaultMeasurements + measurements;
  merged:
  {
    leftHand: defaultMeasurements.leftHand + base.leftHand;
    rightHand: defaultMeasurements.rightHand + base.rightHand;
    leftLeg: defaultMeasurements.leftLeg + base.leftLeg;
    rightLeg: defaultMeasurements.rightLeg + base.rightLeg;
  };
  m0: base + merged;

  strideAbs: math.Abs(strideLength);
  distanceAbs: math.Abs(verticalDistance);
  sign: math.Sign(verticalDistance);

  legTotal0: (m0.leftLeg.upper + m0.leftLeg.lower + m0.rightLeg.upper + m0.rightLeg.lower) / 2;
  defaultLegTotal: (defaultMeasurements.leftLeg.upper + defaultMeasurements.leftLeg.lower + defaultMeasurements.rightLeg.upper + defaultMeasurements.rightLeg.lower) / 2;
  legScale0: legTotal0 / defaultLegTotal;
  stepLen: strideAbs * legScale0;

  scaleLimbToEnd: (limb, end) =>
  {
    total: limb.upper + limb.lower;
    distance: math.Sqrt(end[0] * end[0] + end[1] * end[1]);
    factor: if total <= 0 then 1 else distance / total;
    eval limb + { end: end; upper: limb.upper * factor; lower: limb.lower * factor; sign: limb.sign; };
  };

  eval if strideAbs <= 0 then error("expected strideLength > 0") else
  if progress < 0 or progress > 1 then error("expected progress 0..1") else
  if zoomFactor <= 0 then error("expected zoomFactor > 0") else
  if distanceAbs == 0 then m0 + { anchor: position; } else
  {
    meanLegEndY0: (m0.leftLeg.end[1] + m0.rightLeg.end[1]) / 2;
    midY0: position[1] + meanLegEndY0;
    midY: midY0 + verticalDistance * progress;
    traveled: distanceAbs * progress;

    scale: math.Exp(-sign * zoomFactor * traveled);

    scaled:
      m0 + {
        height: m0.height * scale;
        neckLength: m0.neckLength * scale;
        headRadius: m0.headRadius * scale;
        shoulderWidth: m0.shoulderWidth * scale;
        thighWidth: m0.thighWidth * scale;
        leftHand: m0.leftHand + { upper: m0.leftHand.upper * scale; lower: m0.leftHand.lower * scale; };
        rightHand: m0.rightHand + { upper: m0.rightHand.upper * scale; lower: m0.rightHand.lower * scale; };
        leftLeg: m0.leftLeg + { upper: m0.leftLeg.upper * scale; lower: m0.leftLeg.lower * scale; };
        rightLeg: m0.rightLeg + { upper: m0.rightLeg.upper * scale; lower: m0.rightLeg.lower * scale; };
      };

    meanLegEndY: meanLegEndY0 * scale;
    anchorY: midY - meanLegEndY;
    anchor: [position[0], anchorY];

    spread: if scaled.direction == "front" then 1
      else if scaled.direction == "back" then 1
      else if scaled.direction == "left" then 0
      else if scaled.direction == "right" then 0
      else error("expected direction left|right|front|back");
    bodyDir: [math.Cos(scaled.bodyAngle), math.Sin(scaled.bodyAngle)];
    perpendicular: [-bodyDir[1], bodyDir[0]];
    thighSpread: scaled.thighWidth * spread;
    leftLegAttachY: anchorY + perpendicular[1] * thighSpread;
    rightLegAttachY: anchorY - perpendicular[1] * thighSpread;

    // Stepping model (O(1)):
    // - Define a fixed step length in traveled-midpoint space (`stepLen`).
    // - Within each step, one foot is planted (constant world Y), the other moves so the midpoint
    //   remains linear in `progress`.
    phase: if stepLen <= 0 then 0 else traveled / stepLen;
    stepIndex: math.Floor(phase);
    pairIndex: math.Floor(stepIndex / 2);
    isEvenStep: pairIndex * 2 == stepIndex;

    leftBaseY: midY0 - (stepLen * sign) / 2;
    rightBaseY: midY0 + (stepLen * sign) / 2;
    rightFixedY: rightBaseY + pairIndex * 2 * stepLen * sign;
    leftFixedY: leftBaseY + math.Floor((stepIndex + 1) / 2) * 2 * stepLen * sign;

    legs:
      if progress == 1 then
      {
        worldY: midY;
        leftEndY: worldY - leftLegAttachY;
        rightEndY: worldY - rightLegAttachY;
        eval { leftEnd: [0, leftEndY]; rightEnd: [0, rightEndY]; };
      }
      else if isEvenStep then
      {
        rightWorldY: rightFixedY;
        leftWorldY: 2 * midY - rightWorldY;
        leftEndY: leftWorldY - leftLegAttachY;
        rightEndY: rightWorldY - rightLegAttachY;
        eval { leftEnd: [0, leftEndY]; rightEnd: [0, rightEndY]; };
      }
      else
      {
        leftWorldY: leftFixedY;
        rightWorldY: 2 * midY - leftWorldY;
        leftEndY: leftWorldY - leftLegAttachY;
        rightEndY: rightWorldY - rightLegAttachY;
        eval { leftEnd: [0, leftEndY]; rightEnd: [0, rightEndY]; };
      };

    updatedLeftLeg: scaleLimbToEnd(scaled.leftLeg, legs.leftEnd);
    updatedRightLeg: scaleLimbToEnd(scaled.rightLeg, legs.rightEnd);

    legDiffY: updatedLeftLeg.end[1] - updatedRightLeg.end[1];

    baseHandY0: (m0.leftHand.end[1] + m0.rightHand.end[1]) / 2;
    baseHandY: baseHandY0 * scale;
    leftHandEndY: baseHandY - legDiffY / 2;
    rightHandEndY: baseHandY + legDiffY / 2;

    hands:
      if progress == 1 then
      {
        endY: baseHandY;
        eval { leftEnd: [0, endY]; rightEnd: [0, endY]; };
      }
      else
      {
        eval { leftEnd: [0, leftHandEndY]; rightEnd: [0, rightHandEndY]; };
      };

    updatedLeftHand: scaleLimbToEnd(scaled.leftHand, hands.leftEnd);
    updatedRightHand: scaleLimbToEnd(scaled.rightHand, hands.rightEnd);

    eval scaled + {
      anchor;
      leftLeg: updatedLeftLeg;
      rightLeg: updatedRightLeg;
      leftHand: updatedLeftHand;
      rightHand: updatedRightHand;
    };
  };
}

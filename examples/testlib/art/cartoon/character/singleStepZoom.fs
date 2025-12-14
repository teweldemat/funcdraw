(anchor, measurements, movingFeet, targetFeetY, progress, zoomFactor) =>
{
  base: defaultMeasurements + measurements;
  merged:
  {
    leftHand: defaultMeasurements.leftHand + base.leftHand;
    rightHand: defaultMeasurements.rightHand + base.rightHand;
    leftLeg: defaultMeasurements.leftLeg + base.leftLeg;
    rightLeg: defaultMeasurements.rightLeg + base.rightLeg;
  };
  m: base + merged;

  eval if zoomFactor <= 0 then error("expected zoomFactor > 0") else
  {
    bodyDir: [math.Cos(m.bodyAngle), math.Sin(m.bodyAngle)];
    perpendicular: [-bodyDir[1], bodyDir[0]];
    spread: if m.direction == "front" then 1
      else if m.direction == "back" then 1
      else if m.direction == "left" then 0
      else if m.direction == "right" then 0
      else error("expected direction left|right|front|back");

    thighSpread: m.thighWidth * spread;
    leftAttach0: [anchor[0] + perpendicular[0] * thighSpread, anchor[1] + perpendicular[1] * thighSpread];
    rightAttach0: [anchor[0] - perpendicular[0] * thighSpread, anchor[1] - perpendicular[1] * thighSpread];

    startMovingY: if movingFeet == "left" then leftAttach0[1] + m.leftLeg.end[1]
      else if movingFeet == "right" then rightAttach0[1] + m.rightLeg.end[1]
      else error("expected movingFeet left|right");

    dy: targetFeetY - startMovingY;
    movingFootWorldY: startMovingY + dy * progress;

    bodyShiftY: dy * progress * 0.5;
    shiftedAnchor: [anchor[0], anchor[1] + bodyShiftY];

    scale: 1 - bodyShiftY * zoomFactor;

    scaled:
      m + {
        height: m.height * scale;
        neckLength: m.neckLength * scale;
        headRadius: m.headRadius * scale;
        shoulderWidth: m.shoulderWidth * scale;
        thighWidth: m.thighWidth * scale;
        leftHand: m.leftHand + { upper: m.leftHand.upper * scale; lower: m.leftHand.lower * scale; };
        rightHand: m.rightHand + { upper: m.rightHand.upper * scale; lower: m.rightHand.lower * scale; };
        leftLeg: m.leftLeg + { upper: m.leftLeg.upper * scale; lower: m.leftLeg.lower * scale; };
        rightLeg: m.rightLeg + { upper: m.rightLeg.upper * scale; lower: m.rightLeg.lower * scale; };
      };

    baseLeftWorldY: leftAttach0[1] + m.leftLeg.end[1];
    baseRightWorldY: rightAttach0[1] + m.rightLeg.end[1];

    desiredLeftY: if movingFeet == "left" then movingFootWorldY else baseLeftWorldY;
    desiredRightY: if movingFeet == "right" then movingFootWorldY else baseRightWorldY;

    thighSpreadScaled: scaled.thighWidth * spread;
    leftAttach: [shiftedAnchor[0] + perpendicular[0] * thighSpreadScaled, shiftedAnchor[1] + perpendicular[1] * thighSpreadScaled];
    rightAttach: [shiftedAnchor[0] - perpendicular[0] * thighSpreadScaled, shiftedAnchor[1] - perpendicular[1] * thighSpreadScaled];

    leftEndY: desiredLeftY - leftAttach[1];
    rightEndY: desiredRightY - rightAttach[1];

    leftEnd: [0, leftEndY];
    rightEnd: [0, rightEndY];

    leftLegBaseTotal: scaled.leftLeg.upper + scaled.leftLeg.lower;
    leftLegUpper: math.Abs(leftEndY) * scaled.leftLeg.upper / leftLegBaseTotal;
    leftLegLower: math.Abs(leftEndY) - leftLegUpper;

    rightLegBaseTotal: scaled.rightLeg.upper + scaled.rightLeg.lower;
    rightLegUpper: math.Abs(rightEndY) * scaled.rightLeg.upper / rightLegBaseTotal;
    rightLegLower: math.Abs(rightEndY) - rightLegUpper;

    updatedLeftLeg:
    {
      end: leftEnd;
      upper: leftLegUpper;
      lower: leftLegLower;
      sign: scaled.leftLeg.sign;
    };
    updatedRightLeg:
    {
      end: rightEnd;
      upper: rightLegUpper;
      lower: rightLegLower;
      sign: scaled.rightLeg.sign;
    };

    legDiffY: updatedLeftLeg.end[1] - updatedRightLeg.end[1];
    legDiffStart: m.leftLeg.end[1] - m.rightLeg.end[1];

    handSwingScale: 0.6;
    restLeftHandY: m.leftHand.end[1] + handSwingScale * legDiffStart;
    restRightHandY: m.rightHand.end[1] - handSwingScale * legDiffStart;
    baseLeftHandY: restLeftHandY * scale;
    baseRightHandY: restRightHandY * scale;
    leftHandEndY: baseLeftHandY - handSwingScale * legDiffY;
    rightHandEndY: baseRightHandY + handSwingScale * legDiffY;

    leftHandBaseTotal: scaled.leftHand.upper + scaled.leftHand.lower;
    leftHandUpper: math.Abs(leftHandEndY) * scaled.leftHand.upper / leftHandBaseTotal;
    leftHandLower: math.Abs(leftHandEndY) - leftHandUpper;

    rightHandBaseTotal: scaled.rightHand.upper + scaled.rightHand.lower;
    rightHandUpper: math.Abs(rightHandEndY) * scaled.rightHand.upper / rightHandBaseTotal;
    rightHandLower: math.Abs(rightHandEndY) - rightHandUpper;

    updatedLeftHand:
    {
      end: [0, leftHandEndY];
      upper: leftHandUpper;
      lower: leftHandLower;
      sign: scaled.leftHand.sign;
    };
    updatedRightHand:
    {
      end: [0, rightHandEndY];
      upper: rightHandUpper;
      lower: rightHandLower;
      sign: scaled.rightHand.sign;
    };

    eval scaled + {
      anchor: shiftedAnchor;
      leftLeg: updatedLeftLeg;
      rightLeg: updatedRightLeg;
      leftHand: updatedLeftHand;
      rightHand: updatedRightHand;
    };
  };
}

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
    scaleLimbToEnd: (limb, end) =>
    {
      total: limb.upper + limb.lower;
      distance: math.Sqrt(end[0] * end[0] + end[1] * end[1]);
      factor: distance / total;
      eval limb + { end: end; upper: limb.upper * factor; lower: limb.lower * factor; };
    };

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

    updatedLeftLeg: scaleLimbToEnd(scaled.leftLeg, leftEnd);
    updatedRightLeg: scaleLimbToEnd(scaled.rightLeg, rightEnd);

    legDiffY: updatedLeftLeg.end[1] - updatedRightLeg.end[1];
    legDiffStart: m.leftLeg.end[1] - m.rightLeg.end[1];

    handSwingScale: 0.6;
    restLeftHandY: m.leftHand.end[1] + handSwingScale * legDiffStart;
    restRightHandY: m.rightHand.end[1] - handSwingScale * legDiffStart;
    baseLeftHandY: restLeftHandY * scale;
    baseRightHandY: restRightHandY * scale;
    leftHandEndY: baseLeftHandY - handSwingScale * legDiffY;
    rightHandEndY: baseRightHandY + handSwingScale * legDiffY;

    updatedLeftHand: scaleLimbToEnd(scaled.leftHand, [0, leftHandEndY]);
    updatedRightHand: scaleLimbToEnd(scaled.rightHand, [0, rightHandEndY]);

    eval scaled + {
      anchor: shiftedAnchor;
      leftLeg: updatedLeftLeg;
      rightLeg: updatedRightLeg;
      leftHand: updatedLeftHand;
      rightHand: updatedRightHand;
    };
  };
}

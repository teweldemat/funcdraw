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

  attachmentsY: (anchor, profile) =>
  {
    bodyDir: [math.Cos(profile.bodyAngle), math.Sin(profile.bodyAngle)];
    perpendicular: [-bodyDir[1], bodyDir[0]];
    spread: if profile.direction == "front" then 1
      else if profile.direction == "back" then 1
      else if profile.direction == "left" then 0
      else if profile.direction == "right" then 0
      else error("expected direction left|right|front|back");

    thighSpread: profile.thighWidth * spread;
    eval
    {
      left: anchor[1] + perpendicular[1] * thighSpread;
      right: anchor[1] - perpendicular[1] * thighSpread;
    };
  };

  footWorldY: (anchor, profile, whichFeet) =>
  {
    attach: attachmentsY(anchor, profile);
    eval if whichFeet == "left" then attach.left + profile.leftLeg.end[1]
    else if whichFeet == "right" then attach.right + profile.rightLeg.end[1]
    else error("expected whichFeet left|right");
  };

  eval if strideAbs <= 0 then error("expected strideLength > 0") else
  if progress < 0 or progress > 1 then error("expected progress 0..1") else
  if zoomFactor <= 0 then error("expected zoomFactor > 0") else
  if distanceAbs == 0 then m0 + { anchor: position; } else
  {
    gaitSeedProfile:
    {
      attach0: attachmentsY(position, m0);
      leftY0: attach0.left + m0.leftLeg.end[1];
      rightY0: attach0.right + m0.rightLeg.end[1];
      midY: (leftY0 + rightY0) / 2;
      legTotal0: (m0.leftLeg.upper + m0.leftLeg.lower + m0.rightLeg.upper + m0.rightLeg.lower) / 2;
      defaultLegTotal: (defaultMeasurements.leftLeg.upper + defaultMeasurements.leftLeg.lower + defaultMeasurements.rightLeg.upper + defaultMeasurements.rightLeg.lower) / 2;
      legScale0: legTotal0 / defaultLegTotal;
      diff: strideAbs * legScale0 * sign;
      desiredLeftY: midY - diff / 2;
      desiredRightY: midY + diff / 2;
      leftEndY: desiredLeftY - attach0.left;
      rightEndY: desiredRightY - attach0.right;
      eval
        m0 + {
          leftLeg: m0.leftLeg + { end: [0, leftEndY]; };
          rightLeg: m0.rightLeg + { end: [0, rightEndY]; };
      };
    };

    traveled: distanceAbs * progress;

    otherFeet: (feet) =>
      if feet == "left" then "right"
      else if feet == "right" then "left"
      else error("expected feet left|right");

    stepParams: (state, moving) =>
    {
      fixed: otherFeet(moving);
      movingY: footWorldY(state.anchor, state.profile, moving);
      fixedY: footWorldY(state.anchor, state.profile, fixed);
      diff: math.Abs(fixedY - movingY);

      a: sign * diff * zoomFactor / 2;
      stepScale: (1 - a) / (1 + a);
      nextDiff: diff * stepScale;
      targetY: fixedY + nextDiff * sign;
      stepAdvance: (diff + nextDiff) / 2;

      eval { targetY; stepAdvance; };
    };

    seedProfile: singleStepZoom(position, gaitSeedProfile, "left", 0, 0, zoomFactor);
    seed:
    {
      anchor: seedProfile.anchor;
      profile: seedProfile;
    };

    maxSteps: math.Ceiling(distanceAbs / strideAbs) * 20 + 20;
    walked:
      Range(0, maxSteps) reduce (acc, k) =>
      {
        eval if acc.remaining == 0 then acc else
        {
          isEven: (k div 2) * 2 == k;
          moving: if isEven then "left" else "right";
          params: stepParams(acc.state, moving);

          eval if acc.remaining >= params.stepAdvance then
          {
            nextProfile: singleStepZoom(acc.state.anchor, acc.state.profile, moving, params.targetY, 1, zoomFactor);
            eval
            {
              state: { anchor: nextProfile.anchor; profile: nextProfile; };
              remaining: acc.remaining - params.stepAdvance;
            };
          }
          else
          {
            localProgress: acc.remaining / params.stepAdvance;
            partialProfile: singleStepZoom(acc.state.anchor, acc.state.profile, moving, params.targetY, localProgress, zoomFactor);
            eval
            {
              state: { anchor: partialProfile.anchor; profile: partialProfile; };
              remaining: 0;
            };
          };
        };
      }
      ~ { state: seed; remaining: traveled; };

    finalProfile: walked.state.profile;

    eval if progress == 1 then
    {
      scaleLimbToEnd: (limb, end) =>
      {
        total: limb.upper + limb.lower;
        distance: math.Sqrt(end[0] * end[0] + end[1] * end[1]);
        factor: distance / total;
        eval limb + { end: end; upper: limb.upper * factor; lower: limb.lower * factor; };
      };

      legEndY: (finalProfile.leftLeg.end[1] + finalProfile.rightLeg.end[1]) / 2;
      handEndY: (finalProfile.leftHand.end[1] + finalProfile.rightHand.end[1]) / 2;

      eval finalProfile + {
        leftLeg: scaleLimbToEnd(finalProfile.leftLeg, [0, legEndY]);
        rightLeg: scaleLimbToEnd(finalProfile.rightLeg, [0, legEndY]);
        leftHand: scaleLimbToEnd(finalProfile.leftHand, [0, handEndY]);
        rightHand: scaleLimbToEnd(finalProfile.rightHand, [0, handEndY]);
      };
    }
    else finalProfile;
  };
}

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

  stepOnce: (state, k) =>
  {
    isEven: (k div 2) * 2 == k;
    moving: if isEven then "left" else "right";
    startY: footWorldY(state.anchor, state.profile, moving);
    targetY: startY + 2 * strideAbs * sign;
    nextProfile: singleStepZoom(state.anchor, state.profile, moving, targetY, 1, zoomFactor);
    eval { anchor: nextProfile.anchor; profile: nextProfile; };
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
      diff: strideAbs * sign;
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

    seed:
    {
      anchor: position;
      profile: gaitSeedProfile;
    };

    fullSteps: math.Floor(distanceAbs / strideAbs);
    remainder: distanceAbs - fullSteps * strideAbs;

    traveled: distanceAbs * progress;
    stepIndex: math.Floor(traveled / strideAbs);

    completed:
      Range(0, stepIndex) reduce (state, k) =>
        stepOnce(state, k)
      ~ seed;

    eval if remainder == 0 and stepIndex == fullSteps then completed.profile else
    {
      stepAdvance: if stepIndex < fullSteps then strideAbs else remainder;
      localDistance: traveled - stepIndex * strideAbs;
      localProgress: localDistance / stepAdvance;

      isEvenStep: math.Floor(stepIndex / 2) * 2 == stepIndex;
      movingNow: if isEvenStep then "left" else "right";
      startY: footWorldY(completed.anchor, completed.profile, movingNow);
      targetY: startY + 2 * stepAdvance * sign;

      eval singleStepZoom(completed.anchor, completed.profile, movingNow, targetY, localProgress, zoomFactor);
    };
  };
}

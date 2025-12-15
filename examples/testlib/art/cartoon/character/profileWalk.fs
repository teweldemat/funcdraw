(position, measurements, horizontalDistance, strideLength, progress) =>
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
  distanceAbs: math.Abs(horizontalDistance);
  sign: math.Sign(horizontalDistance);

  seed:
  {
    anchor: position;
    profile: m0;
  };

  stepOnce: (state, k) =>
  {
    isEven: (k div 2) * 2 == k;
    moving: if isEven then "left" else "right";
    movingLeg: if moving == "left" then state.profile.leftLeg else state.profile.rightLeg;
    startWorld: [state.anchor[0] + movingLeg.end[0], state.anchor[1] + movingLeg.end[1]];
    target: [startWorld[0] + 2 * strideAbs * sign, startWorld[1]];
    nextProfile: singleStepProfile(state.anchor, state.profile, moving, target, 1);
    eval { anchor: nextProfile.anchor; profile: nextProfile; };
  };

  eval if strideAbs <= 0 then error("expected strideLength > 0") else
  if progress < 0 or progress > 1 then error("expected progress 0..1") else
  if distanceAbs == 0 then m0 + { anchor: position; } else
  {
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
      movingLegNow: if movingNow == "left" then completed.profile.leftLeg else completed.profile.rightLeg;
      startWorldNow: [completed.anchor[0] + movingLegNow.end[0], completed.anchor[1] + movingLegNow.end[1]];
      targetNow: [startWorldNow[0] + 2 * stepAdvance * sign, startWorldNow[1]];

      eval singleStepProfile(completed.anchor, completed.profile, movingNow, targetNow, localProgress);
    };
  };
}

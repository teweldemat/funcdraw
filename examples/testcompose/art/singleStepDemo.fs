{
  defaultMeasurements:
  {
    height: 16;
    leftHand: { end: [-7, -12]; upper: 8; lower: 8; sign: -1; };
    rightHand: { end: [7, -12]; upper: 8; lower: 8; sign: 1; };
    leftLeg: { end: [-3, -14]; upper: 8; lower: 8; sign: -1; };
    rightLeg: { end: [3, -14]; upper: 8; lower: 8; sign: 1; };
    neckLength: 1.5;
    headRadius: 2.5;
    bodyAngle: math.Pi / 2;
    neckAngle: math.Pi / 2;
  };
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  startAnchor: [0, 0];
  baseFeet:
  {
    leftLeg: { end: [-3, -14]; };
    rightLeg: { end: [3, -14]; };
  };
  moving: "left";
  progress: (1+math.Sin(t-math.pi/2)) / 2;
  targetFoot: [startAnchor[0] - 8, startAnchor[1] - 14];

  initialLeftWorld: [startAnchor[0] + baseFeet.leftLeg.end[0], startAnchor[1] + baseFeet.leftLeg.end[1]];
  initialRightWorld: [startAnchor[0] + baseFeet.rightLeg.end[0], startAnchor[1] + baseFeet.rightLeg.end[1]];
  initialGap: initialRightWorld[0] - initialLeftWorld[0];
  target1: targetFoot;
  target2: [initialRightWorld[0] + initialGap, initialRightWorld[1]];
  target3: initialLeftWorld;

  stepPhase: (anchorIn, leftEnd, rightEnd, target, p) =>
  {
    moving: defaultMeasurements.leftLeg;
    stepLift: (moving.upper + moving.lower) * 0.2;
    startLeftWorld: [anchorIn[0] + leftEnd[0], anchorIn[1] + leftEnd[1]];
    startRightWorld: [anchorIn[0] + rightEnd[0], anchorIn[1] + rightEnd[1]];
    dx: target[0] - startLeftWorld[0];
    dy: target[1] - startLeftWorld[1];
    nextX: startLeftWorld[0] + dx * p;
    nextY: startLeftWorld[1] + dy * p + math.Sin(p * math.Pi) * stepLift;
    anchorShift: [dx * p * 0.5, 0];
    anchorOut: [anchorIn[0] + anchorShift[0], anchorIn[1]];
    leftRel: [nextX - anchorOut[0], nextY - anchorOut[1]];
    rightRel: [startRightWorld[0] - anchorOut[0], startRightWorld[1] - anchorOut[1]];

    eval defaultMeasurements +
    {
      anchor: anchorOut;
      leftLeg: { end: leftRel; upper: moving.upper; lower: moving.lower; sign: moving.sign; };
      rightLeg: { end: rightRel; upper: defaultMeasurements.rightLeg.upper; lower: defaultMeasurements.rightLeg.lower; sign: defaultMeasurements.rightLeg.sign; };
    };
  };

  cycle: t % 3;
  phase: if cycle < 1 then 1 else if cycle < 2 then 2 else 3;
  phaseProgressRaw: if phase == 1 then cycle else if phase == 2 then cycle - 1 else cycle - 2;
  phaseProgress: (1 - math.Cos(phaseProgressRaw * math.Pi)) / 2;

  phase1Progress: if phase == 1 then phaseProgress else 1;
  phase1: stepPhase(startAnchor, baseFeet.leftLeg.end, baseFeet.rightLeg.end, target1, phase1Progress);

  phase2Progress: if phase == 2 then phaseProgress else if phase == 1 then 0 else 1;
  phase2: stepPhase(phase1.anchor, phase1.leftLeg.end, phase1.rightLeg.end, target2, phase2Progress);

  phase3Progress: if phase == 3 then phaseProgress else 0;
  phase3: stepPhase(phase2.anchor, phase2.leftLeg.end, phase2.rightLeg.end, target3, phase3Progress);

  profile: if phase == 1 then phase1 else if phase == 2 then phase2 else phase3;
  leftHandOffset: [defaultMeasurements.leftHand.end[0] - defaultMeasurements.leftLeg.end[0], defaultMeasurements.leftHand.end[1] - defaultMeasurements.leftLeg.end[1]];
  rightHandOffset: [defaultMeasurements.rightHand.end[0] - defaultMeasurements.rightLeg.end[0], defaultMeasurements.rightHand.end[1] - defaultMeasurements.rightLeg.end[1]];
  leftHandTarget: [profile.leftLeg.end[0] + leftHandOffset[0], profile.leftLeg.end[1] + leftHandOffset[1]];
  rightHandTarget: [profile.rightLeg.end[0] + rightHandOffset[0], profile.rightLeg.end[1] + rightHandOffset[1]];
  leftHandLen: math.Sqrt(leftHandTarget[0] * leftHandTarget[0] + leftHandTarget[1] * leftHandTarget[1]);
  rightHandLen: math.Sqrt(rightHandTarget[0] * rightHandTarget[0] + rightHandTarget[1] * rightHandTarget[1]);
  handLowerScale: 0.0001;
  animatedProfile: profile +
  {
    leftHand:
    {
      end: leftHandTarget;
      upper: leftHandLen;
      lower: leftHandLen * handLowerScale;
      sign: defaultMeasurements.leftHand.sign;
    };
    rightHand:
    {
      end: rightHandTarget;
      upper: rightHandLen;
      lower: rightHandLen * handLowerScale;
      sign: defaultMeasurements.rightHand.sign;
    };
  };
  character: package("@funcdraw/testlib").cartoon.character.static(animatedProfile.anchor, animatedProfile, palette);

  startMarker:
  {
    type: "circle";
    center: [startAnchor[0] + baseFeet.leftLeg.end[0], startAnchor[1] + baseFeet.leftLeg.end[1]];
    radius: 0.4;
    stroke: "#ef4444";
    width: 0.2;
  };

  endMarker:
  {
    type: "circle";
    center: targetFoot;
    radius: 0.4;
    stroke: "#22c55e";
    width: 0.2;
  };

  eval
  {
    valueHooks: { t: t };
    view:
    {
      left: -20;
      bottom: -22;
      right: 22;
      top: 26;
    };
    graphics: character + [startMarker, endMarker];
  };
}

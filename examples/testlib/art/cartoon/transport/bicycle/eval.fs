(rearWheelCenterParam, wheelRadiusParam, wheelTurnAngleParam, frameColorParam, frameAccentColorParam, directionParam) =>
{
  rearWheelCenter: rearWheelCenterParam ?? [0, 0];
  baseWheelRadius: 9;
  wheelRadius: wheelRadiusParam ?? baseWheelRadius;
  scaleFactor: wheelRadius / baseWheelRadius;
  wheelTurnAngle: wheelTurnAngleParam ?? 0;
  direction: directionParam ?? "right";
  dirMul: if direction == "right" then 1 else if direction == "left" then -1 else error("expected direction left|right");

  innerRadius: 1 * scaleFactor;
  wheelToWheel: 25 * scaleFactor;
  gearRadius: 2 * scaleFactor;
  gearTeeth: 12;
  gearRatio: 0.6;
  pedalOrbitRadius: gearRadius * 2.5 - 0.3;

  wheelAngle: wheelTurnAngle * dirMul;
  pedalAngle: wheelAngle * gearRatio;

  leftWheelCenter: rearWheelCenter;
  rightWheelCenter: [rearWheelCenter[0] + wheelToWheel * dirMul, rearWheelCenter[1]];
  frontGearCenter: [rearWheelCenter[0] + (wheelToWheel / 2) * dirMul, rearWheelCenter[1]];

  frameHeight: wheelRadius * 1.6;
  frameColor: frameColorParam ?? "#9ca3af";
  frameAccentColor: frameAccentColorParam ?? "#6b7280";

  leftWheel: cartoon.machine.parts.wheel(leftWheelCenter, wheelRadius, innerRadius, wheelAngle);
  rightWheel: cartoon.machine.parts.wheel(rightWheelCenter, wheelRadius, innerRadius, wheelAngle);

  drivetrain: drive(frontGearCenter, leftWheelCenter, gearRadius, gearTeeth, gearRatio, pedalAngle);

  frameResult: frame(leftWheelCenter, rightWheelCenter, frontGearCenter, frameHeight, frameColor, frameAccentColor);

  // Layers for rider/vehicle occlusion: far pedal behind legs, frame between legs, near pedal in front.
  // NOTE: `pedal1`/`pedal2` are already graphics lists, so keep them flat (no nested lists).
  pedalFarGraphics: drivetrain.pedal1;
  pedalNearGraphics: drivetrain.pedal2;
  pedalFarCenter: drivetrain.pedal1Center;
  pedalNearCenter: drivetrain.pedal2Center;

  betweenGraphics:
    leftWheel
    + rightWheel
    + drivetrain.gear2
    + drivetrain.gear1
    + drivetrain.chain1
    + drivetrain.chain2
    + frameResult.graphics;

  graphics:
    pedalFarGraphics
    + betweenGraphics
    + pedalNearGraphics;

  eval
  {
    graphics;
    pedalAngle;
    wheelAngle;
    direction;
    dirMul;
    rearWheelCenter;
    frontWheelCenter: rightWheelCenter;
    leftWheelCenter;
    rightWheelCenter;
    frontGearCenter;
    frameHeight;
    wheelRadius;
    wheelBase: wheelToWheel;
    pedalOrbitRadius;
	  layers:
	    {
	      back: pedalFarGraphics;
	      between: betweenGraphics;
	      front: pedalNearGraphics;
	    };
    attachments:
    {
      seat: frameResult.seat;
      handlebar: frameResult.handlebar;
      // `near`/`far` follow draw order: the near pedal is drawn last.
      pedals:
      {
        a: drivetrain.pedal1Center;
        b: drivetrain.pedal2Center;
        far: pedalFarCenter;
        near: pedalNearCenter;
      };
    };
  };
}

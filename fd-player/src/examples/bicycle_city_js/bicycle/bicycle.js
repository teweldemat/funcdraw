return (rearWheelCenterParam, wheelRadiusParam, wheelTurnAngleParam, frameColorParam, frameAccentColorParam) => {
  const fsDiv = (numerator, denominator) =>
    Number.isInteger(numerator) && Number.isInteger(denominator)
      ? Math.trunc(numerator / denominator)
      : numerator / denominator;
  const rearWheelCenter = rearWheelCenterParam ?? [0, 0];
  const baseWheelRadius = 9;
  const wheelRadius = wheelRadiusParam ?? baseWheelRadius;
  const scaleFactor = fsDiv(wheelRadius, baseWheelRadius);
  const wheelTurnAngle = wheelTurnAngleParam ?? 0;

  const innerRadius = 1 * scaleFactor;
  const wheelToWheel = 25 * scaleFactor;
  const gearRadius = 2 * scaleFactor;
  const gearTeeth = 12;
  const gearRatio = 0.6;

  const wheelAngle = wheelTurnAngle;
  const pedalAngle = wheelAngle * gearRatio;

  const leftWheelCenter = rearWheelCenter;
  const rightWheelCenter = [rearWheelCenter[0] + wheelToWheel, rearWheelCenter[1]];
  const frontGearCenter = [rearWheelCenter[0] + fsDiv(wheelToWheel, 2), rearWheelCenter[1]];

  const frameHeight = wheelRadius * 1.6;
  const frameColor = frameColorParam ?? '#9ca3af';
  const frameAccentColor = frameAccentColorParam ?? '#6b7280';

  const leftWheel = lib.wheel(leftWheelCenter, wheelRadius, innerRadius, wheelAngle);
  const rightWheel = lib.wheel(rightWheelCenter, wheelRadius, innerRadius, wheelAngle);

  const drivetrain = drive(
    frontGearCenter,
    leftWheelCenter,
    gearRadius,
    gearTeeth,
    gearRatio,
    pedalAngle
  );

  const bikeFrame = frame(
    leftWheelCenter,
    rightWheelCenter,
    frontGearCenter,
    frameHeight,
    frameColor,
    frameAccentColor
  );

  const graphics = [
    drivetrain.pedal1,
    leftWheel,
    rightWheel,
    drivetrain.gear2,
    drivetrain.gear1,
    drivetrain.chain1,
    drivetrain.chain2,
    bikeFrame,
    drivetrain.pedal2
  ];

  return {
    graphics,
    pedalAngle,
    wheelAngle,
    leftWheelCenter,
    rightWheelCenter,
    frontGearCenter,
    frameHeight,
    wheelRadius,
    wheelBase: wheelToWheel
  };
};

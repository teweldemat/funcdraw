(frontCenter, rearCenter, frontRadius, frontTeeth, ratio, pedalAngle) =>
{
  pedalLength: frontRadius * 2.5;
  pedalFootThickness: 0.6;
  pedalFootCenterRadius: pedalLength - pedalFootThickness / 2;
  chainOffsetRatio: 0.4;
  chainOffset: frontRadius * chainOffsetRatio;
  rearChainOffset: (frontRadius * ratio) * chainOffsetRatio;

  rearTeethRaw: frontTeeth * ratio;
  rearTeeth: math.Max(3, math.Floor(rearTeethRaw));

  gear1: cartoon.machine.parts.gear(frontCenter, frontRadius, frontTeeth, pedalAngle);
  gear2: cartoon.machine.parts.gear(rearCenter, frontRadius * ratio, rearTeeth, pedalAngle / ratio);

  chain1:
    chain(
      [frontCenter[0], frontCenter[1] + frontRadius + chainOffset],
      [rearCenter[0], rearCenter[1] + (frontRadius * ratio) + rearChainOffset],
      10,
      -pedalAngle * frontRadius);

  chain2:
    chain(
      [frontCenter[0], frontCenter[1] - (frontRadius + chainOffset)],
      [rearCenter[0], rearCenter[1] - ((frontRadius * ratio) + rearChainOffset)],
      10,
      pedalAngle * frontRadius);

  pedal1: pedal(frontCenter, pedalAngle, pedalLength, true);
  pedal2: pedal(frontCenter, pedalAngle + math.Pi, pedalLength, false);
  pedal1Center:
  [
    frontCenter[0] + math.Sin(pedalAngle) * pedalFootCenterRadius,
    frontCenter[1] + math.Cos(pedalAngle) * pedalFootCenterRadius
  ];
  pedal2Center:
  [
    frontCenter[0] + math.Sin(pedalAngle + math.Pi) * pedalFootCenterRadius,
    frontCenter[1] + math.Cos(pedalAngle + math.Pi) * pedalFootCenterRadius
  ];

  eval
  {
    chain1;
    chain2;
    pedal1;
    pedal1Center;
    gear1;
    pedal2;
    pedal2Center;
    gear2;
  };
}

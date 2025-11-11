return (frontCenter, rearCenter, frontRadius, frontTeeth, ratio, pedalAngle) => {
  const pedalLength = frontRadius * 2.5;
  const chainOffsetRatio = 0.4;
  const chainOffset = frontRadius * chainOffsetRatio;
  const rearChainOffset = frontRadius * ratio * chainOffsetRatio;

  const gear1 = lib.gear(frontCenter, frontRadius, frontTeeth, pedalAngle);
  const gear2 = lib.gear(rearCenter, frontRadius * ratio, frontTeeth * ratio, pedalAngle / ratio);

  const chain1 = chain(
    [frontCenter[0], frontCenter[1] + frontRadius + chainOffset],
    [rearCenter[0], rearCenter[1] + frontRadius * ratio + rearChainOffset],
    10,
    -pedalAngle * frontRadius
  );
  const chain2 = chain(
    [frontCenter[0], frontCenter[1] - (frontRadius + chainOffset)],
    [rearCenter[0], rearCenter[1] - (frontRadius * ratio + rearChainOffset)],
    10,
    pedalAngle * frontRadius
  );

  const pedal1 = cycle.pedal(frontCenter, pedalAngle, pedalLength, true);
  const pedal2 = cycle.pedal(frontCenter, pedalAngle + Math.PI, pedalLength, false);

  return {
    chain1,
    chain2,
    pedal1,
    gear1,
    pedal2,
    gear2
  };
};

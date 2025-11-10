return (sizeParam, buttockLocationParam, foot1LocationParam, foot2LocationParam, torsoTiltParam, hand1LocationParam, hand2LocationParam) => {
  const scale = sizeParam ?? 1;
  const butt = buttockLocationParam ?? [0, 0];
  const desiredFoot1 = foot1LocationParam ?? [butt[0] - scale * 0.8, butt[1] - scale * 3];
  const desiredFoot2 = foot2LocationParam ?? [butt[0] + scale * 0.8, butt[1] - scale * 3];
  const torsoTilt = torsoTiltParam ?? 0;
  const desiredHand1 = hand1LocationParam ?? [butt[0] - scale * 1.4, butt[1] + scale * 2.5];
  const desiredHand2 = hand2LocationParam ?? [butt[0] + scale * 1.4, butt[1] + scale * 2.5];

  const clamp = (value, minValue, maxValue) => Math.min(Math.max(value, minValue), maxValue);
  const vectorLength = (vec) => Math.hypot(vec[0], vec[1]);

  const solveLimb = (origin, target, upperLen, lowerLen, bendSign) => {
    const delta = [target[0] - origin[0], target[1] - origin[1]];
    const distance = vectorLength(delta);
    const safeDistance = distance === 0 ? 0.0001 : distance;
    const minReach = Math.abs(upperLen - lowerLen) + 0.0001;
    const maxReach = upperLen + lowerLen - 0.0001;
    const clampedDistance = clamp(safeDistance, minReach, maxReach);
    const dir = [delta[0] / safeDistance, delta[1] / safeDistance];
    const projected = (upperLen * upperLen - lowerLen * lowerLen + clampedDistance * clampedDistance) /
      (2 * clampedDistance);
    const heightSq = upperLen * upperLen - projected * projected;
    const height = heightSq > 0 ? Math.sqrt(heightSq) : 0;
    const perp = [-dir[1] * bendSign, dir[0] * bendSign];
    const joint = [
      origin[0] + dir[0] * projected + perp[0] * height,
      origin[1] + dir[1] * projected + perp[1] * height
    ];
    const effector = [
      origin[0] + dir[0] * clampedDistance,
      origin[1] + dir[1] * clampedDistance
    ];
    return { joint, effector };
  };

  const torsoLength = scale * 4.2;
  const shoulders = [
    butt[0] + Math.sin(torsoTilt) * torsoLength,
    butt[1] + Math.cos(torsoTilt) * torsoLength
  ];
  const headRadius = scale * 0.9;
  const headCenter = [shoulders[0], shoulders[1] + headRadius * 1.5];

  const legUpperLen = scale * 2.1;
  const legLowerLen = scale * 2.3;
  const armUpperLen = scale * 1.6;
  const armLowerLen = scale * 1.5;

  const leg1Solution = solveLimb(butt, desiredFoot1, legUpperLen, legLowerLen, -1);
  const leg2Solution = solveLimb(butt, desiredFoot2, legUpperLen, legLowerLen, 1);
  const arm1Solution = solveLimb(shoulders, desiredHand1, armUpperLen, armLowerLen, -1);
  const arm2Solution = solveLimb(shoulders, desiredHand2, armUpperLen, armLowerLen, 1);

  const limbStroke = '#0f172a';
  const legWidth = 0.35 * scale;
  const armWidth = 0.3 * scale;

  const line = (from, to, width) => ({ type: 'line', data: { from, to, stroke: limbStroke, width } });

  const torso = line(butt, shoulders, 0.45 * scale);
  const leg1Upper = line(butt, leg1Solution.joint, legWidth);
  const leg1Lower = line(leg1Solution.joint, leg1Solution.effector, legWidth);
  const leg2Upper = line(butt, leg2Solution.joint, legWidth);
  const leg2Lower = line(leg2Solution.joint, leg2Solution.effector, legWidth);
  const arm1Upper = line(shoulders, arm1Solution.joint, armWidth);
  const arm1Lower = line(arm1Solution.joint, arm1Solution.effector, armWidth);
  const arm2Upper = line(shoulders, arm2Solution.joint, armWidth);
  const arm2Lower = line(arm2Solution.joint, arm2Solution.effector, armWidth);

  const head = {
    type: 'circle',
    data: {
      center: headCenter,
      radius: headRadius,
      stroke: limbStroke,
      width: 0.35 * scale,
      fill: 'rgba(148,163,184,0.45)'
    }
  };

  return [
    leg1Upper,
    leg1Lower,
    leg2Upper,
    leg2Lower,
    torso,
    arm1Upper,
    arm1Lower,
    arm2Upper,
    arm2Lower,
    head
  ];
};

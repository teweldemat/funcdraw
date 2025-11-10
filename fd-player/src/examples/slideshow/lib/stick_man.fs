(sizeParam, buttockLocationParam, foot1LocationParam, foot2LocationParam, torsoTiltParam, hand1LocationParam, hand2LocationParam) => {
  scale: sizeParam ?? 1;
  butt: buttockLocationParam ?? [0, 0];
  desiredFoot1: foot1LocationParam ?? [butt[0] - scale * 0.8, butt[1] - scale * 3];
  desiredFoot2: foot2LocationParam ?? [butt[0] + scale * 0.8, butt[1] - scale * 3];
  torsoTilt: torsoTiltParam ?? 0;
  desiredHand1: hand1LocationParam ?? [butt[0] - scale * 1.4, butt[1] + scale * 2.5];
  desiredHand2: hand2LocationParam ?? [butt[0] + scale * 1.4, butt[1] + scale * 2.5];

  clamp: (value, minValue, maxValue) => {
    eval if (value < minValue) then minValue else if (value > maxValue) then maxValue else value;
  };

  vectorLength: (vec) => math.sqrt(vec[0] * vec[0] + vec[1] * vec[1]);

  solveLimb: (origin, target, upperLen, lowerLen, bendSign) => {
    delta: [target[0] - origin[0], target[1] - origin[1]];
    distance: vectorLength(delta);
    safeDistance: if (distance = 0) then 0.0001 else distance;
    minReach: math.abs(upperLen - lowerLen) + 0.0001;
    maxReach: upperLen + lowerLen - 0.0001;
    clampedDistance: clamp(safeDistance, minReach, maxReach);
    dir: [delta[0] / safeDistance, delta[1] / safeDistance];
    projected: (upperLen * upperLen - lowerLen * lowerLen + clampedDistance * clampedDistance) / (2 * clampedDistance);
    heightSq: upperLen * upperLen - projected * projected;
    height: if (heightSq > 0) then math.sqrt(heightSq) else 0;
    perp: [-dir[1] * bendSign, dir[0] * bendSign];
    joint: [
      origin[0] + dir[0] * projected + perp[0] * height,
      origin[1] + dir[1] * projected + perp[1] * height
    ];
    effector: [
      origin[0] + dir[0] * clampedDistance,
      origin[1] + dir[1] * clampedDistance
    ];
    eval { joint; effector };
  };

  torsoLength: scale * 4.2;
  shoulders: [
    butt[0] + math.sin(torsoTilt) * torsoLength,
    butt[1] + math.cos(torsoTilt) * torsoLength
  ];
  headRadius: scale * 0.9;
  headCenter: [shoulders[0], shoulders[1] + headRadius * 1.5];

  legUpperLen: scale * 2.1;
  legLowerLen: scale * 2.3;
  armUpperLen: scale * 1.6;
  armLowerLen: scale * 1.5;

  leg1Solution: solveLimb(butt, desiredFoot1, legUpperLen, legLowerLen, -1);
  leg2Solution: solveLimb(butt, desiredFoot2, legUpperLen, legLowerLen, 1);
  arm1Solution: solveLimb(shoulders, desiredHand1, armUpperLen, armLowerLen, -1);
  arm2Solution: solveLimb(shoulders, desiredHand2, armUpperLen, armLowerLen, 1);

  limbStroke: '#0f172a';
  legWidth: 0.35 * scale;
  armWidth: 0.3 * scale;

  leg1Upper: { type: 'line'; data: { from: butt; to: leg1Solution.joint; stroke: limbStroke; width: legWidth; }; };
  leg1Lower: { type: 'line'; data: { from: leg1Solution.joint; to: leg1Solution.effector; stroke: limbStroke; width: legWidth; }; };
  leg2Upper: { type: 'line'; data: { from: butt; to: leg2Solution.joint; stroke: limbStroke; width: legWidth; }; };
  leg2Lower: { type: 'line'; data: { from: leg2Solution.joint; to: leg2Solution.effector; stroke: limbStroke; width: legWidth; }; };

  torso: { type: 'line'; data: { from: butt; to: shoulders; stroke: limbStroke; width: 0.45 * scale; }; };

  arm1Upper: { type: 'line'; data: { from: shoulders; to: arm1Solution.joint; stroke: limbStroke; width: armWidth; }; };
  arm1Lower: { type: 'line'; data: { from: arm1Solution.joint; to: arm1Solution.effector; stroke: limbStroke; width: armWidth; }; };
  arm2Upper: { type: 'line'; data: { from: shoulders; to: arm2Solution.joint; stroke: limbStroke; width: armWidth; }; };
  arm2Lower: { type: 'line'; data: { from: arm2Solution.joint; to: arm2Solution.effector; stroke: limbStroke; width: armWidth; }; };

  head: {
    type: 'circle';
    data: {
      center: headCenter;
      radius: headRadius;
      stroke: limbStroke;
      width: 0.35 * scale;
      fill: 'rgba(148,163,184,0.45)';
    };
  };

  eval [
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

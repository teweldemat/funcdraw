// bird([x, y], bodyWidth, flapTime)
return (locationParam, sizeParam, flapTParam) => {
  const location = locationParam ?? [0, 0];
  const baseBodyWidth = 4.9;
  const targetBodyWidth = sizeParam ?? baseBodyWidth;
  const scale = targetBodyWidth / baseBodyWidth;
  const timeValue = flapTParam ?? 0;

  const lerp = (a, b, u) => a + (b - a) * u;
  const easeInCubic = (u) => u * u * u;
  const easeOutCubic = (u) => 1 - Math.pow(1 - u, 3);
  const rotate = (vector, angle) => {
    const c = Math.cos(angle);
    const s = Math.sin(angle);
    return [vector[0] * c - vector[1] * s, vector[0] * s + vector[1] * c];
  };

  const flapHz = 2.1;
  const flapPhase = timeValue * flapHz;
  const flapU = flapPhase - Math.floor(flapPhase);
  const downFrac = 0.36;
  const flapState =
    flapU < downFrac
      ? easeOutCubic(flapU / downFrac)
      : 1 - easeInCubic((flapU - downFrac) / (1 - downFrac));

  const wingspan = 9.2 * scale;
  const wingAngle = lerp(-0.95, 0.6, flapState);
  const wingLift = (flapState - 0.5) * 5.2 * scale;
  const bodyPitch = lerp(0.12, -0.06, flapState);
  const tailFan = lerp(0.5, 1.0, 1 - flapState);

  const hoverOffset = Math.sin(timeValue * 0.6) * scale * 0.8;
  const bodyLength = 4.4 * scale;
  const bodyCenter = [location[0], location[1] + hoverOffset];
  const nose = [
    bodyCenter[0] + (bodyLength / 2) * Math.cos(bodyPitch),
    bodyCenter[1] + (bodyLength / 2) * Math.sin(bodyPitch)
  ];
  const tailRoot = [
    bodyCenter[0] - (bodyLength / 2) * Math.cos(bodyPitch),
    bodyCenter[1] - (bodyLength / 2) * Math.sin(bodyPitch)
  ];

  const bodyRadius = targetBodyWidth / 2;
  const body = {
    type: 'circle',
    data: {
      center: bodyCenter,
      radius: bodyRadius,
      fill: '#e2e8f0',
      stroke: '#0f172a',
      width: 0.3
    }
  };
  const belly = {
    type: 'circle',
    data: {
      center: [bodyCenter[0] - 0.35 * scale, bodyCenter[1] + 0.65 * scale],
      radius: bodyRadius * 0.7,
      fill: '#f8fafc',
      stroke: 'transparent',
      width: 0
    }
  };
  const head = {
    type: 'circle',
    data: {
      center: [nose[0] + 0.15 * scale, nose[1] - 0.12 * scale],
      radius: 1.08 * scale,
      fill: '#f8fafc',
      stroke: '#0f172a',
      width: 0.3
    }
  };

  const blinkHz = 0.25;
  const blinkPhase = timeValue * blinkHz;
  const blinkU = blinkPhase - Math.floor(blinkPhase);
  const eyeOpen = blinkU < 0.02 ? 0.2 : 1;
  const eye = {
    type: 'circle',
    data: {
      center: [nose[0] - 0.2 * scale, nose[1] - 0.32 * scale],
      radius: 0.2 * eyeOpen * scale,
      fill: '#0f172a',
      stroke: '#0f172a',
      width: 0.1
    }
  };
  const beak = {
    type: 'polygon',
    data: {
      points: [
        [nose[0], nose[1]],
        [nose[0] + 0.85 * scale, nose[1] + 0.2 * scale],
        [nose[0] + 0.6 * scale, nose[1] - 0.1 * scale]
      ],
      fill: '#fbbf24',
      stroke: '#92400e',
      width: 0.2
    }
  };

  const wingFill = '#cbd5f5';
  const wingTipFill = '#e6ecff';
  const wingStroke = '#1e293b';

  const buildWing = (anchor, direction) => {
    const dirAngle = wingAngle * direction + (direction === -1 ? 0.22 : -0.22);
    const baseHalf = 1 * scale;
    const spanVec = rotate([direction * wingspan, wingLift * direction + 1.3 * scale], dirAngle);
    const baseUp = rotate([0, baseHalf], dirAngle);
    const baseDown = rotate([0, -baseHalf], dirAngle);
    const tip = [anchor[0] + spanVec[0], anchor[1] + spanVec[1]];

    const mainTri = {
      type: 'polygon',
      data: {
        points: [
          [anchor[0] + baseUp[0], anchor[1] + baseUp[1]],
          [anchor[0] + baseDown[0], anchor[1] + baseDown[1]],
          tip
        ],
        fill: wingFill,
        stroke: wingStroke,
        width: 0.25
      }
    };

    const tipLen = wingspan * 0.35;
    const tipBaseHalf = baseHalf * 0.6;
    const tipSpan = rotate([direction * tipLen, wingLift * direction + 0.2 * scale], dirAngle);
    const tipUp = rotate([0, tipBaseHalf], dirAngle);
    const tipDown = rotate([0, -tipBaseHalf], dirAngle);
    const tipPoint = [tip[0] + tipSpan[0], tip[1] + tipSpan[1]];

    const tipTri = {
      type: 'polygon',
      data: {
        points: [
          [tip[0] + tipUp[0], tip[1] + tipUp[1]],
          [tip[0] + tipDown[0], tip[1] + tipDown[1]],
          tipPoint
        ],
        fill: wingTipFill,
        stroke: wingStroke,
        width: 0.22
      }
    };

    return { mainTri, tipTri };
  };

  const leftWingAnchor = [bodyCenter[0] - 0.55 * scale, bodyCenter[1] + 0.22 * scale];
  const rightWingAnchor = [bodyCenter[0] + 0.55 * scale, bodyCenter[1] - 0.22 * scale];
  const leftWing = buildWing(leftWingAnchor, -1);
  const rightWing = buildWing(rightWingAnchor, 1);

  const tailSpread = 0.6 * tailFan * scale;
  const tail = {
    type: 'polygon',
    data: {
      points: [
        [tailRoot[0] - 0.35 * scale, tailRoot[1] + 0.28 * scale],
        [tailRoot[0] - (1.2 * scale + tailSpread), tailRoot[1] + (1.4 * scale + 0.2 * tailFan * scale)],
        [tailRoot[0] - 0.65 * scale, tailRoot[1] + 0.45 * scale],
        [tailRoot[0] - (1.1 * scale + tailSpread), tailRoot[1] - (0.2 * scale + 0.15 * tailFan * scale)]
      ],
      fill: '#94a3b8',
      stroke: '#0f172a',
      width: 0.25
    }
  };

  return [
    rightWing.tipTri,
    rightWing.mainTri,
    tail,
    body,
    belly,
    head,
    beak,
    eye,
    leftWing.mainTri,
    leftWing.tipTri
  ];
};

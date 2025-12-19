{
  character: package("@funcdraw/testlib").cartoon.character;
  transport: package("@funcdraw/testlib").cartoon.transport;
  bicycle: transport.bicycle;

  // Rider measurements (side-view pose).
  defaults: character.skeleton.defaults;
  buildMeasurements: (cfg) =>
  {
    s: cfg.s ?? 0.7;
    torsoScale: cfg.torsoScale ?? 0.85;
    armLenScale: cfg.armLenScale ?? 0.95;
    legLenScale: cfg.legLenScale ?? 1.95;
    bodyAngle: cfg.bodyAngle ?? 1.12;
    neckAngle: cfg.neckAngle ?? 1.22;
    shoulderMul: cfg.shoulderMul ?? 1.45;
    thighMul: cfg.thighMul ?? 1;
    neckMul: cfg.neckMul ?? 0.8;
    headMul: cfg.headMul ?? 0.9;

    scaleLimbLen: (limb, lengthScale) =>
    {
      upper: limb.upper * lengthScale;
      lower: limb.lower * lengthScale;
      total: (limb.upper + limb.lower) * lengthScale;
      end: [0, -total * 0.9];
      sign: limb.sign;
    };

    pose:
    {
      direction: "right";
      leftLeg: { sign: 1; };
      rightLeg: { sign: 1; };
      leftHand: { sign: -1; };
      rightHand: { sign: -1; };
    };

    eval
      pose
      + {
        height: defaults.height * s * torsoScale;
        leftHand: scaleLimbLen(defaults.leftHand, s * armLenScale);
        rightHand: scaleLimbLen(defaults.rightHand, s * armLenScale);
        leftLeg: scaleLimbLen(defaults.leftLeg, s * legLenScale);
        rightLeg: scaleLimbLen(defaults.rightLeg, s * legLenScale);
        neckLength: defaults.neckLength * s * neckMul;
        headRadius: defaults.headRadius * s * headMul;

        bodyAngle;
        neckAngle;
        handPhaseOffset: defaults.handPhaseOffset;
        shoulderWidth: defaults.shoulderWidth * s * shoulderMul;
        thighWidth: defaults.thighWidth * s * thighMul;
      };
  };

  dist: (a, b) =>
  {
    dx: b[0] - a[0];
    dy: b[1] - a[1];
    eval math.Sqrt(dx * dx + dy * dy);
  };

  bendSignTo: (origin, target, desiredDir) =>
  {
    dx: target[0] - origin[0];
    dy: target[1] - origin[1];
    len: math.Sqrt(dx * dx + dy * dy);
    eval if len <= 0 then 1 else
    {
      dir: [dx / len, dy / len];
      perp: [-dir[1], dir[0]];
      dot: perp[0] * desiredDir[0] + perp[1] * desiredDir[1];
      eval if dot >= 0 then 1 else -1;
    };
  };

  palette:
  {
    body: "#38bdf8";
    limb: "#fbbf24";
  };

  groundY: 0;

  // Bike parameters.
  wheelRadius: 9;
  wheelAngle: t * 3.5;

  // Two riders: stick + poly, each with its own bike.
  makeRider: (baseAnchorX, riderCfg, skinFactory) =>
  {
    rearWheelCenter: [baseAnchorX, groundY + wheelRadius];
    bike: bicycle(rearWheelCenter, wheelRadius, wheelAngle, "#9ca3af", "#6b7280");

    // Anchor: sit position (pelvis/root).
    anchor:
      [
        bike.attachments.seat[0] + (riderCfg.seatOffsetX ?? 0),
        bike.attachments.seat[1] + (riderCfg.seatOffsetY ?? 0)
      ];
    baseMeasurements: buildMeasurements(riderCfg);
    kneeBendDir: riderCfg.kneeBendDir ?? [1, 0];
    elbowBendDir: riderCfg.elbowBendDir ?? [-0.2, -1];

    // Target attachments.
    pedalNear: bike.attachments.pedals.near;
    pedalFar: bike.attachments.pedals.far;
    barA: bike.attachments.handlebar.from;
    barB: bike.attachments.handlebar.to;

    baseGeometry: character.skeleton.build(anchor, baseMeasurements);

    // Attach the near pedal to the front (near) leg, based on draw order.
    leftPedalTarget: if baseGeometry.direction == "left" then pedalNear else pedalFar;
    rightPedalTarget: if baseGeometry.direction == "left" then pedalFar else pedalNear;
    leftLegEnd:
      [leftPedalTarget[0] - baseGeometry.leftLegAttachment[0], leftPedalTarget[1] - baseGeometry.leftLegAttachment[1]];
    rightLegEnd:
      [rightPedalTarget[0] - baseGeometry.rightLegAttachment[0], rightPedalTarget[1] - baseGeometry.rightLegAttachment[1]];
    leftHandEnd: [barA[0] - baseGeometry.leftHandAttachment[0], barA[1] - baseGeometry.leftHandAttachment[1]];
    rightHandEnd: [barB[0] - baseGeometry.rightHandAttachment[0], barB[1] - baseGeometry.rightHandAttachment[1]];

    leftKneeSign: bendSignTo(baseGeometry.leftLegAttachment, leftPedalTarget, kneeBendDir);
    rightKneeSign: bendSignTo(baseGeometry.rightLegAttachment, rightPedalTarget, kneeBendDir);
    leftElbowSign: bendSignTo(baseGeometry.leftHandAttachment, barA, elbowBendDir);
    rightElbowSign: bendSignTo(baseGeometry.rightHandAttachment, barB, elbowBendDir);

    // Keep limb lengths constant; make them long enough to reach the farthest pedal distance plus a little slack.
    legSlackMul: riderCfg.legSlackMul ?? 1.05;
    armSlackMul: riderCfg.armSlackMul ?? 1.03;
    legMaxDist:
      math.Max(
        dist(baseGeometry.leftLegAttachment, bike.frontGearCenter) + bike.pedalOrbitRadius,
        dist(baseGeometry.rightLegAttachment, bike.frontGearCenter) + bike.pedalOrbitRadius
      ) * legSlackMul;
    armMaxDist:
      math.Max(
        dist(baseGeometry.leftHandAttachment, barA),
        dist(baseGeometry.rightHandAttachment, barB)
      ) * armSlackMul;
    legUpper: legMaxDist / 2;
    legLower: legMaxDist / 2;
    armUpper: armMaxDist / 2;
    armLower: armMaxDist / 2;

    riderMeasurements:
      baseMeasurements
      + {
        leftLeg:
          baseMeasurements.leftLeg
          + { upper: legUpper; lower: legLower; total: legMaxDist; end: leftLegEnd; sign: leftKneeSign; };
        rightLeg:
          baseMeasurements.rightLeg
          + { upper: legUpper; lower: legLower; total: legMaxDist; end: rightLegEnd; sign: rightKneeSign; };
        leftHand:
          baseMeasurements.leftHand
          + { upper: armUpper; lower: armLower; total: armMaxDist; end: leftHandEnd; sign: leftElbowSign; };
        rightHand:
          baseMeasurements.rightHand
          + { upper: armUpper; lower: armLower; total: armMaxDist; end: rightHandEnd; sign: rightElbowSign; };
      };

    skin:
      skinFactory(
        {
          back: bike.layers.back;
          between: bike.layers.between;
          front: bike.layers.front;
        });
    rider: character.static(anchor, riderMeasurements, palette, skin);
    eval rider;
  };

  stickRider:
    makeRider(
      -55,
      {
        s: 0.68;
        torsoScale: 0.82;
        armLenScale: 1.2;
        legLenScale: 1.4;
        bodyAngle: 1.25;
        neckAngle: 1.35;
        seatOffsetX: 1.0;
        seatOffsetY: -0.4;
        armReach01: 0.88;
        legReach01: 0.86;
        kneeBendDir: [1, 0];
        elbowBendDir: [-0.3, -1];
      },
      character.skins.stickRider);

  polyRider:
    makeRider(
      10,
      {
        s: 0.72;
        torsoScale: 0.8;
        armLenScale: 1.15;
        legLenScale: 1.5;
        bodyAngle: 1.22;
        neckAngle: 1.32;
        seatOffsetX: 1.2;
        seatOffsetY: -0.2;
        armReach01: 0.9;
        legReach01: 0.86;
        kneeBendDir: [1, 0];
        elbowBendDir: [-0.2, -1];
      },
      character.skins.polyRider);

  viewHeight: 85;
  ratio: canvas.size.width / canvas.size.height;
  viewWidth: viewHeight * ratio;
  left: -viewWidth / 2;
  bottom: -30;
  view: { left; bottom; right: left + viewWidth; top: bottom + viewHeight; };

  ground:
  {
    type: "line";
    name: "ground";
    from: [view.left, groundY];
    to: [view.right, groundY];
    stroke: "#0f172a";
    width: 0.35;
  };

  eval
  {
    view;
    graphics: [ground] + stickRider + polyRider;
  };
}

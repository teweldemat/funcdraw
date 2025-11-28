const BASE_VIEW = { width: 800, height: 400 };
const BASE_HERO = {
  torsoWidth: 56,
  torsoHeight: 150,
  shoulderExtension: 24,
  headExtent: 70,
  hand: { upper: 70, lower: 60 },
  leg: { upper: 90, lower: 80 },
  palette: {
    torsoFill: '#0f172a',
    headFill: '#fde68a',
    overlayHand: '#fbbf24',
    overlayLeg: '#38bdf8',
    handWidth: 10,
    legWidth: 12
  }
};

function buildScene() {
  const view = { left: 0, bottom: 0, right: BASE_VIEW.width, top: BASE_VIEW.height };
  const cartoon = package('@funcdraw/testlib').cartoon;
  const stickBuilder =
    typeof cartoon?.stickman === 'function' ? cartoon.stickman : () => ({ graphics: [] });

  const heroX = view.right / 2;
  const heroBaseY = view.top / 2;

  const legLength = BASE_HERO.leg.upper + BASE_HERO.leg.lower;
  const stanceKneeBend = 15;
  const groundY = heroBaseY - (legLength - stanceKneeBend);

  const basePhase = ((t % 1) + 1) % 1;
  const cycle = basePhase * Math.PI * 2;

  const torsoBobAmplitude = 4;
  const heroY = heroBaseY + torsoBobAmplitude * Math.sin(cycle * 2);

  const baseFootOffsetY = groundY - heroY;

  const stepForward = 30;
  const stepBack = -30;
  const stancePortion = 0.6;
  const swingHeight = 35;

  function normPhase(offset) {
    let p = basePhase + offset;
    if (p >= 1) p -= 1;
    if (p < 0) p += 1;
    return p;
  }

  function footTarget(phase) {
    if (phase < stancePortion) {
      const u = phase / stancePortion;
      const x = stepForward + (stepBack - stepForward) * u;
      const y = baseFootOffsetY;
      return [x, y];
    } else {
      const u = (phase - stancePortion) / (1 - stancePortion);
      const x = stepBack + (stepForward - stepBack) * u;
      const lift = swingHeight * 4 * u * (1 - u);
      const y = baseFootOffsetY + lift;
      return [x, y];
    }
  }

  const leftFootPhase = normPhase(0);
  const rightFootPhase = normPhase(0.5);

  const [leftLegX, leftFootY] = footTarget(leftFootPhase);
  const [rightLegX, rightFootY] = footTarget(rightFootPhase);

  const armSwingMagnitude = 35;
  const leftArmX = armSwingMagnitude * Math.sin(cycle + Math.PI);
  const rightArmX = armSwingMagnitude * Math.sin(cycle);

  const heroOptions = {
    position: [heroX, heroY],
    palette: BASE_HERO.palette,
    measurements: {
      torso: {
        width: BASE_HERO.torsoWidth,
        height: BASE_HERO.torsoHeight,
        shoulderExtension: BASE_HERO.shoulderExtension,
        direction: 'right'
      },
      head: {
        verticalExtent: BASE_HERO.headExtent,
        direction: 'right'
      },
      hands: {
        left: {
          effectorCoordinate: [leftArmX, -40],
          upperLength: BASE_HERO.hand.upper,
          lowerLength: BASE_HERO.hand.lower,
          positiveBend: true
        },
        right: {
          effectorCoordinate: [rightArmX, -40],
          upperLength: BASE_HERO.hand.upper,
          lowerLength: BASE_HERO.hand.lower,
          positiveBend: false
        }
      },
      legs: {
        left: {
          effectorCoordinate: [leftLegX, leftFootY],
          upperLength: BASE_HERO.leg.upper,
          lowerLength: BASE_HERO.leg.lower,
          positiveBend: true
        },
        right: {
          effectorCoordinate: [rightLegX, rightFootY],
          upperLength: BASE_HERO.leg.upper,
          lowerLength: BASE_HERO.leg.lower,
          positiveBend: true
        }
      }
    }
  };

  const heroGraphic = stickBuilder(heroOptions);

  const groundLine = {
    type: 'line',
    from: [view.left + 40, groundY],
    to: [view.right - 40, groundY],
    stroke: '#475569',
    width: 4
  };

  return {
    view,
    graphics: [groundLine, ...heroGraphic.graphics]
  };
}

return buildScene();

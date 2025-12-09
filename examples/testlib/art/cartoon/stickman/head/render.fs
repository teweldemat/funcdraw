(state)=>
{
  defaults:defaults;

  halfPi:defaults.halfPi;
  tau:defaults.tau;

  isProfile:state.direction = "left" or state.direction = "right";
  isFront:state.direction = "front";
  isBack:state.direction = "back";

  centerAngle:ensureFinite(if isProfile then halfPi else state.angleRad, halfPi);
  center:[
    state.attachmentPoint[0] + math.cos(centerAngle) * state.radius,
    state.attachmentPoint[1] + math.sin(centerAngle) * state.radius
  ];

  neckUp:[math.cos(centerAngle), math.sin(centerAngle)];
  headDown:[-neckUp[0], -neckUp[1]];

  tiltFromVertical:state.angleRad - halfPi;
  lookAngle:if state.direction = "left" then
    defaults.pi + tiltFromVertical
  else if state.direction = "right" then
    tiltFromVertical
  else if state.direction = "back" then
    -halfPi + tiltFromVertical
  else
    state.angleRad;

  lookRadians:ensureFinite(lookAngle, centerAngle);

  forward:[math.cos(lookRadians), math.sin(lookRadians)];
  right:[
    math.cos(lookRadians + halfPi),
    math.sin(lookRadians + halfPi)
  ];

  outlinePoints:buildOutline(0, []);
  buildOutline:(idx, acc)=> {
    eval if idx >= state.segments then acc else {
      theta:(idx / state.segments) * tau;
      point:[
        center[0] + math.cos(theta) * state.radius,
        center[1] + math.sin(theta) * state.radius
      ];
      eval buildOutline(idx + 1, acc + [point]);
    }
  };

  outline:{
    type:"polygon";
    points:outlinePoints;
    fill:state.fill;
    stroke:state.stroke;
    width:state.strokeWidth;
  };

  eyeBase:[
    center[0] + forward[0] * (state.radius * state.eyesConfig.offsetRatio),
    center[1] + forward[1] * (state.radius * state.eyesConfig.offsetRatio)
  ];
  lateralDistance:state.radius * state.eyesConfig.separationRatio;
  eyeRadius:state.radius * state.eyesConfig.radiusRatio;
  highlightRadius:eyeRadius * state.eyesConfig.highlightRatio;

  leftEyeCenter:[
    eyeBase[0] - right[0] * lateralDistance,
    eyeBase[1] - right[1] * lateralDistance
  ];
  rightEyeCenter:[
    eyeBase[0] + right[0] * lateralDistance,
    eyeBase[1] + right[1] * lateralDistance
  ];

  createRoundEye:(eyeCenter)=> {
    base:[
      {
        type:"circle";
        center:eyeCenter;
        radius:eyeRadius;
        fill:state.eyesConfig.fill;
        stroke:state.eyesConfig.stroke;
        width:state.strokeWidth * 0.75;
      }
    ];
    highlight:if highlightRadius > 0 then [{
      type:"circle";
      center:[
        eyeCenter[0] + forward[0] * eyeRadius * 0.25 - right[0] * eyeRadius * 0.2,
        eyeCenter[1] + forward[1] * eyeRadius * 0.25 - right[1] * eyeRadius * 0.2
      ];
      radius:highlightRadius;
      fill:state.eyesConfig.highlight;
      stroke:state.eyesConfig.highlight;
      width:highlightRadius * 0.5;
      opacity:0.85;
    }] else [];
    eval base + highlight;
  };

  frontEyes:if isBack then [] else
    if isProfile then [] else createRoundEye(leftEyeCenter) + createRoundEye(rightEyeCenter);

  profileSignRaw:if isProfile then (if state.direction = "left" then -1 else 1) else 0;
  profileSign:if profileSignRaw < 0 then -1 else if profileSignRaw > 0 then 1 else 0;
  signedRadius:profileSign * state.radius;
  profileEyeCenter:[
    center[0] + signedRadius * 0.65,
    center[1] + state.radius * 0.05
  ];
  profileOuterRadius:math.max(eyeRadius * 1.25, state.radius * 0.08);
  signedOuterRadius:profileSign * profileOuterRadius;
  profileHighlight:if highlightRadius > 0 then [{
    type:"circle";
    center:[
      profileEyeCenter[0] + signedOuterRadius * 0.3,
      profileEyeCenter[1] - profileOuterRadius * 0.2
    ];
    radius:profileOuterRadius * 0.25;
    fill:state.eyesConfig.highlight;
    stroke:state.eyesConfig.highlight;
    width:profileOuterRadius * 0.12;
    opacity:0.85;
  }] else [];
  profileEyes:if isProfile then [
    {
      type:"circle";
      center:profileEyeCenter;
      radius:profileOuterRadius;
      fill:"#f8fafc";
      stroke:state.eyesConfig.stroke;
      width:state.strokeWidth * 0.9;
    },
    {
      type:"circle";
      center:[
        profileEyeCenter[0] + signedOuterRadius * 0.15,
        profileEyeCenter[1]
      ];
      radius:profileOuterRadius * 0.45;
      fill:state.eyesConfig.fill;
      stroke:state.eyesConfig.fill;
      width:state.strokeWidth * 0.7;
    }
  ] + profileHighlight else [];

  eyes:frontEyes + profileEyes;

  frontNose:if isFront then [{
    type:"line";
    from:[
      eyeBase[0] + headDown[0] * state.radius * 0.15,
      eyeBase[1] + headDown[1] * state.radius * 0.15
    ];
    to:[
      eyeBase[0] + headDown[0] * state.radius * 0.55,
      eyeBase[1] + headDown[1] * state.radius * 0.55
    ];
    stroke:state.gazeColor;
    width:state.strokeWidth * 0.9;
  }] else [];

  profileNose:if isProfile then [
    {
      type:"line";
      from:[
        profileEyeCenter[0] + headDown[0] * state.radius * 0.1,
        profileEyeCenter[1] + headDown[1] * state.radius * 0.1
      ];
      to:[
        profileEyeCenter[0] + signedRadius * 0.45,
        profileEyeCenter[1] - state.radius * 0.05
      ];
      stroke:state.gazeColor;
      width:state.strokeWidth * 0.9;
    },
    {
      type:"line";
      from:[
        profileEyeCenter[0] + signedRadius * 0.45,
        profileEyeCenter[1] - state.radius * 0.05
      ];
      to:[
        profileEyeCenter[0] + signedRadius * 0.45,
        profileEyeCenter[1] - state.radius * 0.23
      ];
      stroke:state.gazeColor;
      width:state.strokeWidth * 0.9;
    }
  ] else [];

  facialDetails:frontNose + profileNose;

  graphics:[outline] + eyes + facialDetails;

  eval { graphics:graphics };

  ensureFinite:(value, fallback)=> {
    eval if value = null then fallback else {
      same:value = value;
      eval if same then value else fallback;
    }
  };
}

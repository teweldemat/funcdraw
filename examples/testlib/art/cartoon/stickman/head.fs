(attachmentPointInput, configInput)=>
{
  defaults:{
    verticalExtent:4.5;
    angle:90;
    fill:"#fff7ed";
    stroke:"#fdba74";
    strokeWidth:0.4;
    gazeColor:"#ea580c";
    segments:20;
    eyes:{
      separationRatio:0.38;
      offsetRatio:0.2;
      radiusRatio:0.14;
      fill:"#0f172a";
      stroke:"#0f172a";
      highlight:"#fef9c3";
      highlightRatio:0.4;
    };
  };

  pi:math.pi;
  halfPi:pi / 2;
  tau:pi * 2;
  minVerticalExtent:1;
  minSegments:6;

  attachmentPoint:if attachmentPointInput = null then [20,17] else attachmentPointInput;
  rawConfig:configInput ?? {};

  verticalExtent:math.max(if rawConfig.verticalExtent = null then defaults.verticalExtent else rawConfig.verticalExtent, minVerticalExtent);
  radius:verticalExtent / 2;

  angleDeg:if rawConfig.angle = null then defaults.angle else rawConfig.angle;
  angleRad:ensureFinite(degToRad(angleDeg), halfPi);

  fill:rawConfig.fill ?? defaults.fill;
  stroke:rawConfig.stroke ?? defaults.stroke;
  strokeWidth:math.max(0, if rawConfig.strokeWidth = null then defaults.strokeWidth else rawConfig.strokeWidth);
  gazeColor:rawConfig.gazeColor ?? defaults.gazeColor;

  segmentsRaw:math.floor(if rawConfig.segments = null then defaults.segments else rawConfig.segments);
  segmentsBase:if segmentsRaw = null then minSegments else segmentsRaw;
  segments:if segmentsBase < minSegments then minSegments else segmentsBase;

  direction:normalizeDirection(rawConfig.direction);
  eyesConfig:mixEyesConfig(rawConfig.eyes, defaults.eyes);

  isProfile:direction = "left" or direction = "right";
  isFront:direction = "front";
  isBack:direction = "back";

  centerAngle:ensureFinite(if isProfile then halfPi else angleRad, halfPi);
  center:[
    attachmentPoint[0] + math.cos(centerAngle) * radius,
    attachmentPoint[1] + math.sin(centerAngle) * radius
  ];

  neckUp:[math.cos(centerAngle), math.sin(centerAngle)];
  headDown:[-neckUp[0], -neckUp[1]];

  tiltFromVertical:angleRad - halfPi;

  lookAngle:if direction = "left" then
    pi + tiltFromVertical
  else if direction = "right" then
    tiltFromVertical
  else if direction = "back" then
    -halfPi + tiltFromVertical
  else
    angleRad;

  lookRadians:ensureFinite(lookAngle, centerAngle);

  forward:[math.cos(lookRadians), math.sin(lookRadians)];
  right:[
    math.cos(lookRadians + halfPi),
    math.sin(lookRadians + halfPi)
  ];

  outlinePoints:buildOutline(0, []);
  buildOutline:(idx, acc)=> {
    eval if idx >= segments then acc else {
      theta:(idx / segments) * tau;
      point:[
        center[0] + math.cos(theta) * radius,
        center[1] + math.sin(theta) * radius
      ];
      eval buildOutline(idx + 1, acc + [point]);
    }
  };

  outline:{
    type:"polygon";
    points:outlinePoints;
    fill:fill;
    stroke:stroke;
    width:strokeWidth;
  };

  eyeBase:[
    center[0] + forward[0] * (radius * eyesConfig.offsetRatio),
    center[1] + forward[1] * (radius * eyesConfig.offsetRatio)
  ];
  lateralDistance:radius * eyesConfig.separationRatio;
  eyeRadius:radius * eyesConfig.radiusRatio;
  highlightRadius:eyeRadius * eyesConfig.highlightRatio;

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
        fill:eyesConfig.fill;
        stroke:eyesConfig.stroke;
        width:strokeWidth * 0.75;
      }
    ];
    highlight:if highlightRadius > 0 then [{
      type:"circle";
      center:[
        eyeCenter[0] + forward[0] * eyeRadius * 0.25 - right[0] * eyeRadius * 0.2,
        eyeCenter[1] + forward[1] * eyeRadius * 0.25 - right[1] * eyeRadius * 0.2
      ];
      radius:highlightRadius;
      fill:eyesConfig.highlight;
      stroke:eyesConfig.highlight;
      width:highlightRadius * 0.5;
      opacity:0.85;
    }] else [];
    eval base + highlight;
  };

  frontEyes:if isBack then [] else
    if isProfile then [] else createRoundEye(leftEyeCenter) + createRoundEye(rightEyeCenter);

  profileSign:if isProfile then (if direction = "left" then -1 else 1) else 0;
  profileEyeCenter:[
    center[0] + profileSign * radius * 0.65,
    center[1] + radius * 0.05
  ];
  profileOuterRadius:math.max(eyeRadius * 1.25, radius * 0.08);
  profileHighlight:if highlightRadius > 0 then [{
    type:"circle";
    center:[
      profileEyeCenter[0] + profileSign * profileOuterRadius * 0.3,
      profileEyeCenter[1] - profileOuterRadius * 0.2
    ];
    radius:profileOuterRadius * 0.25;
    fill:eyesConfig.highlight;
    stroke:eyesConfig.highlight;
    width:profileOuterRadius * 0.12;
    opacity:0.85;
  }] else [];
  profileEyes:if isProfile then [
    {
      type:"circle";
      center:profileEyeCenter;
      radius:profileOuterRadius;
      fill:"#f8fafc";
      stroke:eyesConfig.stroke;
      width:strokeWidth * 0.9;
    },
    {
      type:"circle";
      center:[
        profileEyeCenter[0] + profileSign * profileOuterRadius * 0.15,
        profileEyeCenter[1]
      ];
      radius:profileOuterRadius * 0.45;
      fill:eyesConfig.fill;
      stroke:eyesConfig.fill;
      width:strokeWidth * 0.7;
    }
  ] + profileHighlight else [];

  eyes:frontEyes + profileEyes;

  frontNose:if isFront then [{
    type:"line";
    from:[
      eyeBase[0] + headDown[0] * radius * 0.15,
      eyeBase[1] + headDown[1] * radius * 0.15
    ];
    to:[
      eyeBase[0] + headDown[0] * radius * 0.55,
      eyeBase[1] + headDown[1] * radius * 0.55
    ];
    stroke:gazeColor;
    width:strokeWidth * 0.9;
  }] else [];

  profileNose:if isProfile then [
    {
      type:"line";
      from:[
        profileEyeCenter[0] + headDown[0] * radius * 0.1,
        profileEyeCenter[1] + headDown[1] * radius * 0.1
      ];
      to:[
        profileEyeCenter[0] + profileSign * radius * 0.45,
        profileEyeCenter[1] - radius * 0.05
      ];
      stroke:gazeColor;
      width:strokeWidth * 0.9;
    },
    {
      type:"line";
      from:[
        profileEyeCenter[0] + profileSign * radius * 0.45,
        profileEyeCenter[1] - radius * 0.05
      ];
      to:[
        profileEyeCenter[0] + profileSign * radius * 0.45,
        profileEyeCenter[1] - radius * 0.23
      ];
      stroke:gazeColor;
      width:strokeWidth * 0.9;
    }
  ] else [];

  facialDetails:frontNose + profileNose;

  graphics:[outline] + eyes + facialDetails;

  eval { graphics:graphics };

  normalizeDirection:(value)=> {
    lowered:if value = null then null else text.lower(format(value));
    eval if lowered = null then null
    else if lowered = "left" then "left"
    else if lowered = "right" then "right"
    else if lowered = "front" then "front"
    else if lowered = "back" then "back"
    else null;
  };

  degToRad:(degrees)=> (degrees * pi) / 180;

  ensureFinite:(value, fallback)=> {
    eval if value = null then fallback else {
      same:value = value;
      eval if same then value else fallback;
    }
  };

  mixEyesConfig:(rawConfig, base)=> {
    baseConfig:base ?? {};
    config:baseConfig + (rawConfig ?? {});

    separationRatio:helpers.clamp(if config.separationRatio = null then baseConfig.separationRatio ?? 0.38 else config.separationRatio, 0.1, 0.8);
    offsetRatio:helpers.clamp(if config.offsetRatio = null then baseConfig.offsetRatio ?? 0.2 else config.offsetRatio, -0.2, 0.6);
    radiusRatio:helpers.clamp(if config.radiusRatio = null then baseConfig.radiusRatio ?? 0.14 else config.radiusRatio, 0.05, 0.35);
    highlightRatio:helpers.clamp(if config.highlightRatio = null then baseConfig.highlightRatio ?? 0.4 else config.highlightRatio, 0, 1);

    fillColor:config.fill ?? baseConfig.fill ?? "#0f172a";
    strokeColor:config.stroke ?? config.fill ?? baseConfig.stroke ?? fillColor;
    highlightColor:config.highlight ?? baseConfig.highlight ?? "#fef9c3";

    eval {
      separationRatio:separationRatio;
      offsetRatio:offsetRatio;
      radiusRatio:radiusRatio;
      highlightRatio:highlightRatio;
      fill:fillColor;
      stroke:strokeColor;
      highlight:highlightColor;
    };
  };
}

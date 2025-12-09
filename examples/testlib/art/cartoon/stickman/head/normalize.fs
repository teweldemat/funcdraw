(attachmentPointInput, configInput)=>
{
  helpers:cartoon.helpers;
  defaults:defaults;

  rawConfig:configInput ?? {};
  base:defaults.headDefaults;

  attachmentPoint:if attachmentPointInput = null then [20,17] else attachmentPointInput;
  verticalExtent:math.max(if rawConfig.verticalExtent = null then base.verticalExtent else rawConfig.verticalExtent, defaults.minVerticalExtent);
  radius:verticalExtent / 2;

  angleDeg:if rawConfig.angle = null then base.angle else rawConfig.angle;
  angleRad:ensureFinite(degToRad(angleDeg, defaults.pi), defaults.halfPi);

  fill:rawConfig.fill ?? base.fill;
  stroke:rawConfig.stroke ?? base.stroke;
  strokeWidth:math.max(0, if rawConfig.strokeWidth = null then base.strokeWidth else rawConfig.strokeWidth);
  gazeColor:rawConfig.gazeColor ?? base.gazeColor;

  segmentsRaw:math.floor(if rawConfig.segments = null then base.segments else rawConfig.segments);
  segmentsBase:if segmentsRaw = null then defaults.minSegments else segmentsRaw;
  segments:if segmentsBase < defaults.minSegments then defaults.minSegments else segmentsBase;

  direction:normalizeDirection(rawConfig.direction);
  eyesConfig:mixEyesConfig(rawConfig.eyes, base.eyes);

  eval {
    attachmentPoint:attachmentPoint;
    radius:radius;
    angleRad:angleRad;
    fill:fill;
    stroke:stroke;
    strokeWidth:strokeWidth;
    gazeColor:gazeColor;
    segments:segments;
    direction:direction;
    eyesConfig:eyesConfig;
  };

  degToRad:(degrees, pi)=> (degrees * pi) / 180;

  ensureFinite:(value, fallback)=> {
    eval if value = null then fallback else {
      same:value = value;
      eval if same then value else fallback;
    }
  };

  normalizeDirection:(value)=> {
    lowered:if value = null then null else text.lower(format(value));
    eval if lowered = null then null
    else if lowered = "left" then "left"
    else if lowered = "right" then "right"
    else if lowered = "front" then "front"
    else if lowered = "back" then "back"
    else null;
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

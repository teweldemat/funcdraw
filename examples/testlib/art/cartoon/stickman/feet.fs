(options)=>
{
  defaults:{ length:2.2; stroke:"#f97316"; strokeWidth:0.5 };

  input:helpers.normalizeInput(options ?? {}, {});
  anklePoint:helpers.normalizePoint(input.anklePoint ?? input.anchor ?? input.position, [0,0]);
  side:normalizeSide(input.side, "left");
  directionHint:normalizeDirection(input.directionHint, normalizeDirection(side, "left"));
  lineLength:clampPositive(input.length, defaults.length, 0.05);
  style:helpers.normalizeInput(input.style, {});
  stroke:input.stroke ?? style.stroke ?? defaults.stroke;
  strokeWidth:clampPositive(style.width ?? input.strokeWidth, defaults.strokeWidth, 0.01);
  half:lineLength / 2;
  sign:if directionHint = "right" then 1 else -1;
  fromPoint:if directionHint = "center" then [anklePoint[0] - half, anklePoint[1]] else anklePoint;
  toePoint:if directionHint = "center" then [anklePoint[0] + half, anklePoint[1]] else [anklePoint[0] + sign * lineLength, anklePoint[1]];
  centerPoint:if directionHint = "center" then anklePoint else toePoint;

  eval {
    graphics:[
      {
        type:"line";
        from:fromPoint;
        to:toePoint;
        stroke:stroke;
        width:strokeWidth;
      }
    ];
    anklePoint:anklePoint;
    center:centerPoint;
    toePoint:toePoint;
    side:side;
    direction:directionHint;
    length:lineLength;
  };

  clampPositive:(value, fallback, min)=> {
    minValue:min ?? 0.01;
    resolved:helpers.resolveNumber(value, fallback);
    return if resolved >= minValue then resolved else fallback;
  };

  normalizeSide:(value, fallback)=> {
    defaultSide:fallback ?? "left";
    eval if value = null then defaultSide else {
      lowered:text.lower(format(value));
      eval if lowered = "right" then "right" else if lowered = "left" then "left" else defaultSide;
    }
  };

  normalizeDirection:(value, fallback)=> {
    defaultDirection:fallback ?? "left";
    eval if value = null then defaultDirection else {
      lowered:text.lower(format(value));
      eval if lowered = "center" then "center"
      else if lowered = "middle" then "center"
      else if lowered = "right" then "right"
      else if lowered = "left" then "left"
      else defaultDirection;
    }
  };
}

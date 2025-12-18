(options, state) =>
{
  point: options.point;
  radius: options.radius;
  hitPadding: options?.hitPadding??0;

  baseFill: options?.fill??"#e2e8f0";
  hoverFill: options?.hoverFill??baseFill;
  activeFill: options?.activeFill??hoverFill;

  baseStroke: options?.stroke??"#0f172a";
  hoverStroke: options?.hoverStroke??baseStroke;
  activeStroke: options?.activeStroke??hoverStroke;

  baseWidth: options?.width??0.2;
  hoverWidth: options?.hoverWidth??baseWidth;
  activeWidth: options?.activeWidth??baseWidth * 2;

  hoverScale: options?.hoverScale??1.0;
  activeScale: options?.activeScale??1.4;

  currentPoint: if point == null then error("ui/dragdot: expected options.point") else point;

  x: currentPoint[0];
  y: currentPoint[1];

  pointerId: if state == null then null else state.pointerId;
  hovered: if state == null then false else state.hovered;
  offset:
    if state == null then [0, 0]
    else if state.offset == null then [0, 0]
    else state.offset;
  offsetX: offset[0];
  offsetY: offset[1];

  dragging: pointerId != null;

  isInside: (p) =>
  {
    dx: p.x - x;
    dy: p.y - y;
    r: radius + hitPadding;
    eval dx * dx + dy * dy <= r * r;
  };

  stepper: (event) =>
  {
    eval if event.type != "pointer" then null else
    {
      id: event.pointer?.id??0;
      inside: isInside(event.point);

      startDrag: event.action == "down" and inside and !dragging;
      stopDrag:
        (event.action == "up" or event.action == "cancel") and dragging and id == pointerId;

      draggingNow: startDrag or (dragging and !stopDrag);

      nextPointerId: if startDrag then id else if stopDrag then null else pointerId;
      nextOffset:
        if startDrag then [x - event.point.x, y - event.point.y]
        else if stopDrag then [0, 0]
        else [offsetX, offsetY];

      nextPoint:
        if draggingNow and (event.action == "move" or event.action == "down") and id == nextPointerId then
          [event.point.x + nextOffset[0], event.point.y + nextOffset[1]]
        else [x, y];

      nextHovered: if draggingNow then true else inside;

      pointChanged: nextPoint[0] != x or nextPoint[1] != y;
      dragChanged: nextPointerId != pointerId;
      hoverChanged: nextHovered != hovered;

      events:
        if startDrag then [{ type: "ui"; action: "dragstart"; }]
        else if pointChanged and draggingNow then [{ type: "ui"; action: "change"; value: nextPoint; }]
        else if stopDrag then [{ type: "ui"; action: "dragend"; }]
        else [];

      nextState:
      {
        hovered: nextHovered;
        pointerId: nextPointerId;
        offset: nextOffset;
      };

      eval if pointChanged or dragChanged or hoverChanged then { state: nextState; events; } else null;
    };
  };

  visualRadius:
    if dragging then radius * activeScale
    else if hovered then radius * hoverScale
    else radius;

  visualFill: if dragging then activeFill else if hovered then hoverFill else baseFill;
  visualStroke: if dragging then activeStroke else if hovered then hoverStroke else baseStroke;
  visualWidth: if dragging then activeWidth else if hovered then hoverWidth else baseWidth;

  eval
  {
    graphics:
    [
      {
        type: "circle";
        center: [x, y];
        radius: visualRadius;
        fill: visualFill;
        stroke: visualStroke;
        width: visualWidth;
      }
    ];
    step: stepper;
  };
}

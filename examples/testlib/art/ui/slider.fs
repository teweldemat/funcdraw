(options, state) =>
{
  position: options.position;
  size: options.size;
  min: options.min;
  max: options.max;
  value: options.value;
  label: options?.label;

  hovered: if state == null then false else state.hovered;
  dragging: if state == null then false else state.dragging;

  range: max - min;
  t: (value - min) / range;
  tClamped: if t < 0 then 0 else if t > 1 then 1 else t;
  knobX: position[0] + tClamped * size[0];
  centerY: position[1] + size[1] / 2;
  knobRadius: size[1] * 0.45;
  barWidth: size[1] * 0.5;

  isInside: (point) =>
  {
    x: point.x;
    y: point.y;
    eval
      x >= position[0] and
      x <= position[0] + size[0] and
      y >= position[1] and
      y <= position[1] + size[1];
  };

  valueAtPoint: (point) =>
  {
    ratio: (point.x - position[0]) / size[0];
    ratioClamped: if ratio < 0 then 0 else if ratio > 1 then 1 else ratio;
    eval min + ratioClamped * range;
  };

  stepper: (event) =>
  {
    eval if event.type != "pointer" then null else
    {
      inside: isInside(event.point);
      startDrag: event.action == "down" and inside;
      stopDrag: (event.action == "up" or event.action == "cancel") and dragging;
      draggingNow: startDrag or (dragging and !stopDrag);
      nextDragging: if startDrag then true else if stopDrag then false else dragging;
      nextHovered: if draggingNow then true else inside;

      computedValue:
        if startDrag or (event.action == "move" and draggingNow) then valueAtPoint(event.point)
        else value;

      valueChanged: computedValue != value;
      events:
        if valueChanged and (startDrag or (event.action == "move" and draggingNow)) then
        [
          { type: "ui"; action: "change"; value: computedValue; }
        ]
        else [];

      eval
      if nextHovered != hovered or nextDragging != dragging or valueChanged then
      {
        state: { hovered: nextHovered; dragging: nextDragging; };
        events;
      }
      else null;
    };
  };

  track:
  {
    type: "rect";
    position;
    size;
    fill: if hovered or dragging then "#334155" else "#1e293b";
    stroke: if hovered or dragging then "#fbbf24" else "#38bdf8";
    width: if hovered or dragging then 0.6 else 0.35;
  };

  bar:
  {
    type: "line";
    from: [position[0], centerY];
    to: [knobX, centerY];
    stroke: "#38bdf8";
    width: barWidth;
  };

  knob:
  {
    type: "circle";
    center: [knobX, centerY];
    radius: knobRadius;
    fill: if hovered or dragging then "#fbbf24" else "#38bdf8";
    stroke: "#0f172a";
    width: 0.2;
  };

  labelPosition: [position[0], position[1] + size[1] + 1.2];
  labelNode: if label == null then null else
  {
    type: "text";
    text: label;
    position: labelPosition;
    fontSize: 1.2;
    color: "#94a3b8";
  };

  eval
  {
    graphics: if labelNode == null then [track, bar, knob] else [track, bar, knob, labelNode];
    step: stepper;
  };
}

(options, state) =>
{
  position: options.position;
  size: options.size;
  value: options.value;
  label: options?.label;

  baseFill: "#1e293b";
  onFill: "#0f766e";
  baseStroke: "#38bdf8";
  hoverStroke: "#fbbf24";
  baseWidth: 0.35;
  hoverWidth: 0.6;
  knobFill: "#e2e8f0";
  knobStroke: "#0f172a";
  knobWidth: 0.2;
  textColor: "#e2e8f0";

  hovered: if state == null then false else state.hovered;

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

  centerY: position[1] + size[1] / 2;
  fontSize: size[1] * 0.75;
  labelGap: size[1] * 0.6;
  switchWidth: size[1] * 1.8;
  knobRadius: size[1] * 0.45;

  knobX:
    if value then position[0] + switchWidth - size[1] / 2
    else position[0] + size[1] / 2;
  knobCenter: [knobX, centerY];

  labelMetrics: if label == null then null else fd.measureText(label, fontSize);
  labelLineCount: if labelMetrics == null then 1 else Len(labelMetrics.lines);
  labelVerticalOffset:
    if labelMetrics == null then 0
    else (labelMetrics.ascent - labelMetrics.descent - labelMetrics.lineHeight * (labelLineCount - 1)) / 2;
  labelBaselineY: centerY - labelVerticalOffset;

  labelPosition: [position[0] + switchWidth + labelGap, labelBaselineY];

  trackPosition: position;
  trackSize: [switchWidth, size[1]];

  track:
  {
    type: "rect";
    position: trackPosition;
    size: trackSize;
    fill: if value then onFill else baseFill;
    stroke: if hovered then hoverStroke else baseStroke;
    width: if hovered then hoverWidth else baseWidth;
  };

  knob:
  {
    type: "circle";
    center: knobCenter;
    radius: knobRadius;
    fill: knobFill;
    stroke: knobStroke;
    width: knobWidth;
  };

  labelNode: if label == null then null else
  {
    type: "text";
    text: label;
    position: labelPosition;
    fontSize;
    align: "left";
    color: textColor;
  };

  content: if labelNode == null then [track, knob] else [track, knob, labelNode];

  stepper: (event) =>
    if event.type != "pointer" then null else
    {
      inside: isInside(event.point);
      nextHovered: inside;
      clicked: event.action == "down" and inside;
      nextValue: if clicked then !value else value;
      events: if clicked then [{ type: "ui"; action: "change"; value: nextValue; }] else [];

      eval if nextHovered != hovered or clicked then
      {
        state: { hovered: nextHovered; };
        events;
      }
      else null;
    };

  eval
  {
    graphics: content;
    step: stepper;
  };
}

(options, state) =>
{
  position: options.position;
  size: options.size;
  label: options?.label;
  graphics: options?.graphics;

  baseFill: "#1e293b";
  hoverFill: "#334155";
  baseStroke: "#38bdf8";
  hoverStroke: "#fbbf24";
  baseWidth: 0.35;
  hoverWidth: 0.6;
  textColor: "#e2e8f0";
  fontSize: 2.4;
  graphicsSize: fontSize;
  gap: fontSize * 0.6;

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

  center: [position[0] + size[0] / 2, position[1] + size[1] / 2];

  labelMetrics: if graphics == null or label == null then null else fd.measureText(label, fontSize);
  labelWidth: if labelMetrics == null then 0 else labelMetrics.width;

  groupWidth: if graphics == null then labelWidth else if label == null then graphicsSize else graphicsSize + gap + labelWidth;
  groupLeft: center[0] - groupWidth / 2;
  iconCenterX: groupLeft + graphicsSize / 2;
  labelLeftX: if graphics == null or label == null then center[0] else groupLeft + graphicsSize + gap;

  iconTransform: if graphics == null then null else
  {
    type: "transform";
    matrix: [graphicsSize, 0, 0, graphicsSize, if label == null then center[0] else iconCenterX, center[1]];
    graphics;
  };
  textNode: if label == null then null else
  {
    type: "text";
    text: label;
    position: [if graphics == null then center[0] else labelLeftX, center[1]];
    fontSize;
    align: if graphics == null then "center" else "left";
    color: textColor;
  };

  content: if iconTransform == null and textNode == null then error("ui/button: expected options.label or graphics")
    else if iconTransform == null then [textNode]
    else if textNode == null then [iconTransform]
    else [iconTransform, textNode];

  stepper: (event) =>
  {
    eval if event.type != "pointer" then null else
    {
      inside: isInside(event.point);
      nextHovered: inside;
      clicked: event.action == "down" and inside;
      events: if clicked then [{ type: "ui"; action: "click"; }] else [];

      eval if nextHovered != hovered or clicked then
      {
        state: { hovered: nextHovered; };
        events;
      }
      else null;
    };
  };

  eval
  {
    graphics:
    [
      {
        type: "rect";
        position;
        size;
        fill: if hovered then hoverFill else baseFill;
        stroke: if hovered then hoverStroke else baseStroke;
        width: if hovered then hoverWidth else baseWidth;
      }
    ] + content;
    step: stepper;
  };
}

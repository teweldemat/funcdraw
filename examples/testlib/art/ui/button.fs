(options, state) =>
{
  position: options.position;
  size: options.size;
  label: options.label;

  baseFill: "#1e293b";
  hoverFill: "#334155";
  baseStroke: "#38bdf8";
  hoverStroke: "#fbbf24";
  baseWidth: 0.35;
  hoverWidth: 0.6;
  textColor: "#e2e8f0";
  fontSize: 2.4;

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

  stepper: (event) =>
  {
    inside: isInside(event.point);
    eval if event.type != "pointer" or !inside then null else
    {
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
      },
      {
        type: "text";
        text: label;
        position: center;
        fontSize;
        align: "center";
        color: textColor;
      }
    ];
    step: stepper;
  };
}

(state) =>
{
  rectPosition: [-8, -4];
  rectSize: [16, 8];

  baseFill: "#1e293b";
  hoverFill: "#334155";
  baseStroke: "#38bdf8";
  hoverStroke: "#fbbf24";
  textColor: "#e2e8f0";
  hintColor: "#94a3b8";

  hovered: if state == null then false else state.hovered;
  count: if state == null then 0 else state.count;

  isInside: (point) =>
  {
    x: point.x;
    y: point.y;
    eval
      x >= rectPosition[0] and
      x <= rectPosition[0] + rectSize[0] and
      y >= rectPosition[1] and
      y <= rectPosition[1] + rectSize[1];
  };

  stepper: (event) =>
  {
    inside: isInside(event.point);
    nextHovered: inside;
    nextCount: if event.action == "down" and inside then count + 1 else count;

    eval
    if nextHovered != hovered or nextCount != count then
    {
      state: { hovered: nextHovered; count: nextCount; };
      events: [];
    }
    else null;
  };

  boxFill: if hovered then hoverFill else baseFill;
  boxStroke: if hovered then hoverStroke else baseStroke;
  boxWidth: if hovered then 0.6 else 0.35;
  counterText: "Clicks\n" + count;

  eval
  {
    view:
    {
      left: -12;
      bottom: -8;
      right: 12;
      top: 8;
    };
    graphics:
    [
      {
        type: "rect";
        position: rectPosition;
        size: rectSize;
        fill: boxFill;
        stroke: boxStroke;
        width: boxWidth;
      },
      {
        type: "text";
        text: "Move over the box and click";
        position: [-11, 6.5];
        fontSize: 1.6;
        color: hintColor;
      },
      {
        type: "text";
        text: counterText;
        position: [0, 1.5];
        fontSize: 2.4;
        align: "center";
        color: textColor;
      }
    ];
    step: stepper;
  };
}

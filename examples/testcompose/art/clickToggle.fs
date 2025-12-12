(state) =>
{
  primary: "#38bdf8";
  secondary: "#f472b6";
  stroke: "#0ea5e9";
  textColor: "#e2e8f0";

  fill: if state == null then primary else state.fill;
  toggled: if fill == primary then secondary else primary;

  stepper: (event) =>
  {
    nextFill: if event.action == "down" then toggled else fill;
    eval { state: { fill: nextFill; }; events: []; };
  };

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
        position: [-8, -4];
        size: [16, 8];
        fill;
        stroke;
        width: 0.35;
      },
      {
        type: "text";
        text: "Click the rectangle to toggle fill";
        position: [-10.5, 6];
        fontSize: 2;
        color: textColor;
      }
    ];
    step: stepper;
  };
}

(state) =>
{
  buttonState: if state == null then null else state.button;
  count: if state == null then 0 else state.count;

  options:
  {
    position: [-8, -4];
    size: [16, 8];
    label: "Clicks\n" + count;
  };

  button: package("@funcdraw/testlib").ui.button(options, buttonState);

  stepper: (event) =>
  {
    buttonStep: button.step(event);
    clicked: buttonStep != null and Len(buttonStep.events) > 0;
    nextCount: if clicked then count + 1 else count;

    eval
    if buttonStep == null then null else
    {
      state: { button: buttonStep.state; count: nextCount; };
      events: buttonStep.events;
    };
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
      button.graphics + [
        {
          type: "text";
          text: "testlib/ui/button demo";
          position: [-11.5, 6.5];
          fontSize: 1.4;
          color: "#94a3b8";
        }
      ];
    step: stepper;
  };
}

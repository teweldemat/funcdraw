(state) =>
{
  leftButtonState: if state == null then null else state.leftButton;
  rightButtonState: if state == null then null else state.rightButton;
  leftCount: if state == null then 0 else state.leftCount;
  rightCount: if state == null then 0 else state.rightCount;

  icon:
  [
    {
      type: "circle";
      center: [0, 0];
      radius: 0.55;
      fill: "#22c55e";
      stroke: "#16a34a";
      width: 0.12;
    },
    {
      type: "line";
      from: [-0.25, 0];
      to: [0.25, 0];
      stroke: "#052e16";
      width: 0.12;
    },
    {
      type: "line";
      from: [0, -0.25];
      to: [0, 0.25];
      stroke: "#052e16";
      width: 0.12;
    }
  ];

  leftOptions:
  {
    position: [-11, -4];
    size: [14, 8];
    label: "Add";
    graphics: icon;
  };

  rightOptions:
  {
    position: [5, -4];
    size: [6, 8];
    graphics: icon;
  };

  leftButton: package("@funcdraw/testlib").ui.button(leftOptions, leftButtonState);
  rightButton: package("@funcdraw/testlib").ui.button(rightOptions, rightButtonState);

  stepper: (event) =>
  {
    leftStep: leftButton.step(event);
    rightStep: rightButton.step(event);

    leftClicked: leftStep != null and Len(leftStep.events) > 0;
    rightClicked: rightStep != null and Len(rightStep.events) > 0;

    nextLeftCount: if leftClicked then leftCount + 1 else leftCount;
    nextRightCount: if rightClicked then rightCount + 1 else rightCount;

    nextLeftState: if leftStep == null then leftButtonState else leftStep.state;
    nextRightState: if rightStep == null then rightButtonState else rightStep.state;

    events: (if leftStep == null then [] else leftStep.events) + (if rightStep == null then [] else rightStep.events);

    eval
    if leftStep == null and rightStep == null then null else
    {
      state:
      {
        leftButton: nextLeftState;
        rightButton: nextRightState;
        leftCount: nextLeftCount;
        rightCount: nextRightCount;
      };
      events;
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
      leftButton.graphics +
      rightButton.graphics +
      [
        {
          type: "text";
          text: "testlib/ui/button icon demo";
          position: [-11.5, 6.5];
          fontSize: 1.4;
          color: "#94a3b8";
        },
        {
          type: "text";
          text: "Add clicks: " + leftCount + " | icon-only clicks: " + rightCount;
          position: [-11.5, 5.2];
          fontSize: 1.4;
          color: "#94a3b8";
        }
      ];
    step: stepper;
  };
}

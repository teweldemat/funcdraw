{
  options:
  {
    position: [-8, -4];
    size: [16, 8];
    label: "Click";
  };

  moveInside:
  {
    type: "pointer";
    action: "move";
    point: { x: 0; y: 0; };
  };

  moveOutside:
  {
    type: "pointer";
    action: "move";
    point: { x: 20; y: 0; };
  };

  downInside:
  {
    type: "pointer";
    action: "down";
    point: { x: 0; y: 0; };
  };

  downOutside:
  {
    type: "pointer";
    action: "down";
    point: { x: 20; y: 0; };
  };

  nonPointer:
  {
    type: "keyboard";
    action: "down";
  };

  eval
  [
    {
      name: "renders base styles and label";
      test: (button) =>
      {
        initial: button(options, null);
        rect: initial.graphics[0];
        label: initial.graphics[1];
        eval
        [
          assert.equal(rect.fill, "#1e293b"),
          assert.equal(rect.stroke, "#38bdf8"),
          assert.equal(rect.width, 0.35),
          assert.equal(label.text, "Click"),
          assert.equal(label.position[0], 0),
          assert.equal(label.position[1], 0),
          assert.equal(label.align, "center"),
          assert.equal(label.color, "#e2e8f0"),
          assert.equal(label.fontSize, 2.4)
        ];
      };
    },
    {
      name: "highlights on hover and clears when leaving";
      test: (button) =>
      {
        initial: button(options, null);
        hoverStep: initial.step(moveInside);
        hovered: button(options, hoverStep.state);
        leaveStep: hovered.step(moveOutside);
        left: button(options, leaveStep.state);
        eval
        [
          assert.equal(hoverStep.state.hovered, true),
          assert.equal(hovered.graphics[0].fill, "#334155"),
          assert.equal(hovered.graphics[0].stroke, "#fbbf24"),
          assert.equal(hovered.graphics[0].width, 0.6),
          assert.equal(leaveStep.state.hovered, false),
          assert.equal(left.graphics[0].fill, "#1e293b"),
          assert.equal(left.graphics[0].stroke, "#38bdf8")
        ];
      };
    },
    {
      name: "emits ui click event on pointer down inside";
      test: (button) =>
      {
        initial: button(options, null);
        clickStep: initial.step(downInside);
        clicked: button(options, clickStep.state);
        eval
        [
          assert.equal(clickStep.state.hovered, true),
          assert.equal(Len(clickStep.events), 1),
          assert.equal(clickStep.events[0].type, "ui"),
          assert.equal(clickStep.events[0].action, "click"),
          assert.equal(clicked.graphics[0].fill, "#334155")
        ];
      };
    },
    {
      name: "returns null for pointer down outside when idle";
      test: (button) =>
      {
        initial: button(options, null);
        eval
        [
          assert.isnull(initial.step(downOutside))
        ];
      };
    },
    {
      name: "clears hover on pointer down outside without click event";
      test: (button) =>
      {
        hovered: button(options, { hovered: true; });
        step: hovered.step(downOutside);
        next: button(options, step.state);
        eval
        [
          assert.equal(step.state.hovered, false),
          assert.equal(Len(step.events), 0),
          assert.equal(next.graphics[0].fill, "#1e293b")
        ];
      };
    },
    {
      name: "ignores non-pointer events";
      test: (button) =>
      {
        initial: button(options, null);
        eval
        [
          assert.isnull(initial.step(nonPointer))
        ];
      };
    }
  ];
}

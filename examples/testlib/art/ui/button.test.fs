{
  options:
  {
    position: [-8, -4];
    size: [16, 8];
    label: "Click";
  };

  icon:
  {
    type: "circle";
    center: [0, 0];
    radius: 0.5;
    fill: "#22c55e";
    stroke: "#22c55e";
    width: 0.2;
  };

  optionsWithIcon: options + { graphics: icon; };

  optionsIconOnly:
  {
    position: [-8, -4];
    size: [16, 8];
    graphics: icon;
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
        fontSize: 2.4;
        center: [options.position[0] + options.size[0] / 2, options.position[1] + options.size[1] / 2];
        metrics: fd.measureText(options.label, fontSize);
        lineCount: Len(metrics.lines);
        baselineY: center[1] - (metrics.ascent - metrics.descent - metrics.lineHeight * (lineCount - 1)) / 2;
        eval
        [
          assert.equal(rect.fill, "#1e293b"),
          assert.equal(rect.stroke, "#38bdf8"),
          assert.equal(rect.width, 0.35),
          assert.equal(label.text, "Click"),
          assert.equal(label.position[0], 0),
          assert.approx(label.position[1], baselineY, 0.0001),
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
    ,
    {
      name: "renders optional icon beside label";
      test: (button) =>
      {
        initial: button(optionsWithIcon, null);
        icon: initial.graphics[1];
        label: initial.graphics[2];

        fontSize: 2.4;
        gap: fontSize * 0.6;
        center: [options.position[0] + options.size[0] / 2, options.position[1] + options.size[1] / 2];
        metrics: fd.measureText(options.label, fontSize);
        lineCount: Len(metrics.lines);
        baselineY: center[1] - (metrics.ascent - metrics.descent - metrics.lineHeight * (lineCount - 1)) / 2;
        groupWidth: fontSize + gap + metrics.width;
        groupLeft: center[0] - groupWidth / 2;
        iconCenterX: groupLeft + fontSize / 2;
        labelLeftX: groupLeft + fontSize + gap;

        eval
        [
          assert.equal(icon.type, "transform"),
          assert.equal(icon.graphics.type, "circle"),
          assert.equal(label.text, options.label),
          assert.equal(label.align, "left"),
          assert.approx(icon.matrix[0], fontSize, 0.0001),
          assert.approx(icon.matrix[3], fontSize, 0.0001),
          assert.approx(icon.matrix[4], iconCenterX, 0.0001),
          assert.approx(icon.matrix[5], center[1], 0.0001),
          assert.approx(label.position[0], labelLeftX, 0.0001),
          assert.approx(label.position[1], baselineY, 0.0001)
        ];
      };
    },
    {
      name: "renders graphics-only button";
      test: (button) =>
      {
        initial: button(optionsIconOnly, null);
        icon: initial.graphics[1];

        fontSize: 2.4;
        center: [optionsIconOnly.position[0] + optionsIconOnly.size[0] / 2, optionsIconOnly.position[1] + optionsIconOnly.size[1] / 2];

        eval
        [
          assert.equal(Len(initial.graphics), 2),
          assert.equal(icon.type, "transform"),
          assert.approx(icon.matrix[0], fontSize, 0.0001),
          assert.approx(icon.matrix[3], fontSize, 0.0001),
          assert.approx(icon.matrix[4], center[0], 0.0001),
          assert.approx(icon.matrix[5], center[1], 0.0001)
        ];
      };
    }
  ];
}

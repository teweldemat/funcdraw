{
  options:
  {
    position: [-8, -1];
    size: [24, 2];
    value: false;
    label: "same left/right";
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
    point: { x: 40; y: 0; };
  };

  downInside:
  {
    type: "pointer";
    action: "down";
    point: { x: 0; y: 0; };
  };

  nonPointer:
  {
    type: "keyboard";
    action: "down";
  };

  eval
  [
    {
      name: "renders base track knob and label";
      test: (toggle) =>
      {
        initial: toggle(options, null);
        track: initial.graphics[0];
        knob: initial.graphics[1];
        label: initial.graphics[2];

        switchWidth: options.size[1] * 1.8;
        labelGap: options.size[1] * 0.6;
        centerY: options.position[1] + options.size[1] / 2;

        fontSize: options.size[1] * 0.75;
        metrics: fd.measureText(options.label, fontSize);
        lineCount: Len(metrics.lines);
        baselineY: centerY - (metrics.ascent - metrics.descent - metrics.lineHeight * (lineCount - 1)) / 2;

        eval
        [
          assert.equal(track.type, "rect"),
          assert.equal(track.fill, "#1e293b"),
          assert.equal(track.stroke, "#38bdf8"),
          assert.equal(track.width, 0.35),
          assert.approx(track.position[0], -8, 0.0001),
          assert.approx(track.position[1], -1, 0.0001),
          assert.approx(track.size[0], switchWidth, 0.0001),
          assert.approx(track.size[1], 2, 0.0001),
          assert.equal(knob.type, "circle"),
          assert.approx(knob.center[0], options.position[0] + options.size[1] / 2, 0.0001),
          assert.approx(knob.center[1], centerY, 0.0001),
          assert.equal(label.type, "text"),
          assert.equal(label.text, options.label),
          assert.approx(label.position[0], options.position[0] + switchWidth + labelGap, 0.0001),
          assert.approx(label.position[1], baselineY, 0.0001)
        ];
      };
    },
    {
      name: "renders on state";
      test: (toggle) =>
      {
        on: toggle(options + { value: true; }, null);
        track: on.graphics[0];
        knob: on.graphics[1];
        switchWidth: options.size[1] * 1.8;
        eval
        [
          assert.equal(track.fill, "#0f766e"),
          assert.approx(knob.center[0], options.position[0] + switchWidth - options.size[1] / 2, 0.0001)
        ];
      };
    },
    {
      name: "highlights on hover and clears when leaving";
      test: (toggle) =>
      {
        initial: toggle(options, null);
        hoverStep: initial.step(moveInside);
        hovered: toggle(options, hoverStep.state);
        leaveStep: hovered.step(moveOutside);
        left: toggle(options, leaveStep.state);
        eval
        [
          assert.equal(hoverStep.state.hovered, true),
          assert.equal(hovered.graphics[0].stroke, "#fbbf24"),
          assert.equal(hovered.graphics[0].width, 0.6),
          assert.equal(leaveStep.state.hovered, false),
          assert.equal(left.graphics[0].stroke, "#38bdf8"),
          assert.equal(left.graphics[0].width, 0.35)
        ];
      };
    },
    {
      name: "emits change event on click";
      test: (toggle) =>
      {
        initial: toggle(options, null);
        step: initial.step(downInside);
        eval
        [
          assert.equal(step.state.hovered, true),
          assert.equal(Len(step.events), 1),
          assert.equal(step.events[0].type, "ui"),
          assert.equal(step.events[0].action, "change"),
          assert.equal(step.events[0].value, true)
        ];
      };
    },
    {
      name: "returns null for pointer move outside when idle";
      test: (toggle) =>
      {
        initial: toggle(options, null);
        eval
        [
          assert.isnull(initial.step(moveOutside))
        ];
      };
    },
    {
      name: "ignores non-pointer events";
      test: (toggle) =>
      {
        initial: toggle(options, null);
        eval
        [
          assert.isnull(initial.step(nonPointer))
        ];
      };
    }
  ];
}


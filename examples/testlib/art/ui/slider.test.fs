{
  options:
  {
    position: [-8, -1];
    size: [16, 2];
    min: 0;
    max: 10;
    value: 5;
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
    point: { x: 4; y: 0; };
  };

  moveRight:
  {
    type: "pointer";
    action: "move";
    point: { x: 8; y: 0; };
  };

  up:
  {
    type: "pointer";
    action: "up";
    point: { x: 8; y: 0; };
  };

  eval
  [
    {
      name: "renders base track and knob";
      test: (slider) =>
      {
        initial: slider(options, null);
        track: initial.graphics[0];
        bar: initial.graphics[1];
        knob: initial.graphics[2];
        eval
        [
          assert.equal(track.type, "rect"),
          assert.equal(track.fill, "#1e293b"),
          assert.equal(track.stroke, "#38bdf8"),
          assert.equal(track.width, 0.35),
          assert.equal(bar.type, "line"),
          assert.equal(knob.type, "circle"),
          assert.approx(knob.center[0], 0, 0.0001),
          assert.approx(knob.center[1], 0, 0.0001)
        ];
      };
    },
    {
      name: "renders label when provided";
      test: (slider) =>
      {
        withLabel: slider(options + { label: "Volume"; }, null);
        labelNode: withLabel.graphics[3];
        eval
        [
          assert.equal(Len(withLabel.graphics), 4),
          assert.equal(labelNode.type, "text"),
          assert.equal(labelNode.text, "Volume"),
          assert.approx(labelNode.position[0], -8, 0.0001),
          assert.approx(labelNode.position[1], 2.2, 0.0001)
        ];
      };
    },
    {
      name: "highlights on hover and clears when leaving";
      test: (slider) =>
      {
        initial: slider(options, null);
        hoverStep: initial.step(moveInside);
        hovered: slider(options, hoverStep.state);
        leaveStep: hovered.step(moveOutside);
        left: slider(options, leaveStep.state);
        eval
        [
          assert.equal(hoverStep.state.hovered, true),
          assert.equal(Len(hoverStep.events), 0),
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
      name: "returns null for pointer move outside when idle";
      test: (slider) =>
      {
        initial: slider(options, null);
        eval
        [
          assert.isnull(initial.step(moveOutside))
        ];
      };
    },
    {
      name: "emits change events when dragging";
      test: (slider) =>
      {
        initial: slider(options, null);
        downStep: initial.step(downInside);
        downValue: downStep.events[0].value;
        afterDown: slider(options + { value: downValue; }, downStep.state);

        moveStep: afterDown.step(moveRight);
        moveValue: moveStep.events[0].value;
        afterMove: slider(options + { value: moveValue; }, moveStep.state);

        upStep: afterMove.step(up);
        eval
        [
          assert.equal(downStep.state.dragging, true),
          assert.equal(Len(downStep.events), 1),
          assert.equal(downStep.events[0].type, "ui"),
          assert.equal(downStep.events[0].action, "change"),
          assert.approx(downValue, 7.5, 0.0001),
          assert.approx(afterDown.graphics[2].center[0], 4, 0.0001),
          assert.equal(Len(moveStep.events), 1),
          assert.approx(moveValue, 10, 0.0001),
          assert.approx(afterMove.graphics[2].center[0], 8, 0.0001),
          assert.equal(upStep.state.dragging, false),
          assert.equal(Len(upStep.events), 0)
        ];
      };
    }
  ];
}

{
  options:
  {
    radius: 1;
    fill: "#e2e8f0";
    hoverFill: "#334155";
    activeFill: "#fbbf24";
    stroke: "#0f172a";
    width: 0.2;
    activeScale: 1.4;
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
    point: { x: 4; y: 0; };
  };

  downInside:
  {
    type: "pointer";
    action: "down";
    pointer: { id: 7; };
    point: { x: 0.2; y: 0.2; };
  };

  dragMove:
  {
    type: "pointer";
    action: "move";
    pointer: { id: 7; };
    point: { x: 1; y: 2; };
  };

  up:
  {
    type: "pointer";
    action: "up";
    pointer: { id: 7; };
    point: { x: 1; y: 2; };
  };

  eval
  [
    {
      name: "renders a circle at the point";
      test: (dragdot) =>
      {
        initial: dragdot(options + { point: [0, 0]; }, null);
        dot: initial.graphics[0];
        eval
        [
          assert.equal(dot.type, "circle"),
          assert.approx(dot.center[0], 0, 0.0001),
          assert.approx(dot.center[1], 0, 0.0001),
          assert.approx(dot.radius, 1, 0.0001),
          assert.equal(dot.fill, "#e2e8f0")
        ];
      };
    },
    {
      name: "highlights on hover and clears when leaving";
      test: (dragdot) =>
      {
        initial: dragdot(options + { point: [0, 0]; }, null);
        hoverStep: initial.step(moveInside);
        hovered: dragdot(options + { point: [0, 0]; }, hoverStep.state);
        leaveStep: hovered.step(moveOutside);
        left: dragdot(options + { point: [0, 0]; }, leaveStep.state);
        eval
        [
          assert.equal(hoverStep.state.hovered, true),
          assert.equal(hovered.graphics[0].fill, "#334155"),
          assert.equal(leaveStep.state.hovered, false),
          assert.equal(left.graphics[0].fill, "#e2e8f0")
        ];
      };
    },
    {
      name: "tracks pointer capture and updates point while dragging";
      test: (dragdot) =>
      {
        initial: dragdot(options + { point: [0, 0]; }, null);
        downStep: initial.step(downInside);
        afterDown: dragdot(options + { point: [0, 0]; }, downStep.state);
        moveStep: afterDown.step(dragMove);
        movedPoint: moveStep.events[0].value;
        afterMove: dragdot(options + { point: movedPoint; }, moveStep.state);
        upStep: afterMove.step(up);

        eval
        [
          assert.equal(downStep.events[0].action, "dragstart"),
          assert.equal(downStep.state.pointerId, 7),
          assert.equal(moveStep.events[0].action, "change"),
          assert.approx(movedPoint[0], 0.8, 0.0001),
          assert.approx(movedPoint[1], 1.8, 0.0001),
          assert.approx(afterMove.graphics[0].center[0], 0.8, 0.0001),
          assert.approx(afterMove.graphics[0].center[1], 1.8, 0.0001),
          assert.equal(afterMove.graphics[0].fill, "#fbbf24"),
          assert.equal(upStep.events[0].action, "dragend"),
          assert.isnull(upStep.state.pointerId)
        ];
      };
    }
  ];
}

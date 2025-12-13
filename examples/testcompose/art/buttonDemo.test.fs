{
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

  eval
  [
    {
      name: "highlights rectangle on hover";
      test: (scene) =>
      {
        initial: scene(null);
        hoverStep: initial.step(moveInside);
        hoveredScene: scene(hoverStep.state);
        eval
        [
          assert.equal(hoverStep.state.hovered, true),
          assert.equal(hoveredScene.graphics[0].fill, "#334155"),
          assert.equal(hoveredScene.graphics[0].stroke, "#fbbf24")
        ];
      };
    },
    {
      name: "clears highlight when leaving";
      test: (scene) =>
      {
        hovered: scene({ hovered: true; count: 0; });
        leaveStep: hovered.step(moveOutside);
        leftScene: scene(leaveStep.state);
        eval
        [
          assert.equal(leaveStep.state.hovered, false),
          assert.equal(leftScene.graphics[0].fill, "#1e293b"),
          assert.equal(leftScene.graphics[0].stroke, "#38bdf8")
        ];
      };
    },
    {
      name: "increments count on click inside";
      test: (scene) =>
      {
        initial: scene({ hovered: true; count: 0; });
        clickStep: initial.step(downInside);
        clickedScene: scene(clickStep.state);
        eval
        [
          assert.equal(clickStep.state.count, 1),
          assert.equal(clickedScene.graphics[2].text, "Clicks\n1")
        ];
      };
    },
    {
      name: "ignores click outside rectangle";
      test: (scene) =>
      {
        initial: scene(null);
        clickStep: initial.step(downOutside);
        eval
        [
          assert.isnull(clickStep)
        ];
      };
    }
  ];
}

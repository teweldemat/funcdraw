{
  pointerDown:
  {
    type: "pointer";
    action: "down";
  };

  pointerUp:
  {
    type: "pointer";
    action: "up";
  };

  eval
  [
    {
      name: "toggles fill on pointer down events";
      test: (scene) =>
      {
        initial: scene(null);
        firstStep: initial.step(pointerDown);
        nextScene: scene(firstStep.state);
        secondStep: nextScene.step(pointerDown);
        eval
        [
          assert.equal(initial.graphics[0].fill, "#38bdf8"),
          assert.equal(firstStep.state.fill, "#f472b6"),
          assert.equal(secondStep.state.fill, "#38bdf8")
        ];
      };
    },
    {
      name: "ignores non-down actions";
      test: (scene) =>
      {
        initial: scene(null);
        noChange: initial.step(pointerUp);
        eval
        [
          assert.equal(noChange.state.fill, "#38bdf8")
        ];
      };
    }
  ];
}

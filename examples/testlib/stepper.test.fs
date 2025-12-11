{
  eval [
    {
      name: "steps model state across five events";
      test: () =>
      {
        model: (inState) =>
        {
          graphics:
          {
            type: "line";
            from: [0, 0];
            to: [0, 0];
          };
          step: (event) =>
          {
            state: (inState??0) + 1;
            events: [];
          };
        };
        s1: model(null).step(null);
        s2: model(s1.state).step(null);
        s3: model(s2.state).step(null);
        s4: model(s3.state).step(null);
        s5: model(s4.state).step(null);

        eval
        [
          assert.equal(s1.state, 1),
          assert.equal(s2.state, 2),
          assert.equal(s3.state, 3),
          assert.equal(s4.state, 4),
          assert.equal(s5.state, 5)
        ];
      };
    }
  ];
}

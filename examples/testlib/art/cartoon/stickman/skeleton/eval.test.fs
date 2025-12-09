{
  exportsAlias:{
    name:"eval exports builders and defaults";
    test:(module)=> {
      built:module.build({ position:[2, 3] });
      normalized:normalize({ position:[4, 5] });
      builtNormalized:module.buildNormalized(normalized);

      eval [
        assert.noerror(built),
        assert.noerror(builtNormalized),
        assert.equal(module.defaultMeasurements.torso.width, defaults.defaultMeasurements.torso.width),
        assert.equal(built.skeleton.position[0], 2),
        assert.equal(built.skeleton.position[1], 3),
        assert.equal(builtNormalized.skeleton.position[0], 4),
        assert.equal(builtNormalized.skeleton.position[1], 5),
        assert.equal(module.defaults.positionY, defaults.positionY)
      ];
    };
  };

  eval [exportsAlias];
}

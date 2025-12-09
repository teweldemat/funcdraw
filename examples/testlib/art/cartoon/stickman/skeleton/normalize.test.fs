{
  usesDefaults:{
    name:"normalize fills defaults when options are missing";
    test:(normalizeFn)=> {
      normalized:normalizeFn({});
      eval [
        assert.equal(normalized.position[0], 0),
        assert.equal(normalized.position[1], defaults.positionY),
        assert.isnull(normalized.handOverrides),
        assert.isnull(normalized.legOverrides),
        assert.equal(normalized.measurements.torso.width, defaults.defaultMeasurements.torso.width),
        assert.equal(normalized.measurements.hands.left.effectorCoordinate[0], defaults.defaultMeasurements.hands.left.effectorCoordinate[0])
      ];
    };
  };

  appliesOverrides:{
    name:"normalize forwards provided overrides";
    test:(normalizeFn)=> {
      options:{
        position:[5, 7];
        measurements:{
          torso:{ width:12 };
          hands:{ left:{ effectorCoordinate:[-3, 2] } };
          legs:{ right:{ effectorCoordinate:[4, -9] } };
        };
      };
      normalized:normalizeFn(options);
      eval [
        assert.equal(normalized.position[0], 5),
        assert.equal(normalized.position[1], 7),
        assert.equal(normalized.measurements.torso.width, 12),
        assert.equal(normalized.measurements.hands.left.effectorCoordinate[0], -3),
        assert.equal(normalized.measurements.legs.right.effectorCoordinate[1], -9),
        assert.equal(normalized.handOverrides.left.effectorCoordinate[0], -3),
        assert.equal(normalized.legOverrides.right.effectorCoordinate[1], -9)
      ];
    };
  };

  eval [usesDefaults, appliesOverrides];
}

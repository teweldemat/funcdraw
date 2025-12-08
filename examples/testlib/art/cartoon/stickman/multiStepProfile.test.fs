{
  mutationSuite:{
    name:"multiStepProfile does not mutate its inputs across calls";
    test:(builder)=> {
      anchor:[0, 18.6];
      baseMeasurements:{
        torso:{ direction:"right"; height:20; width:2 };
        head:{ direction:"right"; verticalExtent:5 };
        hands:{
          left:{ effectorCoordinate:[-7.8, 4.7]; upperLength:8; lowerLength:8 };
          right:{ effectorCoordinate:[7.8, 4.7]; upperLength:8; lowerLength:8 };
        };
        legs:{
          left:{ upperLength:12.4; lowerLength:11.6; effectorCoordinate:[-4, -18.6] };
          right:{ upperLength:12.4; lowerLength:11.6; effectorCoordinate:[4, -18.6] };
        };
      };

      args:{
        initialPosition:anchor;
        initialMeasurements:baseMeasurements;
        displacement:30;
        progress:0.4;
        strideLength:12;
        handSwing:{ enabled:true; mode:"mirror"; amplitude:14; lift:1.8; forwardOffset:0 };
        direction:"right";
      };

      first:builder(args);
      second:builder(args);

      eval [
        assert.noerror(first),
        assert.noerror(second),
        assert.equal(baseMeasurements.legs.left.effectorCoordinate[0], -4),
        assert.equal(baseMeasurements.legs.left.effectorCoordinate[1], -18.6),
        assert.equal(baseMeasurements.hands.left.effectorCoordinate[0], -7.8),
        assert.equal(baseMeasurements.hands.left.effectorCoordinate[1], 4.7),
        assert.equal(first.position[0], second.position[0]),
        assert.equal(first.position[1], second.position[1]),
        assert.equal(first.measurements.legs.left.effectorCoordinate[0], second.measurements.legs.left.effectorCoordinate[0]),
        assert.equal(first.measurements.hands.left.effectorCoordinate[1], second.measurements.hands.left.effectorCoordinate[1])
      ];
    };
  };

  eval [mutationSuite];
}

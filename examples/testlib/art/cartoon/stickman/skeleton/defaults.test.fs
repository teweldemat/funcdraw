{
  defaultsSuite:{
    name:"defaults exposes baseline skeleton values";
    test:(res)=> {
      hands:res.defaultMeasurements.hands;
      legs:res.defaultMeasurements.legs;
      leftHand:hands.left;
      rightHand:hands.right;
      leftLeg:legs.left;
      rightLeg:legs.right;

      eval [
        assert.noerror(res),
        assert.approx(res.legTotal, res.legUpper + res.legLower, 0.000001),
        assert.approx(res.positionY, res.legTotal + res.footThickness, 0.000001),
        assert.equal(res.defaultMeasurements.torso.width, res.torsoWidth),
        assert.equal(res.defaultMeasurements.torso.height, res.torsoHeight),
        assert.equal(leftHand.effectorCoordinate[0], -res.handOffset),
        assert.equal(leftHand.effectorCoordinate[1], res.handDrop),
        assert.equal(rightHand.effectorCoordinate[0], res.handOffset),
        assert.equal(rightHand.effectorCoordinate[1], res.handDrop),
        assert.equal(leftLeg.effectorCoordinate[1], -res.legTotal),
        assert.equal(rightLeg.effectorCoordinate[1], -res.legTotal)
      ];
    };
  };

  eval [defaultsSuite];
}

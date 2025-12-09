{
  buildSkeleton:{
    name:"build produces skeleton frames from normalized input";
    test:(builder)=> {
      normalized:normalize({});
      result:builder(normalized);
      sk:result.skeleton;

      distance:(a, b)=> {
        dx:a[0] - b[0];
        dy:a[1] - b[1];
        eval math.sqrt(dx * dx + dy * dy);
      };

      eval [
        assert.noerror(result),
        assert.equal(sk.position[0], normalized.position[0]),
        assert.equal(sk.position[1], normalized.position[1]),
        assert.equal(sk.torso.width, normalized.measurements.torso.width),
        assert.equal(sk.torso.height, normalized.measurements.torso.height),
        assert.equal(sk.head.attachmentPoint[0], sk.torso.headAttachmentPoint[0]),
        assert.equal(sk.head.attachmentPoint[1], sk.torso.headAttachmentPoint[1]),
        assert.equal(sk.hands.left.attachmentPoint[0], sk.torso.handAttachmentPoints.left[0]),
        assert.equal(sk.legs.left.attachmentPoint[1], sk.torso.legAttachmentPoints.left[1]),
        assert.approx(
          distance(sk.legs.left.attachmentPoint, sk.legs.left.reachTarget),
          sk.legs.left.lengths.upper + sk.legs.left.lengths.lower,
          0.0001
        ),
        assert.approx(
          distance(sk.hands.left.attachmentPoint, sk.hands.left.reachTarget),
          sk.hands.left.lengths.upper + sk.hands.left.lengths.lower,
          0.0001
        )
      ];
    };
  };

  eval [buildSkeleton];
}

{
  build: (args) =>
  {
    geometry: args.geometry;
    headCenter: args.headCenter;
    anchor: args.anchor;
    hit: args.hit;
    handleRadius: args.handleRadius;

    pickPart: (eventPoint) =>
    {
      p: [eventPoint.x, eventPoint.y];
      eval
        if hit.hitCircle(p, headCenter, geometry.measurements.headRadius) then "head"
        else if hit.hitSegment(p, geometry.neck.from, geometry.neck.to) then "neck"
        else if hit.hitSegment(p, geometry.body.from, geometry.body.to) then "body"
        else if hit.hitSegment(p, geometry.leftHand.from, geometry.leftHand.joint) or hit.hitSegment(p, geometry.leftHand.joint, geometry.leftHand.to) then "leftArm"
        else if hit.hitSegment(p, geometry.rightHand.from, geometry.rightHand.joint) or hit.hitSegment(p, geometry.rightHand.joint, geometry.rightHand.to) then "rightArm"
        else if hit.hitSegment(p, geometry.leftLeg.from, geometry.leftLeg.joint) or hit.hitSegment(p, geometry.leftLeg.joint, geometry.leftLeg.to) then "leftLeg"
        else if hit.hitSegment(p, geometry.rightLeg.from, geometry.rightLeg.joint) or hit.hitSegment(p, geometry.rightLeg.joint, geometry.rightLeg.to) then "rightLeg"
        else if hit.hitSegment(p, geometry.leftHandAttachment, geometry.rightHandAttachment) then "shoulders"
        else if hit.hitSegment(p, geometry.leftLegAttachment, geometry.rightLegAttachment) then "thighs"
        else null;
    };

    pickHandle: (eventPoint) =>
    {
      p: [eventPoint.x, eventPoint.y];
      eval
        if hit.hitCircle(p, anchor, handleRadius) then "anchor"
        else if hit.hitCircle(p, geometry.leftHand.to, handleRadius) then "leftHandEnd"
        else if hit.hitCircle(p, geometry.rightHand.to, handleRadius) then "rightHandEnd"
        else if hit.hitCircle(p, geometry.leftLeg.to, handleRadius) then "leftLegEnd"
        else if hit.hitCircle(p, geometry.rightLeg.to, handleRadius) then "rightLegEnd"
        else null;
    };

    eval { pickPart; pickHandle; };
  };
}

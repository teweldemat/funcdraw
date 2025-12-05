{
  view:constants.zoomedInView;
  groundY:0;
  markers:(range(0, 10) map (i)=> {
    x:view.left + 8 + i * 12;
    eval { type:"circle"; center:[x, groundY]; radius:0.6; fill:"#94a3b8"; stroke:"#94a3b8"; width:0.12 };
  }) + [
    { type:"line"; from:[view.left, groundY]; to:[view.right, groundY]; stroke:"#94a3b8"; width:0.5 }
  ];

  cartoonLibrary:package("@funcdraw/testlib").cartoon;
  stickmanModule:cartoonLibrary.stickman;
  steperBuilder:stickmanModule.steperManProfile;
  staticBuilder:stickmanModule.static;
  consts:if constants != null then constants else {};
  timeValue:t;

  baseMeasurements:{
    torso:{ direction:"right"; height:consts.shared?.torso?.height ?? 22; width:consts.shared?.torso?.width ?? 12 };
    head:{ direction:"right"; verticalExtent:consts.shared?.head?.verticalExtent ?? 9 };
    hands:{
      left:{ effectorCoordinate:consts.shared?.hands?.left ?? [-7.8, 4.7] };
      right:{ effectorCoordinate:consts.shared?.hands?.right ?? [7.8, 4.7] };
    };
    legs:{
      left:{ upperLength:consts.shared?.legs?.lengths?.upper ?? 12.4; lowerLength:consts.shared?.legs?.lengths?.lower ?? 11.6; effectorCoordinate:[-4, -18.6] };
      right:{ upperLength:consts.shared?.legs?.lengths?.upper ?? 12.4; lowerLength:consts.shared?.legs?.lengths?.lower ?? 11.6; effectorCoordinate:[4, -18.6] };
    };
  };

  walker: {
    step:steperBuilder({
      position:[0, 18.6];
      measurements:baseMeasurements;
      movingSide:if math.floor(timeValue) % 2 = 0 then "left" else "right";
      movingFeetTargetPoint:[12, 0];
      progress:clamp01(timeValue % 1);
      handSwing:{ amplitude:6; lift:0.23; forwardOffset:0; mode:"sine" };
    });
    pose:staticBuilder({
      position:step.position ?? [0, 18.6];
      measurements:if step.measurements != null then step.measurements else baseMeasurements;
      palette:{ overlayLeg:"#f97316"; overlayHand:"#0ea5e9" };
    });
    eval (pose?.graphics) ?? [];
  };

  eval {
    view:view;
    graphics:markers + walker;
  };

  

  clamp01:(value)=> {
    num:number(value);
    eval if num != num then 0 else if num < 0 then 0 else if num > 1 then 1 else num;
  };
}

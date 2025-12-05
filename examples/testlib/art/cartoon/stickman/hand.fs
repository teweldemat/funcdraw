(options)=>
{
  defaults:{ stroke:"#f97316"; width:0.8 };

  config:helpers.normalizeInput(options ?? {}, {});
  joints:extractJointPoints(config);

  eval if joints = null then {
    graphics:[];
  } else {
    style:helpers.normalizeInput(config.style, {});
    stroke:style.stroke ?? defaults.stroke;
    width:style.width ?? defaults.width;

    graphics:[
      {
        type:"line";
        from:joints.attachment;
        to:joints.hinge;
        stroke:stroke;
        width:width;
      },
      {
        type:"line";
        from:joints.hinge;
        to:joints.effector;
        stroke:stroke;
        width:width;
      }
    ];

    eval { graphics:graphics };
  };

  extractJointPoints:(config)=> {
    jointsInput:config.joints;
    eval if jointsInput = null then null else {
      attachment:helpers.toPoint(jointsInput.attachment ?? jointsInput.shoulder);
      hinge:helpers.toPoint(jointsInput.hinge ?? jointsInput.elbow);
      effector:helpers.toPoint(jointsInput.effector ?? jointsInput.wrist);
      eval if attachment = null or hinge = null or effector = null then null else {
        attachment:attachment;
        hinge:hinge;
        effector:effector;
      };
    };
  };
}

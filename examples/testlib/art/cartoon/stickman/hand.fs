(options)=>
{
  defaults:{ stroke:"#f97316"; width:0.8 };

  config:options ?? {};
  joints:extractJointPoints(config);

  eval if joints = null then {
    graphics:[];
  } else {
    style:config.style ?? {};
    stroke:style.stroke ?? defaults.stroke;
    width:style.width ?? defaults.width;

    lines:[
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

    eval {
      graphics:[{
        type:"testlib/cartoon/stickman/hand";
        name:"testlib/cartoon/stickman/hand";
        graphics:lines;
      }];
    };
  };

  extractJointPoints:(config)=> {
    jointsInput:config.joints;
    eval if jointsInput = null then null else {
      attachment:jointsInput.attachment ?? jointsInput.shoulder;
      hinge:jointsInput.hinge ?? jointsInput.elbow;
      effector:jointsInput.effector ?? jointsInput.wrist;
      eval if attachment = null or hinge = null or effector = null then null else {
        attachment:attachment;
        hinge:hinge;
        effector:effector;
      };
    };
  };
}

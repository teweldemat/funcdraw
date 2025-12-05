(options)=>
{
  defaults:{ stroke:"#0ea5e9"; width:1.1 };

  config:options ?? {};
  joints:extractJointPoints(config);

  eval if joints = null then error("leg model now requires joints from the skeleton (leg.joints missing)") else {
    style:config.style ?? {};
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
      attachment:jointsInput.attachment ?? jointsInput.hip;
      hinge:jointsInput.hinge ?? jointsInput.knee;
      effector:jointsInput.effector ?? jointsInput.ankle;

      eval if attachment = null or hinge = null or effector = null then null else {
        attachment:attachment;
        hinge:hinge;
        effector:effector;
      };
    };
  };
}

{
  center:constants.center;
  radius:constants.radius;
  view:constants.view;

  eval {
    view:view;
    graphics:[
      {
        type:"circle";
        center:center;
        radius:radius;
        fill:constants.fill;
        stroke:constants.stroke;
        width:constants.strokeWidth;
      }
    ];
  };
}

{
  diagram: import("@funcdraw/diagram");

  sourceModule: diagram.box([-8, 0], [10, 6], { label: 'Source' });
  targetModule: diagram.box([8, 0], [10, 6], { label: 'Target' });
  queueModule: diagram.box([-8, -9], [9, 5], {
    label: 'Queue';
    fill: '#fef3c7';
    stroke: '#b45309';
    strokeWidth: 0.25;
    labelSize: 2.1;
  });
  connectorModule: diagram.connector.line({
    start: sourceModule.attachments.right;
    end: targetModule.attachments.left;
  });

  sourceTopPoint: sourceModule.attachments.top.point;
  targetTopPoint: targetModule.attachments.top.point;
  elbowY: sourceTopPoint[1] - 4;
  topPolyline: diagram.connector.polyline({
    start: sourceModule.attachments.top;
    end: targetModule.attachments.top;
    via: [
      [sourceTopPoint[0], elbowY],
      [targetTopPoint[0], elbowY]
    ];
    style: {
      stroke: '#22c55e';
      width: 0.3;
      dash: [0.6, 0.4];
    };
    arrow: {
      stroke: '#15803d';
      fill: '#22c55e';
      headLength: 0.9;
      baseWidth: 0.65;
    };
  });

  queueRightPoint: queueModule.attachments.right.point;
  targetBottomPoint: targetModule.attachments.bottom.point;
  elbowX: queueRightPoint[0] + 6;
  elbowY2: targetBottomPoint[1] + 3;
  bottomConnector: diagram.connector.polyline({
    start: queueModule.attachments.right;
    end: targetModule.attachments.bottom;
    via: [
      [elbowX, queueRightPoint[1]],
      [elbowX, elbowY2],
      [targetBottomPoint[0], elbowY2]
    ];
    style: {
      stroke: '#a855f7';
      width: 0.3;
      dash: [0.4, 0.4];
    };
    arrow: {
      stroke: '#7c3aed';
      fill: '#a855f7';
      headLength: 0.8;
      baseWidth: 0.55;
    };
    startArrow: {
      stroke: '#7c3aed';
      fill: '#a855f7';
      headLength: 0.8;
      baseWidth: 0.55;
    };
  });

  eval [
    sourceModule.graphics,
    targetModule.graphics,
    queueModule.graphics,
    connectorModule.graphics,
    topPolyline.graphics,
    bottomConnector.graphics
  ];
}

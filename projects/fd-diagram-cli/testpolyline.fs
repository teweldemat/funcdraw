{
  diagram: import("@funcdraw/diagram");
  eval diagram.connector.polyline({
    start: { point: [-8, -3] };
    end: { point: [8, -3] };
    via: [
      [-6, -7],
      [6, -7]
    ];
    style: {
      stroke: '#22c55e';
      width: 0.35;
      dash: [0.8, 0.4];
    };
    arrow: {
      stroke: '#15803d';
      fill: '#22c55e';
      headLength: 1.1;
      baseWidth: 0.8;
    };
    startArrow: {
      stroke: '#15803d';
      fill: '#22c55e';
      headLength: 1.1;
      baseWidth: 0.8;
    };
  }).graphics;
}

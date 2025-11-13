{
  flipPoint: (point) => [point[0], -point[1]];
  raysCount: 12;
  makeRay: (index) => {
    angle: index * (2 * math.pi / raysCount);
    inner: 2.2;
    outer: 3.6;
    eval {
      type: 'line',
      from: flipPoint([
        math.sin(angle) * inner,
        6 + math.cos(angle) * inner
      ]),
      to: flipPoint([
        math.sin(angle) * outer,
        6 + math.cos(angle) * outer
      ]),
      stroke: '#fde047',
      width: 0.3
    };
  };
  rays: range(0, raysCount) map makeRay;
  eval [
    {
      type: 'circle',
      center: flipPoint([0, 6]),
      radius: 2.6,
      stroke: '#fcd34d',
      width: 0.2,
      fill: 'rgba(253, 224, 71, 0.32)'
    },
    {
      type: 'circle',
      center: flipPoint([0, 6]),
      radius: 2.1,
      fill: '#fde047',
      stroke: '#f97316',
      width: 0.35
    },
    rays
  ];
}

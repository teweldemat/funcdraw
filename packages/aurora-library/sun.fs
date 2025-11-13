(centerParam, radiusParam, optionsParam) => {
  flipPoint: (point) => [point[0], -point[1]];

  center: centerParam ?? [0, 6];
  coreRadius: radiusParam ?? 2.1;
  options: optionsParam ?? {};

  haloRadius: options.haloRadius ?? coreRadius * 1.25;
  haloColor: options.haloColor ?? 'rgba(253, 224, 71, 0.32)';
  haloStroke: options.haloStroke ?? '#fcd34d';

  coreColor: options.coreColor ?? '#fde047';
  coreStroke: options.coreStroke ?? '#f97316';

  rayColor: options.rayColor ?? '#fde047';
  rayCount: options.rayCount ?? 12;
  rayInner: options.rayInner ?? haloRadius;
  rayOuter: options.rayOuter ?? coreRadius * 1.75;
  rayWidth: options.rayWidth ?? (coreRadius * 0.14);
  rayRotation: options.rayRotation ?? 0;

  makeRay: (index) => {
    angle: rayRotation + index * (2 * math.pi / rayCount);
    fromPoint: [
      center[0] + math.sin(angle) * rayInner,
      center[1] + math.cos(angle) * rayInner
    ];
    toPoint: [
      center[0] + math.sin(angle) * rayOuter,
      center[1] + math.cos(angle) * rayOuter
    ];
    eval {
      type: 'line';
      from: flipPoint(fromPoint);
      to: flipPoint(toPoint);
      stroke: rayColor;
      width: rayWidth;
    };
  };

  rays:
    range(0, rayCount) map makeRay;

  eval [
    {
      type: 'circle';
      center: flipPoint(center);
      radius: haloRadius;
      fill: haloColor;
      stroke: haloStroke;
      width: coreRadius * 0.08;
    },
    {
      type: 'circle';
      center: flipPoint(center);
      radius: coreRadius;
      fill: coreColor;
      stroke: coreStroke;
      width: coreRadius * 0.12;
    },
    rays
  ];
}

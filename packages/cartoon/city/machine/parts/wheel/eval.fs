(centerParam, radiusParam, optionsParam) => {
  center: centerParam ?? [0, 0];
  radius: radiusParam ?? 1.1;
  options: optionsParam ?? {};

  tireColor: options.tireColor ?? '#0f172a';
  sidewallColor: options.sidewallColor ?? '#1e293b';
  rimColor: options.rimColor ?? '#e2e8f0';
  spokeColor: options.spokeColor ?? '#94a3b8';
  hubColor: options.hubColor ?? '#f8fafc';

  rimRadius: radius * 0.7;
  hubRadius: radius * 0.27;
  spokeWidth: radius * 0.22;

  tire: {
    type: 'circle';
    center: center;
    radius: radius;
    fill: tireColor;
    stroke: '#0b1120';
    width: radius * 0.12;
  };

  sidewall: {
    type: 'circle';
    center: center;
    radius: radius * 0.86;
    fill: sidewallColor;
    stroke: 'transparent';
  };

  rim: {
    type: 'circle';
    center: center;
    radius: rimRadius;
    fill: rimColor;
    stroke: '#cbd5f5';
    width: radius * 0.06;
  };

  verticalSpoke: {
    type: 'rect';
    position: [center[0] - spokeWidth / 2, center[1] - rimRadius];
    size: [spokeWidth, rimRadius * 2];
    fill: spokeColor;
    stroke: '#94a3b8';
    width: radius * 0.04;
  };

  horizontalSpoke: {
    type: 'rect';
    position: [center[0] - rimRadius, center[1] - spokeWidth / 2];
    size: [rimRadius * 2, spokeWidth];
    fill: spokeColor;
    stroke: '#94a3b8';
    width: radius * 0.04;
  };

  hub: {
    type: 'circle';
    center: center;
    radius: hubRadius;
    fill: hubColor;
    stroke: '#94a3b8';
    width: radius * 0.04;
  };

  lugMarks:
    [0, 1, 2, 3] map (index) => {
      offsetX: (index - 1.5) * hubRadius * 0.55;
      eval {
        type: 'circle';
        center: [center[0] + offsetX, center[1]];
        radius: radius * 0.06;
        fill: '#64748b';
        stroke: 'transparent';
      };
    };

  totalHeight: radius * 2.2;
  halfHeight: totalHeight / 2;

  eval {
    graphics: [tire, sidewall, rim, verticalSpoke, horizontalSpoke, hub, lugMarks];
    width: radius * 2.6;
    height: totalHeight;
    above: halfHeight;
    below: halfHeight;
  };
}

{
  wallFill: "#e2e8f0";
  wallStroke: "#0f172a";
  wallWidth: 0.35;

  roofFill: "#ef4444";
  roofStroke: "#7f1d1d";
  roofWidth: 0.45;

  windowStroke: "#0f172a";
  windowWidth: 0.3;

  doorFill: "#b45309";
  doorStroke: "#0f172a";
  doorWidth: 0.35;
  knobFill: "#fbbf24";
  knobStroke: "#0f172a";
  knobWidth: 0.2;

  makeWindow: (position, size, lightColor) =>
  {
    x: position[0];
    y: position[1];
    w: size[0];
    h: size[1];
    midX: x + w / 2;
    midY: y + h / 2;
    eval
    [
      { type: "rect"; name: "window"; position; size; fill: lightColor; stroke: windowStroke; width: windowWidth; },
      { type: "line"; name: "window-mullion"; from: [midX, y]; to: [midX, y + h]; stroke: windowStroke; width: windowWidth / 2; },
      { type: "line"; name: "window-mullion"; from: [x, midY]; to: [x + w, midY]; stroke: windowStroke; width: windowWidth / 2; }
    ];
  };

  makeDoor: (position, size, openProgress, lightColor) =>
  {
    x: position[0];
    y: position[1];
    w: size[0];
    h: size[1];
    angle: openProgress * math.Pi / 2;
    visibleW: w * math.Cos(angle);
    knobX: x + visibleW * 0.72;
    knobY: y + h * 0.45;
    knobR: math.Min(w, h) * 0.05;
    eval
    [
      { type: "rect"; name: "door-opening"; position; size; fill: lightColor; stroke: wallStroke; width: wallWidth; },
      { type: "polygon"; name: "door"; points: [[x, y], [x + visibleW, y], [x + visibleW, y + h], [x, y + h]]; fill: doorFill; stroke: doorStroke; width: doorWidth; },
      { type: "circle"; name: "door-knob"; center: [knobX, knobY]; radius: knobR; fill: knobFill; stroke: knobStroke; width: knobWidth; }
    ];
  };
}

(options) =>
{
  center: options.center;
  radius: options.radius;
  fill: options.fill;
  stroke: options.stroke;
  width: options.width;
  rayCount: options.rayCount;
  rayLength: options.rayLength;
  rayColor: options.rayColor;
  rayWidth: options.rayWidth;
  rotation: options.rotation;

  eval if radius <= 0 then error("expected radius > 0") else
  if rayCount < 1 then error("expected rayCount >= 1") else
  {
    rays:
      Range(0, rayCount) map (k, idx) =>
      {
        angle: rotation + 2 * math.Pi * k / rayCount;
        dir: [math.Cos(angle), math.Sin(angle)];
        from: [center[0] + dir[0] * radius * 1.1, center[1] + dir[1] * radius * 1.1];
        to: [center[0] + dir[0] * (radius + rayLength), center[1] + dir[1] * (radius + rayLength)];
        eval { type: "line"; name: "sun-ray"; from; to; stroke: rayColor; width: rayWidth; };
      };

    body:
    {
      type: "circle";
      name: "sun";
      center;
      radius;
      fill;
      stroke;
      width;
    };

    eval rays + [body];
  };
}

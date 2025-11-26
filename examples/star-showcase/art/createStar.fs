(cx, cy, radius, color)=>
{
  type:"polygon";
  points:starpoints(cx ?? 0, cy ?? 0, radius ?? 3);
  fill:color ?? "#facc15";
  stroke:"#0f172a";
  width:0.35;

  starpoints:(centerX, centerY, size)=>([0,1,2,3,4] map (idx) =>
      {
        angle:(idx * 0.4) * math.pi;
        angle2:((idx + 0.5) * 0.4) * math.pi;
        eval [
          [centerX + math.cos(angle) * size, centerY + math.sin(angle) * size],
          [centerX + math.cos(angle2) * size * 0.5, centerY + math.sin(angle2) * size * 0.5]
        ]
      }) reduce (points, pair) => points + pair;
}

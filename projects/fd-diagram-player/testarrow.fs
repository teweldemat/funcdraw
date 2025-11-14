{
  diagram: import("@funcdraw/diagram");
  eval diagram.connector.arrow({
    start: { point: [-4, -2] };
    end: { point: [4, 2] };
    style: {
      stroke: '#0f172a';
      fill: '#fbbf24';
      headLength: 1.6;
      baseWidth: 1.1;
    };
  }).graphics;
}

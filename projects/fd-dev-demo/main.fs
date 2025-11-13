{
  aurora: import("aurora-library");
  meadow: aurora.meadow;
  marker: {
    type: 'circle';
    data: {
      center: [0, 4];
      radius: 1.2;
      fill: '#fbbf24';
      stroke: '#0f172a';
      width: 0.2;
    };
  };

  // Combine the imported meadow shapes with a local marker circle.
  eval [meadow, marker];
}

{
  aurora: import("aurora-library");
  cartoon: import("@funcdraw/cat-cartoon");
  meadow: aurora.meadow();
  featureTree: cartoon.landscape.tree('evergreen', [5, -4], 9);
  marker: {
    type: 'circle';
    center: [0, 4];
    radius: 1.2;
    fill: '#fbbf24';
    stroke: '#0f172a';
    width: 0.2;
  };

  // Combine the imported meadow shapes with a cartoon tree and local marker circle.
  eval [meadow, featureTree.graphics, marker];
}

{
  library: import("@funcdraw/cat-cartoon");
  leftTree: library.landscape.tree('evergreen', [-6, -4], 8);
  centerTree: library.landscape.tree('evergreen', [0, -4], 10);
  rightTree: library.landscape.tree('evergreen', [6, -4], 6);
  combined: [leftTree.graphics, centerTree.graphics, rightTree.graphics];
  eval combined;
}

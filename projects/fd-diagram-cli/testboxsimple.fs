{
  diagram: import("@funcdraw/diagram");
  node: diagram.box([0, 0], [10, 6], {
    label: 'Node',
    cornerRadius: 1.2,
    fill: '#e0f2fe',
    stroke: '#0369a1',
    strokeWidth: 0.3,
    labelSize: 1.2
  });
  eval [node.graphics];
}

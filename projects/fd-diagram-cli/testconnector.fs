{
  diagram: import("@funcdraw/diagram");
  connector: diagram.connector.line({ start: { point: [-2, 0] }, end: { point: [2, 0] } });
  eval connector.graphics;
}

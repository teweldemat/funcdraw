{
  sceneGenerator: scene.scene1 ?? ((timeParam) => { graphics: [] });
  eval sceneGenerator(t ?? 0);
}

{
  basePrepare: scene.prepare ?? ((configParam) => {});
  config: parameters ?? {};
  prepared: basePrepare(config);
  eval prepared;
}

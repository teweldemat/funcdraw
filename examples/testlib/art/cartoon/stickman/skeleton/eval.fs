{
  defaults:defaults;
  normalize:normalize;
  buildSkeleton:build;

  evaluate:(optionsInput)=> build(normalize(optionsInput));

  eval {
    defaults:defaults;
    normalize:normalize;
    build:evaluate;
    buildNormalized:build;
    defaultMeasurements:defaults.defaultMeasurements;
  };
}

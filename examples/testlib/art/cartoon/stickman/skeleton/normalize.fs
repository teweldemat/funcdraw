(optionsInput)=>
{
  defaults:defaults;

  rawOptions:optionsInput ?? {};
  measurementOverrides:rawOptions.measurements ?? {};
  measurements:defaults.defaultMeasurements + measurementOverrides;
  position:if rawOptions.position = null then [0, defaults.positionY] else rawOptions.position;

  eval {
    position:position;
    measurements:measurements;
    handOverrides:measurementOverrides.hands ?? null;
    legOverrides:measurementOverrides.legs ?? null;
    normalizedOptions:rawOptions;
  };
}

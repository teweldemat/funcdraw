(typeOrLocationParam, baseLocationParam, widthParam, optionsParam) => {
  cottage: import("./cottage");
  townhome: import("./townhome");
  modern: import("./modern");
  availableTypes: {
    cottage: cottage,
    townhome: townhome,
    modern: modern
  };

  typeKey:
    if (typeOrLocationParam = 'townhome')
      then 'townhome'
      else if (typeOrLocationParam = 'modern')
        then 'modern'
        else if (typeOrLocationParam = 'cottage')
          then 'cottage'
          else null;

  requestedType: typeKey ?? 'cottage';

  calledWithType: typeKey != null;

  baseLocation: if (calledWithType)
      then baseLocationParam ?? [0, 0]
      else typeOrLocationParam ?? [0, 0];

  width: if (calledWithType)
      then widthParam
      else baseLocationParam;

  options: if (calledWithType)
      then optionsParam ?? {}
      else widthParam ?? {};

  selectedModule: availableTypes[requestedType] ?? cottage;

  return selectedModule(baseLocation, width, options);
}

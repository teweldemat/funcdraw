(typeParam, baseLocationParam, heightParam) => {
  selectedType: typeParam ?? 'evergreen';
  evergreen: import("./evergreen");
  treeModule:
    if (selectedType = 'evergreen')
      then evergreen(baseLocationParam, heightParam)
      else evergreen(baseLocationParam, heightParam);
  return treeModule;
}

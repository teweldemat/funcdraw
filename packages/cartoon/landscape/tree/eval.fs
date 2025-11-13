(typeParam, baseLocationParam, heightParam) => {
  selectedType: typeParam ?? 'evergreen';
  treeModule:
    if (selectedType = 'evergreen')
      then evergreen(baseLocationParam, heightParam)
      else evergreen(baseLocationParam, heightParam);
  return treeModule;
}

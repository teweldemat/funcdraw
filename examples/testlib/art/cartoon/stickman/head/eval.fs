{
  defaults:defaults;
  normalize:normalize;
  render:render;

  evaluate:(attachmentPointInput, configInput)=> render(normalize(attachmentPointInput, configInput));

  eval {
    defaults:defaults;
    normalize:normalize;
    render:render;
    evaluate:evaluate;
  };
}

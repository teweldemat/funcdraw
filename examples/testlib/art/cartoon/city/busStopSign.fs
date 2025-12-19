(options) =>
{
  base: options.base;
  poleHeight: options.poleHeight;
  signSize: options.signSize;
  fill: options.fill;
  stroke: options.stroke;
  width: options.width;
  textColor: options.textColor;

  poleTop: [base[0], base[1] + poleHeight];
  signPos: [base[0] - signSize[0] / 2, poleTop[1] - signSize[1]];
  labelPos: [base[0], signPos[1] + signSize[1] * 0.35];

  pole:
  {
    type: "line";
    name: "bus-stop-pole";
    from: base;
    to: poleTop;
    stroke;
    width;
  };

  sign:
  {
    type: "rect";
    name: "bus-stop-sign";
    position: signPos;
    size: signSize;
    fill;
    stroke;
    width;
  };

  label:
  {
    type: "text";
    name: "bus-stop-label";
    text: "BUS";
    position: labelPos;
    align: "center";
    fontSize: signSize[1] * 0.55;
    color: textColor;
  };

  eval [pole, sign, label];
}


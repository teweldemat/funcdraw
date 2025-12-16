{
  view:
  {
    left: -20;
    bottom: -15;
    right: 20;
    top: 15;
  };

  background:
  {
    type: "rect";
    position: [view.left, view.bottom];
    size: [view.right - view.left, view.top - view.bottom];
    fill: "#020617";
    stroke: "none";
    width: 0;
  };

  title:
  {
    type: "text";
    text: "Blend modes (group.blendMode + opacity)";
    position: [0, view.top - 1.6];
    fontSize: 1.8;
    color: "#e2e8f0";
    align: "center";
  };

  overlayAlpha: 0.8;
  overlayOpacity: 0.9;

  cellWidth: 18;
  cellHeight: 8;

  makeCell: (origin, label, blendMode) =>
  {
    x0: origin[0];
    y0: origin[1];

    panel:
    {
      type: "rect";
      position: [x0, y0];
      size: [cellWidth, cellHeight];
      fill: "#0b1220";
      stroke: "#334155";
      width: 0.2;
    };

    baseA:
    {
      type: "circle";
      center: [x0 + 6, y0 + 5.1];
      radius: 3.2;
      fill: "#38bdf8";
      stroke: "none";
      width: 0;
    };

    baseB:
    {
      type: "circle";
      center: [x0 + 12, y0 + 3.2];
      radius: 3.2;
      fill: "#22c55e";
      stroke: "none";
      width: 0;
    };

    overlay:
    {
      type: "group";
      opacity: overlayOpacity;
      blendMode;
      graphics:
      [
        {
          type: "rect";
          position: [x0 + 2.4, y0 + 1.4];
          size: [cellWidth - 4.8, cellHeight - 2.8];
          fill: fd.color.alpha("#f472b6", overlayAlpha);
          stroke: "none";
          width: 0;
        },
        {
          type: "circle";
          center: [x0 + 9, y0 + 4];
          radius: 2.2;
          fill: fd.color.alpha("#fbbf24", overlayAlpha);
          stroke: "none";
          width: 0;
        }
      ];
    };

    labelText:
    {
      type: "text";
      text: label;
      position: [x0 + cellWidth / 2, y0 + 0.8];
      fontSize: 1.4;
      color: "#94a3b8";
      align: "center";
    };

    eval [panel, baseA, baseB, overlay, labelText];
  };

  cells:
  [
    makeCell([-19, 3], "source-over", "source-over"),
    makeCell([1, 3], "multiply", "multiply"),
    makeCell([-19, -7], "screen", "screen"),
    makeCell([1, -7], "overlay", "overlay"),
    makeCell([-19, -15], "darken", "darken"),
    makeCell([1, -15], "difference", "difference")
  ];

  eval
  {
    view;
    graphics: [background, title] + cells;
  };
}


{
  cartoon: import('@funcdraw/cat-cartoon');

  columnPositions: [-14, 0, 14];
  rowConfigs: [
    { groundY: 8.0; labelOffset: 7.5 },
    { groundY: -8.0; labelOffset: 5.5 }
  ];

  background: {
    type: 'rect';
    position: [-24, -22];
    size: [48, 44];
    fill: '#e4ffc7';
    stroke: '#94a3b8';
    width: 0.25;
  };

  makeLabel: (textValue, x, y) => {
    type: 'text';
    position: [x, y];
    text: textValue;
    color: '#0f172a';
    fontSize: 1.6;
    align: 'center';
  };

  showcaseItems: [
    {
      label: 'Evergreen Tree';
      rowIndex: 0;
      colIndex: 0;
      render: (x, baseY) => cartoon.landscape.tree('evergreen', [x, baseY], 8);
      defaultWidth: 8;
    },
    {
      label: 'Cottage House';
      rowIndex: 0;
      colIndex: 1;
      render: (x, baseY) => cartoon.city.house('cottage', [x, baseY], 8);
      defaultWidth: 8.5;
    },
    {
      label: 'Townhome';
      rowIndex: 0;
      colIndex: 2;
      render: (x, baseY) => cartoon.city.house('townhome', [x, baseY], 7.2);
      defaultWidth: 7.5;
    },
    {
      label: 'Modern House';
      rowIndex: 1;
      colIndex: 0;
      render: (x, baseY) => cartoon.city.house('modern', [x, baseY], 11);
      defaultWidth: 12.5;
    },
    {
      label: 'City Car';
      rowIndex: 1;
      colIndex: 1;
      render: (x, baseY) => cartoon.city.car([x, baseY], 11);
      defaultWidth: 13;
    },
    {
      label: 'Wheel Module';
      rowIndex: 1;
      colIndex: 2;
      anchorOffset: 1.2;
      render: (x, baseY) => cartoon.city.machine.parts.wheel([x, baseY], 1.4, { rimColor: '#f8fafc' });
      defaultWidth: 3.6;
    }
  ];

  showcaseGraphics:
    showcaseItems map (item) => {
      columnX: columnPositions[item.colIndex];
      rowInfo: rowConfigs[item.rowIndex];
      anchorGround: rowInfo.groundY + (item.anchorOffset ?? 0);
      moduleResult: item.render(columnX, anchorGround);
      moduleGraphics: moduleResult.graphics ?? moduleResult;
      widthCandidate: moduleResult.width ?? (item.explicitWidth ?? (item.defaultWidth ?? 10));
      moduleWidth: widthCandidate + 0;
      aboveExtent: moduleResult.above ?? (moduleResult.height ?? (item.outlineHeight ?? 8));
      belowExtent: moduleResult.below ?? 0;
      outlineHeight: aboveExtent + belowExtent;
      baseLine: {
        type: 'line';
        from: [columnX - 5.5, rowInfo.groundY - 0.2];
        to: [columnX + 5.5, rowInfo.groundY - 0.2];
        stroke: '#94a3b8';
        width: 0.18;
      };
      outlineRect: {
        type: 'rect';
        position: [columnX - moduleWidth / 2, anchorGround - aboveExtent];
        size: [moduleWidth, outlineHeight];
        fill: 'transparent';
        stroke: '#0f172a';
        width: 0.18;
        strokeDasharray: [0.5, 0.4];
      };
      label: makeLabel(item.label, columnX, rowInfo.groundY + rowInfo.labelOffset);
      eval [baseLine, outlineRect, label, moduleGraphics];
    };

  eval [background, showcaseGraphics];
}

{
  view:{
    left:0;
    bottom:0;
    right:40;
    top:30;
  };

  graphics:[panel, diagonalA, diagonalB];

  panel:{
    type:"rect";
    position:[view.left, view.bottom];
    size:[view.right - view.left, view.top - view.bottom];
    fill:"#111827";
    stroke:"#94a3b8";
    width:0.6;
  };

  diagonalA:{
    type:"line";
    from:[view.left, view.bottom];
    to:[view.right, view.top];
    stroke:"#38bdf8";
    width:1;
  };

  diagonalB:{
    type:"line";
    from:[view.left, view.top];
    to:[view.right, view.bottom];
    stroke:"#38bdf8";
    width:1;
  };
}

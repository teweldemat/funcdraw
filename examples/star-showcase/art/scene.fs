{
  view:[60,40];
  graphics:[background, title, caption] + starField;

  background:{
    type:"rect";
    position:[2,0];
    size:[56,40];
    fill:"#020617";
    stroke:"#0f172a";
    width:0.5;
  };

  title:{
    type:"text";
    text:"Star Showcase";
    position:[4,35];
    fontSize:4.5;
    color:"#e2e8f0";
  };

  caption:{
    type:"text";
    text:"FuncScript-only constellation";
    position:[4,31.5];
    fontSize:2.4;
    color:"#94a3b8";
  };

  starField:starSeeds map (seed) =>
    createStar(
      randomFloat(seed,6,54),
      randomFloat(seed + 7,6,32),
      randomFloat(seed + 13,2.6,5.5),
      selectColor(seed)
    );

  starSeeds:[1,7,13,21,34];

  palette:[
    "#facc15",
    "#4ade80",
    "#38bdf8",
    "#fb7185",
    "#f472b6"
  ];

  selectColor:(idx)=>
    palette[idx % length(palette)];

  randomFloat:(seed, min, max)=>
    min + (max - min) * randomUnit(seed);

  randomUnit:(seed)=>
    (math.sin(seed * 12.9898) + 1) / 2;
}

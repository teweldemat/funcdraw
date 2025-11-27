{
  view:{
    left:0;
    bottom:0;
    right:80;
    top:40;
  };

  groundY:4;
  ground:{
    type:"line";
    from:[view.left, groundY];
    to:[view.right, groundY];
    stroke:"#475569";
    width:0.35;
  };

  cartoonLib:package("@funcdraw/testlib");
  stickmanLib:cartoonLib.cartoon.stickman;
  houseLib:cartoonLib.cartoon.house;
  treeLib:cartoonLib.cartoon.tree;
  stickBuilder:stickmanLib ?? ((options)=>{ graphics:[]; overlays:[]; skeleton:null; });
  houseBuilder:houseLib ?? ((options)=>{ graphics:[]; });
  treeBuilder:treeLib ?? ((options)=>{ graphics:[]; });

  heroOptions:{
    position:[40, groundY + 8.5];
    palette:{
      overlayHand:"#f472b6";
      overlayLeg:"#38bdf8";
      torsoFill:"#0f172a";
      headFill:"#fde68a";
    };
    measurements:{
      legs:{
        left:{
          effectorCoordinate:[-4,-4.5];
          positiveBend:true;
        };
        right:{
          effectorCoordinate:[4,-4.5];
          positiveBend:false;
        };
      };
    };
  };

  hero:stickBuilder(heroOptions);

  houseClassic:houseBuilder({
    type:"classic";
    position:[15, groundY];
    width:14;
  });

  houseModern:houseBuilder({
    type:"modern";
    position:[35, groundY - 0.4];
    width:18;
  });

  houseCottage:houseBuilder({
    type:"cottage";
    position:[70, groundY];
    width:12;
  });

  treeLeft:treeBuilder({
    type:"round";
    position:[6, groundY];
    height:16;
  });

  treeRight:treeBuilder({
    type:"pine";
    position:[52, groundY];
    height:18;
  });

  treeFar:treeBuilder({
    type:"column";
    position:[76, groundY];
    height:14;
  });

  housesGraphics:houseClassic.graphics + houseModern.graphics + houseCottage.graphics;
  treesGraphics:treeLeft.graphics + treeRight.graphics + treeFar.graphics;
  peopleGraphics:hero.graphics;

  graphics:[ground] + housesGraphics + treesGraphics + peopleGraphics;
}

# Creating Art with FuncDraw

FuncDraw scenes live inside an `art/` directory. Each FuncScript file is treated as a packaged expression that `funcdraw-play` can evaluate locally (or dump to the console with `funcdraw-play --dump`). You can split helpers into additional files or declare them inline using `components/*` blocks just like regular FuncScript packages.

## Example: Drawing Random Stars with FuncDraw

Below is a complete FuncScript block that renders five stars at random positions and colors via FuncDraw. Each star uses a reusable helper to compute its polygon points and falls back to the default stroke when one is not specified:

```funcscript
{
  view:[60,40];

  stars:
    [0,1,2,3,4] map (idx) => {
      centerX: math.random(0, 60);
      centerY: math.random(0, 40);
      size: math.random(2, 5);
      color: palette idx;
      graphics: components/star {
        x:centerX;
        y:centerY;
        size:size;
        fill:color;
      };
    };

  graphics: stars map (item) => item.graphics;
}

components/star {
  parameters:[x,y,size,fill];
  graphics:[
    {
      type:"polygon";
      points: starpoints(x, y, size);
      fill: fill ?? "#facc15";
    }
  ];
}

func starpoints(cx, cy, radius) {
  list: [0,1,2,3,4] map (idx) =>
    angle = (idx * 72) / 180 * math.pi;
    angle2 = ((idx + 0.5) * 72) / 180 * math.pi;
    [
      [cx + math.cos(angle) * radius, cy + math.sin(angle) * radius],
      [cx + math.cos(angle2) * radius / 2, cy + math.sin(angle2) * radius / 2]
    ]
  reduce (points, pair) => points + pair;
}

func palette(index) {
  colors:[
    "#facc15",
    "#4ade80",
    "#38bdf8",
    "#fb7185",
    "#f472b6"
  ];
  colors[ index % length(colors) ];
}
```

Each evaluation produces a different constellation because `math.random` is used to pick the star coordinates and sizes. Replace the `graphics` binding with other combinations or feed the `stars` list into custom primitives to build more elaborate scenes.

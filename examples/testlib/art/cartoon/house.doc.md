# house model

## Overview

`cartoon/house.js` draws a simple house composed of a base rectangle, windows, a door (optionally swung open), and one of three roof styles. Call `package("@funcdraw/testlib").cartoon.house(options?)` to get the shape list.

## Inputs

```ts
type HouseOptions = {
  position?: [number, number] | { x?: number; y?: number; left?: number; top?: number }; // center-bottom anchor, default [0, 0]
  width?: number;               // overall house width, clamped to [8, 400], default 16
  type?: "classic" | "modern" | "cottage"; // roof/palette style, default "classic"
  doorOpenLevel?: number;       // 0..1 swing amount for the door (0 = closed, 1 = fully swung flat at 180°)
  interior?: DrawableShape[] | { graphics: DrawableShape[]; opacity?: number; } | (ctx: InteriorContext) => DrawableShape[];
};

type InteriorContext = {
  doorWidth: number;
  doorHeight: number;
  doorPosition: [number, number]; // bottom-center of the doorway
  doorCenter: [number, number];
  doorAnchor: [number, number];   // alias for doorPosition
  baseWidth: number;
  baseHeight: number;
  reveal: number; // equals doorOpenLevel
  palette: Palette;
};
```

- Palette is derived from `type`:
  - `classic`: light body, red roof, orange accent
  - `modern`: slate roof, cool grays, cyan accent
  - `cottage`: warm creams/browns, orange accent

## Outputs

```ts
type HouseResult = {
  graphics: DrawableShape[]; // rects/polygons for body, windows, door(s), roof
  anchor: [number, number];  // resolved position (center-bottom of base)
  width: number;             // resolved width
  type: string;              // resolved style key
};
```

## Notes

- The door swings about its left hinge when `doorOpenLevel > 0`, going all the way flat (180°) as the level approaches 1.
- The dark doorway fill is drawn first (and fades out as the door opens), then the `interior` graphics render on top so occupants are not obscured. By default the interior also fades in with `doorOpenLevel`; override by wrapping in `{ graphics, opacity }`.
- Walls are drawn on top of the swung door to hide any portion that crosses inside the facade; windows remain above the wall fills.
- Windows are two equal squares placed left/right of center at ~55% of the wall height.
- Roof shape depends on `type`: triangle (`classic`), flat slab with accent bar (`modern`), or a soft gable (`cottage`).

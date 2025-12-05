# Stickman Feet Helper

## Overview

`stickman/feet.fs` draws the slim toe line used for the stickman model. Hand it the IK-solved ankle point, plus a side/direction hint, and it will emit either a short centered dash (front/back facings) or a single line that protrudes left or right (profile facings) to suggest a foot.

## Inputs

```ts
type FootDirection = "left" | "right" | "center";
type FootSide = "left" | "right";
type PointInput = [number, number] | { x: number; y: number } | { left: number; top: number };

type FeetOptions = {
  anklePoint?: PointInput;         // required from stickman: hip/leg reach target
  side?: FootSide;                 // controls default direction ("left" when omitted)
  directionHint?: FootDirection;   // optional override for toe direction (defaults to side; "center" draws a dash around the ankle)
  length?: number;                 // toe-line length (default ≈ 2.2 units; front/back callers often shrink this)
  stroke?: string;                 // explicit stroke color (defaults to palette skin stroke)
  strokeWidth?: number;            // explicit stroke width (default 0.5)
  style?: { stroke?: string; width?: number }; // same overrides but bundled
};
```

Only these inputs exist because the stickman renderer never needed radius/horizontal scaffolding; everything collapses to a single line of adjustable length.

## Output

```ts
type FeetResult = {
  graphics: [
    {
      type: "line";
      from: [number, number];
      to: [number, number];
      stroke: string;
      width: number;
    }
  ];
  anklePoint: [number, number];
  toePoint: [number, number];
  center: [number, number];        // toePoint for left/right, ankle for center-aligned feet
  side: FootSide;
  direction: FootDirection;
  length: number;
};
```

Render `graphics` directly to draw the toe line. The returned metadata (`toePoint`, `direction`, etc.) can assist other models when aligning props near the feet. The stickman renderer automatically feeds the solved ankle points and the facing-aware direction hint (`"center"` for front/back, otherwise `"left"` for the screen-left leg and `"right"` for the screen-right leg).

# steperManZoom Model (front/back zoom stride)
## Overview
`stickman/steperManZoom.js` matches the `steperManProfile` interface while animating an in-place stride that reads as walking toward or away from the camera. Arms and legs stay vertical; their drop length oscillates so one side shortens while the other lengthens, mimicking depth swing. Body scale eases between 1 and `zoomFactor` so the figure appears to move into or out of the screen. Call `package("@funcdraw/testlib").cartoon.stickman.steperManZoom(options)` to get the same renderable shape as `stickman.static`, plus `sequenceState` and a `step` descriptor for chaining motions.

## Construction Overview

1. **Normalize inputs** - resolve `position`, facing, clamp `progress`/`zoomProgress`, and clamp `zoomFactor` to >= 0.
2. **Compute stride phase** - turn `progress` into a 0-2pi phase; sample a sine wave for each side (right is pi out of phase) so limbs alternate.
3. **Swing limbs** - hold the base x offsets, scale the y drop by `1 - sin(phase) * swing` (hands use a softer multiplier) so limbs shorten when "coming toward" camera and lengthen when "heading away".
4. **Derive lengths** - set upper/lower lengths from the base ratios but match the total effector reach, keeping the limbs straight down.
5. **Scale and render** - scale torso/head/shoulder extension by the blended body scale, forward everything to `stickman.static`, and attach `sequenceState` plus a `step` descriptor.

## Inputs

```ts
type Direction = "front" | "back" | "left" | "right";
type Side = "left" | "right";
type PointInput = [number, number] | { x: number; y: number } | { left: number; top: number };

type LimbSideInput = {
  upperLength?: number;
  lowerLength?: number;
  effectorCoordinate?: PointInput;
  positiveBend?: boolean;
  foot?: { length?: number | null; direction?: "left" | "right" }; // legs only
};

type TorsoInput = {
  width?: number;
  height?: number;
  shoulderExtension?: number;
  direction?: Direction;
};

type HeadInput = {
  verticalExtent?: number;
  angle?: number;
  direction?: Direction;
};

type SteperManZoomOptions = {
  position?: PointInput;           // torso center-bottom anchor (defaults to static pose anchor)
  measurements?: {
    torso?: TorsoInput;
    head?: HeadInput;
    hands?: { left?: LimbSideInput; right?: LimbSideInput };
    legs?: { left?: LimbSideInput; right?: LimbSideInput };
  };                               // same shape as stickman.static; used as the base for swing/scale
  palette?: StickmanOptions["palette"]; // forwarded to stickman.static
  direction?: Direction;           // optional facing override (otherwise falls back to measurements.torso/head then "front")
  progress?: number;               // stride loop driver (0-1 maps to 0-2pi)
  zoomProgress?: number;           // optional separate zoom driver (defaults to progress)
  zoomFactor?: number;             // body scale target blended with zoomProgress (default 1, clamped >= 0)
  swing?: number;                  // leg depth swing amount (default 0.25; 0 disables)
  handSwing?: number;              // hand swing amount (defaults to swing * 0.6)
};
```

- Defaults mirror the static stickman: leg offsets `[-2, -11]` / `[2, -11]`, hand offsets `[-3.9, 2.35]` / `[3.9, 2.35]`, leg lengths `5.2/4.8`, hand lengths `4/3`, torso `6x11` with `shoulderExtension = width * 0.15`, head height `4.5`, and head angle `90`.
- `progress` drives the sine oscillation; loop it over time for continuous in/out steps.
- `zoomProgress` lets you desync body zoom from the limb swing (e.g., zoom in while the limbs swing).
- `handSwing` defaults to 60% of `swing`.

## Outputs

```ts
type SteperManZoomResult = {
  graphics: DrawableShape[];
  overlays: OverlayPoint[];
  skeleton: SkeletonPose;
  sequenceState: {
    position: [number, number];
    measurements: StickmanOptions["measurements"];
  };
  step: {
    mode: "zoom";
    progress: number;
    zoomProgress: number;
    zoomFactor: number;
    swing: number;
    handSwing: number;
    anchorPoint: [number, number];
    direction: Direction;
  };
};
```

- The returned `graphics`/`overlays`/`skeleton` match `stickman.static`.
- `sequenceState` echoes the resolved anchor/measurements so you can feed the pose into the next animation step.
- `step` describes the current zoom stride, keeping the API parallel to `steperManProfile` for easy swapping.

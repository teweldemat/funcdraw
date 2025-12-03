# steperManZoom Model (front/back zoom stride)
## Overview
`stickman/steperManZoom.js` animates a simple zoom step: you provide the current anchor/measurements, which foot should move, the world **Y** target for that foot, a `zoom` factor, and a `progress` value. The moving foot interpolates to the target Y, the torso keeps a zoom-scaled distance above the average of both feet, limbs straighten as zoom increases, and torso facing flips to `"back"` when the moving foot rises (positive Y delta) or `"front"` when it lowers.

## Construction Overview

1. **Normalize inputs** - resolve `position`, `movingSide`, clamp `progress`, and clamp `zoom` to >= 0.
2. **Fix/move legs** - keep the fixed leg at its incoming world Y, interpolate the moving leg from its incoming world Y to `movingFootTargetY`, and blend limb spread toward vertical as zoom increases.
3. **Torso drift** - keep the torso at a height where its distance above the average of both feet scales with zoom:  
   `distance(torso, avgFeet) = initialDistance * (1 + progress * (zoom - 1))`.
4. **Scale and render** - scale torso/head/shoulder extension and limb lengths by the blended body scale, forward everything to `stickman.static`, and attach `sequenceState` plus a `step` descriptor.

## Inputs

Shared types (`Direction`, `Side`, `PointInput`, `StickmanMeasurements`) live in `schema.md` and `staticMan.doc.md`.

```ts
type SteperManZoomOptions = {
  position?: PointInput;               // torso center-bottom anchor (defaults to static pose anchor)
  measurements?: StickmanMeasurements; // same shape as stickman.static; used as the base for scale/offsets
  movingSide?: Side;                   // which foot moves ("left" default)
  movingFootTargetY?: number;          // world-space Y target for the moving foot; start is derived from measurements
  zoom?: number;                       // body scale target blended with progress (default 1, >= 0)
  zoomProgress?: number;               // optional blend driver for zoom (default 1 to apply zoom immediately)
  progress?: number;                   // stride + zoom driver (0–1 single stride)
};
```

- Defaults mirror the static stickman (leg/hand lengths and offsets, torso/head sizes); see `schema.md` and `skeleton.doc.md` for the baseline values.
- `progress` drives the foot motion and zoom blend; loop it if you want continuous motion.
- Torso facing flips automatically based on the moving foot delta Y (up => `"back"`, down => `"front"`).

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
    zoom: number;
    zoomFactor: number;
    anchorPoint: [number, number];
    direction: Direction;
    fixedSide: Side;
    movingSide: Side;
    fixedPoint: [number, number];
    movingPoint: [number, number];
  };
};
```

- The returned `graphics`/`overlays`/`skeleton` match `stickman.static`.
- `sequenceState` echoes the resolved anchor/measurements so you can feed the pose into the next animation step.
- `step` describes the current zoom stride, keeping the API parallel to `steperManProfile` for easy swapping.

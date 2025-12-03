# sideWalkMan Example (profile walk measurer)

## Overview
`stickman/sideWalkMan.js` uses `steperManProfile` to generate the stickman measurements for a sideways (profile) walk across multiple steps. Given an initial torso anchor and measurements, a horizontal displacement (positive or negative), and a progress value between 0 and 1, it walks the character across as many steps as needed and returns the measurements/anchor at that progress so callers can render or chain poses without world-point math.

## Construction Overview

1. **Normalize inputs** – read the initial torso anchor/measurements and clamp `progress` to `[0, 1]`.
2. **Plan multiple steps** – derive a stride length from the average leg length (~35% by default, overridable via `strideLength`) and split the total `displacement` into enough steps to cover it; starting fixed foot is the left foot when `displacement >= 0`, otherwise the right.
3. **Simulate step-by-step** – for each stride up to the current progress, call `steperManProfile` with the current moving foot target (plus the current anchor/leg offsets) and accumulate the resulting anchor so the torso and feet translate smoothly.
4. **Extract measurements** – return the resolved `measurements` and anchor from the active step so downstream code can draw or continue the walk.

## Inputs

Shared types (`Direction`, `Side`, `PointInput`, `StickmanMeasurements`) live in `schema.md` and `staticMan.doc.md`.

```ts
type SideWalkOptions = {
  initialPosition?: PointInput;        // torso anchor for the start of the step (defaults to [0, 10])
  initialMeasurements?: StickmanMeasurements; // baseline measurements; defaults mirror static stickman
  displacement?: number;               // total horizontal delta for the walk (positive => right, negative => left)
  progress?: number;                   // walk progress in [0, 1], spanning multiple steps
  direction?: "left" | "right";        // torso/head facing (default "right")
  strideLength?: number;               // optional override for per-step stride length (defaults to ~35% of average leg length)
  handSwing?: SteperManProfileOptions["handSwing"]; // optional arm swing settings forwarded to steperManProfile
};
```

## Outputs

```ts
type SideWalkResult = {
  measurements: StickmanMeasurements;  // resolved measurements at the given progress
  position: [number, number];          // recentered torso anchor for this frame
};
```

Use the returned `measurements` with `stickman.static` and set its `position` to the returned `position` to show the sideways translation. You can also feed both into subsequent step calculations to keep the walk continuous.***

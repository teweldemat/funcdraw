# zoomWalkMan Helper (front/back depth walk)

## Overview
`stickman/zoomWalkMan.js` chains the single-step `steperManZoom` pose across multiple strides along the Y/depth axis. Give it the starting torso anchor and measurements, a total `depthDelta` to cover (negative to zoom in/deeper, positive to zoom out/shallower), a `zoom` factor, and a normalized `progress` value. It plans however many strides are needed, alternates feet (starting with the deeper foot when zooming in), blends zoom across the walk, and returns the anchor/measurements for the active step so you can render with `stickman.static` or keep iterating.

## Construction Overview

1. **Normalize inputs** – read the initial anchor/measurements, clamp `progress` to `[0, 1]`, clamp `zoom` to `>= 0`, and pick a torso `direction` fallback (`"front"`/`"back"`/`"left"`/`"right"`).
2. **Plan strides** – derive a stride length from the average leg length (~35% by default, or the leg offset gap if lengths are missing), add ~25% overreach (min 0.75), and set `stepCount = ceil(|depthDelta| / passDistance)`. Track which stride/phase is active based on `progress`.
3. **Step simulation** – alternate fixed/moving sides (start with the deeper foot when zooming in, shallower when zooming out). For each step, compute the moving foot’s world-Y target, blend zoom as `currentZoom = 1 + (zoom - 1) * zoomPhase`, and call the `stepper` (defaults to `steperManZoom`) with the current anchor/measurements plus that target.
4. **Accumulate pose** – pull `position`/`measurements` from the stepper (or its `sequenceState`), recalc both foot world positions, and continue until the active stride is resolved. Return the final anchor/measurements for the current progress slice.

## Inputs

Shared types (`Direction`, `Side`, `PointInput`, `StickmanMeasurements`) are defined in `schema.md` and `staticMan.doc.md`.

```ts
type ZoomWalkOptions = {
  initialPosition?: PointInput;               // starting torso anchor (default [0, 10] from static pose)
  initialMeasurements?: StickmanMeasurements; // baseline measurements/offsets for stride planning
  depthDelta?: number;                        // total world-Y delta for the moving foot across the walk (negative = deeper/zoom in)
  zoom?: number;                              // zoom factor to blend across the whole walk (default 1, clamped >= 0)
  progress?: number;                          // normalized walk progress [0, 1] spanning all strides
  direction?: Direction;                      // torso/head facing hint (default "front")
  stepper?: typeof steperManZoom;             // optional override for the stride builder; defaults to global steperManZoom
};
```

- If leg lengths are provided, stride length comes from their average; otherwise it falls back to the effector offset gap (~22 units by default).
- `progress` is distributed across all computed strides; loop it to keep walking.
- Passing a custom `stepper` is useful for instrumentation or variants; it must mirror the `steperManZoom` interface.

## Outputs

```ts
type ZoomWalkResult = {
  measurements: StickmanMeasurements; // resolved measurements for the active stride
  position: [number, number];         // torso anchor for the active stride
};
```

Feed `position` and `measurements` into `stickman.static` (or another pose consumer) to draw the walker at the current depth/zoom. The result intentionally omits `graphics` to stay lightweight; if you need rendered output, call your `stepper` directly or compose with `stickman.static`.

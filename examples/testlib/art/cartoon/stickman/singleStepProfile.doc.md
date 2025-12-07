# singleStepProfile Model (profile walk)
## Overview
`stickman/singleStepProfile.js` focuses on producing a single profile step pose that you can chain yourself. Give it a torso anchor, the current stickman measurements, tell it which side should move, and pass the world target for that moving foot. The helper animates the moving foot along a lifted arc toward the target, recenters the torso anchor between both ankles, and patches the leg offsets (plus arm swing, if enabled) so you can feed the returned `position`/`measurements` into the next call or straight into `stickman/staticMan`.

## Construction Overview

1. **Resolve inputs** – normalize the current anchor (`position`), measurements, `movingSide`, `movingFeetTargetPoint`, and `progress` (clamped 0–1). Defaults pull from the base static pose when a measurement offset is missing.
2. **Arc interpolation** – animate the moving foot from its current world position (anchor + effectorCoordinate) toward `movingFeetTargetPoint` along a sine-lifted arc (height scales with stride length, minimum lift 1.5).
3. **Recenter torso anchor** – compute the anchor implied by each ankle (`footWorld - effectorCoordinate`), average them, and clamp vertical drift to ±1.2 around the incoming anchor so the torso bobs instead of pogoing.
4. **Update measurements** – translate both world ankle targets into new `legs.*.effectorCoordinate` offsets relative to the recentered anchor; mirror the torso/head direction and apply optional hand swing based on the leg offsets.
5. **Render (optional)** – the updated pose is forwarded to `stickman/staticMan`, so you still get `graphics`, `overlays`, and `skeleton` when you want to draw the step directly.

## Inputs

`singleStepProfile(options?)` accepts the same base options as `stickman.static` plus a small set of step fields. Shared types (`Side`, `PointInput`, `StickmanMeasurements`) are defined in `schema.md` and `staticMan.doc.md`.

```ts
type SteperManProfileOptions = {
  position?: PointInput;                     // current torso anchor
  measurements?: StickmanOptions["measurements"]; // current pose (legs offsets, torso/head direction, etc.)

  movingSide?: Side;                         // which foot is stepping ("left" default)
  movingFeetTargetPoint?: PointInput;        // world-space destination for the moving ankle
  progress?: number;                         // step phase between 0 and 1
  arcHeight?: number;                        // optional lift override

  handSwing?: {                              // optional arm swing controls (same as before)
    enabled?: boolean;                       // false disables automation (default true)
    amplitude?: number;                      // horizontal swing multiplier (default 1.4)
    lift?: number;                           // vertical swing multiplier (default 0.35)
    forwardOffset?: number;                  // horizontal bias applied to both arms (default 0)
    phase?: number;                          // extra radians when mode === "sine" (default 0)
    mode?: "mirror" | "sine";                // "mirror" follows the opposite leg (default)
  };

  // Legacy compatibility helpers (kept so older callers keep working)
  fixedFeet?: Side;
  fixedFeetPoint?: PointInput;
  movingFeetStartPoint?: PointInput;
  movingFeetTarget?: PointInput;
};
```

- All point inputs accept `[x, y]`, `{ x, y }`, or `{ left, top }`.
- If `movingFeetTargetPoint` is omitted the moving foot stays at its current world position; animating `progress` alone lifts/drops the foot in place.
- The helper infers the fixed foot as the opposite of `movingSide`; the legacy `fixedFeet` and `movingFeetStartPoint` fields are still read for callers that have not switched to the new API yet.

## Outputs

```ts
type SteperManProfileResult = {
  position: [number, number];                     // recentered torso anchor
  measurements: StickmanOptions["measurements"];  // updated pose with new leg offsets (and optional hand swing)
  finalPosition: [number, number];                // alias of position
  finalMeasurements: StickmanOptions["measurements"]; // alias of measurements
  sequenceState: { position: [number, number]; measurements: StickmanOptions["measurements"]; };
  step: { fixedSide: Side; movingSide: Side; fixedPoint: [number, number]; movingPoint: [number, number]; anchorPoint: [number, number]; progress: number; };
  graphics?: DrawableShape[];                     // when staticMan is available
  overlays?: OverlayPoint[];
  skeleton?: SkeletonPose;
};
```

Use the top-level `position`/`measurements` (or `sequenceState`) to feed the next step or hand the pose to `stickman.static` yourself. `graphics`, `overlays`, and `skeleton` stay available for drop-in rendering, and `step` exposes the resolved ankle/anchor world points for debug overlays or footprint tracking.

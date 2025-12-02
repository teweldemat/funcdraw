# steperManProfile Model (profile walk)
## Overview
`stickman/steperManProfile.js` reuses the static cartoon stickman while orchestrating a single profile (side-view) walking step. Call `package("@funcdraw/testlib").cartoon.stickman.steperManProfile(options)` to pin one foot in place (`fixedFeet`), move the opposite foot from `movingFeetStartPoint` toward `movingFeetTargetPoint`, and let the helper animate that leg along a simple arc (lift controlled by the segment length). The helper also slides the torso anchor (stickman `position`) between both ankle constraints, so the character’s body shifts naturally as the step progresses. The rest of the pose is forwarded to `stickman/staticMan`, so every measurement override behaves exactly like the base model.

## Construction Overview

1. **Determine defaults** – capture the static stickman’s skeletal rest pose to figure out reasonable baseline ankle offsets if the caller omits explicit points.
2. **Resolve inputs** – normalize `position` (fallback anchor), `fixedFeet*`, `movingFeet*`, and `progress` (clamped between 0 and 1) while converting everything to world-space coordinates.
3. **Arc interpolation** – compute the moving foot’s current world coordinate by lerping between `movingFeetStartPoint` and `movingFeetTargetPoint`, then add a sine-based vertical lift so the toes follow an arc.
4. **Recenter torso anchor** – subtract each ankle’s default offset from its world position, average the candidates so the stickman anchor glides between both legs, and clamp the vertical drift to ±1.2 units so the torso bobs gently instead of pogoing.
5. **Update measurements** – translate both world-space ankle targets into offsets relative to the recentered anchor and patch them into `measurements.legs.left/right.effectorCoordinate`.
6. **Delegate to static stickman** – pass the augmented options to `staticMan` and attach a small `step` metadata object that reports which foot was fixed, which is moving, and the resolved world coordinates/anchor.

## Inputs

`steperManProfile(options?)` accepts everything `stickman.static` understands plus a handful of step-specific fields:

```ts
type Side = "left" | "right";

type SteperManProfileOptions = {
  position?: PointInput;            // Fallback torso anchor if neither ankle supplies a usable position
  measurements?: StickmanOptions["measurements"];
  palette?: StickmanOptions["palette"];

  fixedFeet?: Side;                 // Which foot should stay planted ("left" by default)
  fixedFeetPoint?: PointInput;      // World-space target for the fixed ankle (defaults to static pose)
  movingFeetStartPoint?: PointInput;// World-space point where the moving foot begins (defaults to static pose)
  movingFeetTargetPoint?: PointInput;// World-space destination the moving foot should reach (falls back to start)
  progress?: number;                // Step progress between 0 and 1 (clamped); drives interpolation along the arc

  handSwing?: {                     // Optional arm swing controls
    enabled?: boolean;              // false disables the automation entirely (default true)
    amplitude?: number;             // horizontal offset multiplier (default 1.4)
    lift?: number;                  // vertical swing multiplier (default 0.35)
    forwardOffset?: number;         // constant horizontal offset applied to both arms (default 0)
    phase?: number;                 // extra radians used only when mode === "sine" (default 0)
    mode?: "mirror" | "sine";       // "mirror" copies the opposite leg angles (default), "sine" preserves the legacy wave
  };
};
```

- All point inputs accept `[x, y]`, `{ x, y }`, or `{ left, top }` just like the static model.
- If `movingFeetTargetPoint` is omitted the leg stays near `movingFeetStartPoint`, letting you hold the foot in mid-air simply by animating `progress`.
- Arc height defaults to 25% of the planar distance between the start and target (with a minimum lift of 1.5 units) so short steps still pick up slightly.

### Hand swing controls

When `handSwing.enabled !== false`, `steperManProfile` offsets the stickman’s arm effectors every frame so they swing opposite the stepping leg (right leg forward pushes the left arm forward, etc.). In the default `mode: "mirror"` the helper inspects both leg offsets, computes how far each ankle leads/lag relative to the torso, and applies a mirrored version of that angle/height to the opposite arm. `amplitude` and `lift` act as multipliers on the mirrored horizontal/vertical deltas, while `forwardOffset` nudges both hands equally. Swap to `mode: "sine"` if you want the legacy cosine/sine sweep that ignores leg placement but still responds to `amplitude`, `lift`, `forwardOffset`, and `phase`.

## Outputs

```ts
type SteperManProfileResult = {
  graphics: DrawableShape[];
  overlays: OverlayPoint[];
  skeleton: SkeletonPose;
  sequenceState: {
    position: [number, number];
    measurements: StickmanOptions["measurements"];
  };
  step: {
    fixedSide: Side;
    movingSide: Side;
    fixedPoint: [number, number];
    movingPoint: [number, number];
    anchorPoint: [number, number];
    progress: number;
  };
};
```

`graphics`, `overlays`, and `skeleton` come directly from `stickman.static`, so render or inspect them the same way you would the base model. `sequenceState` exposes the resolved `position` plus the leg-updated `measurements`, making it easy to pass the pose into another step or animation stage. The additional `step` metadata tells you which limb is grounded, the resolved ankle coordinates (world space), the animated torso anchor, and the clamped progress value; downstream callers can use that to synchronize props (e.g., footprints) or blend multiple steperManProfile calls into a full gait cycle.

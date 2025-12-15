"""
## Overview

`zoomWalk` produces a continuous walk animation along the Y axis with a compounding zoom illusion.
It is the vertical counterpart to `profileWalk` and composes multiple `singleStepZoom` steps to
cover the full travel distance.

This is intended for "walk toward the camera" / "walk away from the camera" shots where vertical
travel implies scale change.

## Construction Overview

1. Merge `measurements` over `defaultMeasurements` (deep-merge limb records).
2. Normalize step parameters: `strideAbs = Abs(strideLength)`, `sign = Sign(verticalDistance)`.
3. Seed the gait with a symmetric leg phase so progress starts from a stable pose.
4. Apply all fully completed steps via `Range(...) reduce stepOnce`.
5. Apply the current partial step with `singleStepZoom(..., localProgress, zoomFactor)`.
6. Return the current profile (including the updated `anchor`).

## Inputs

```
zoomWalk(
  position:[x,y],                // world-space starting anchor
  measurements:{ ... },          // merged over `defaultMeasurements`
  verticalDistance:number,       // signed total Y travel (negative moves down)
  strideLength:number,           // >0 step size (sign comes from verticalDistance)
  progress:number,               // 0..1 over the whole walk
  zoomFactor:number              // >0; passed through to singleStepZoom
)
```

Notes:
- `progress` maps linearly to anchor motion: `anchor[1] = position[1] + verticalDistance * progress`.
- Scale compounds step-to-step because each `singleStepZoom` call scales the incoming profile.
- `direction` inside `measurements` controls hip spread ("front"/"back" spread; "left"/"right" no spread).
- Limb straightness is enforced by `singleStepZoom` (vertical `end[0]=0` and segment rescale to reach the end).

## Outputs

Returns the same profile shape as `singleStepZoom` (a measurements KVC with an `anchor:[x,y]`).
The returned profile is suitable for `cartoon.character.static(anchor, profile, palette)`.
"""

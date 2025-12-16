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
3. Seed the gait by offsetting the feet by `strideAbs` and normalize the pose via `singleStepZoom(..., progress=0)`.
4. March forward by applying repeated `singleStepZoom` steps until the desired traveled distance is reached.
5. The effective stride (foot separation) scales with zoom so step size stays proportional as the character grows/shrinks.
6. At `progress == 1`, reset limbs to equal size (neutral stance).
7. Return the current profile (including the updated `anchor`).

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
- `progress` maps linearly to the feet midpoint (average foot world Y), not the anchor.
- Scale compounds step-to-step because each `singleStepZoom` call scales the incoming profile and
  applies the zoom around the planted foot.
- `direction` inside `measurements` controls hip spread ("front"/"back" spread; "left"/"right" no spread).
- Limb straightness is enforced by `singleStepZoom` (vertical `end[0]=0` and segment rescale to reach the end).
- `strideLength` is the base stride size at legScale=1; the gait seed scales it by the current leg size and zoom scaling adapts it over time to avoid giant steps when zoomed out.

## Outputs

Returns the same profile shape as `singleStepZoom` (a measurements KVC with an `anchor:[x,y]`).
The returned profile is suitable for `cartoon.character.static(anchor, profile, palette)`.
"""

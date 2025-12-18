"""
## Overview

`zoomWalk` produces a continuous walk animation along the Y axis with a compounding zoom illusion.
It is the vertical counterpart to `profileWalk`.

This is intended for "walk toward the camera" / "walk away from the camera" shots where vertical
travel implies scale change.

## Construction Overview

1. Merge `measurements` over `defaultMeasurements` (deep-merge limb records).
2. Normalize step parameters: `strideAbs = Abs(strideLength)`, `sign = Sign(verticalDistance)`.
3. Move the feet midpoint linearly with `progress` (this is the primary contract).
4. Apply an exponential scale factor based on traveled midpoint distance: `scale = exp(-sign * zoomFactor * traveled)`.
5. Use a simple stepping model: within each step, one foot stays planted while the other moves to preserve the linear midpoint.
6. Solve straight, vertical limbs by rescaling limb lengths to match the resulting leg/hand `end`.
7. At `progress == 1`, reset limbs to equal size (neutral stance).
8. Return the current profile (including the updated `anchor`).

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
- Scale compounds because it is exponential in traveled distance.
- `direction` inside `measurements` controls hip spread ("front"/"back" spread; "left"/"right" no spread).
- Limb straightness is enforced by keeping vertical `end[0]=0` and rescaling segment lengths to reach `end`.
- `strideLength` is the base stride size at legScale=1; it scales with leg size and also controls the stepping frequency.

## Outputs

Returns a measurements KVC with an `anchor:[x,y]`.
The returned profile is suitable for `cartoon.character.static(anchor, profile, palette)`.
"""

"""
## Overview

`singleStepZoom` computes a single vertical gait step with a "zoom" illusion. It returns a profile
(anchor + scaled body measurements + limb definitions) suitable for `cartoon.character.static` and
for chaining in `zoomWalk`.

The step moves one foot toward a target world-space Y, shifts the body anchor by half that delta,
and scales the body so the character appears to grow/shrink as it moves along the Y axis. Hands
mirror the current leg extension difference for a consistent silhouette.

This step intentionally keeps legs and hands straight (no bend): the limb `end` vectors are forced
vertical (`end[0]=0`) and the limb segment lengths are rescaled so `upper+lower == Abs(end[1])`.
The "walk toward/away from camera" illusion comes from shortening/lengthening these straight limbs
while the body scales.

## Construction Overview

1. Merge `measurements` over `defaultMeasurements` and deep-merge the limb records.
2. Compute hip attachment points from `anchor`, `bodyAngle`, `thighWidth`, and `direction` spread.
3. Drive the selected foot toward `targetFeetY` (world-space) and shift the anchor by `0.5 * dy`.
4. Compute `scale = 1 - bodyShiftY * zoomFactor` and scale body dimensions + limb segment lengths.
5. Keep the non-moving foot planted (no sliding) while the moving foot advances, then compute
   vertical leg `end` vectors and rescale the leg segments to reach them (straight legs).
6. Mirror hand extension against leg extension, then rescale the hand segments to reach the mirrored
   vertical ends (straight hands).
7. Return the updated profile with `anchor`.

## Inputs

```
singleStepZoom(
  anchor:[x,y],                  // world-space body anchor (hips)
  measurements:{ ... },          // merged over `defaultMeasurements` (see below)
  movingFeet:"left"|"right",     // which foot moves this step
  targetFeetY:number,            // world-space Y for the moving foot at progress=1
  progress:number,               // 0..1 step progress
  zoomFactor:number              // >0; scale sensitivity
)
```

`measurements` (all fields optional; defaults shown from `defaultMeasurements.fs`):

```
{
  direction:"front"|"back"|"left"|"right"; // default: "front" (controls hip spread)
  height:number;                            // default: 16
  neckLength:number;                        // default: 1.5
  headRadius:number;                        // default: 2.5
  bodyAngle:number;                         // radians; default: Pi/2
  neckAngle:number;                         // radians; default: Pi/2
  shoulderWidth:number;                     // default: 2
  thighWidth:number;                        // default: 1.2
  handPhaseOffset:number;                   // default: 0

  leftHand:{ end:[x,y]; upper:number; lower:number; sign:-1|1; };
  rightHand:{ end:[x,y]; upper:number; lower:number; sign:-1|1; };
  leftLeg:{ end:[x,y]; upper:number; lower:number; sign:-1|1; };
  rightLeg:{ end:[x,y]; upper:number; lower:number; sign:-1|1; };
}
```

Limb `end` vectors are local offsets from their attachment points. This step keeps legs and hands
vertical (`end[0] = 0`) and uses `end[1]` for extension; segment lengths are rescaled so limbs stay
fully extended.

## Outputs

Returns a profile KVC (shape compatible with `defaultMeasurements`) with an added/updated `anchor`:

```
{
  anchor:[x,y];                  // updated world-space anchor
  direction:"front"|"back"|"left"|"right";
  height:number;
  neckLength:number;
  headRadius:number;
  bodyAngle:number;              // radians
  neckAngle:number;              // radians
  shoulderWidth:number;
  thighWidth:number;
  handPhaseOffset:number;

  leftLeg:{ end:[0,y]; upper:number; lower:number; sign:-1|1; };
  rightLeg:{ end:[0,y]; upper:number; lower:number; sign:-1|1; };
  leftHand:{ end:[0,y]; upper:number; lower:number; sign:-1|1; };
  rightHand:{ end:[0,y]; upper:number; lower:number; sign:-1|1; };
}
```

Key invariants:
- Legs and hands are straight: `upper+lower == Abs(end[1])` and `end[0] == 0`.
- Hand extension mirrors leg extension (`leftHand.end[1] - rightHand.end[1] == rightLeg.end[1] - leftLeg.end[1]`).
"""

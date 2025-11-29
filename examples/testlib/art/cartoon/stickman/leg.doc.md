# Stickman Leg Model

## Overview

`leg.js` draws a limb from the hip attachment to the ankle effector using two straight segments. Visually, each leg is a pair of colored strokes with a slight “joint” break where the knee bends; the foot itself is added by `feet.js`. Call `leg(options?)` with the solved joints from `skeleton.build(...)` so it can render the hip→knee→ankle path without recomputing inverse kinematics.

## Construction Overview

1. **Read joints** – Consume `joints.attachment`, `joints.hinge`, and `joints.effector` directly from the skeleton’s leg entries.
2. **Draw thigh** – Line from hip to knee using the requested stroke color and width.
3. **Draw shin** – Second line from knee to ankle. The caller can reuse `resolvedTargetPoint` to place feet or overlays.

## Inputs

```ts
type Point = [number, number] | { x: number; y: number } | { left: number; top: number };

type LegJoints = {
  attachment: Point; // hip position
  hinge: Point;      // knee position
  effector: Point;   // ankle after IK clamping
};

// left/right refer to screen-left/screen-right legs even when the torso faces back.

type LegOptions = {
  joints: LegJoints;           // required: provide skeleton.legs.*.joints
  style?: { stroke?: string; width?: number }; // default stroke "#0ea5e9", width 1.1
};

function leg(options?: LegOptions): { graphics: Graphic[] }; // returns the thigh + shin segments ready to draw
```

Always pass `joints` from the skeleton; the model no longer recomputes IK.

## Outputs

```ts
type LegResult = {
  graphics: Graphic[]; // thigh + shin segments
};
```

Render `graphics` directly; rely on the skeleton’s joints when you need hip/knee/ankle positions for feet or overlays.

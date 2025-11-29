# Stickman Hand Model

## Overview

`hand.js` draws an arm from the shoulder attachment to the wrist effector using two straight segments. The shoulder→elbow segment represents the upper arm, while the elbow→wrist segment represents the forearm. Call `hand(options?)` with the solved joints from `skeleton.build(...)` so it can render the limb without recomputing inverse kinematics.

## Construction Overview

1. **Read joints** – Use `joints.attachment`, `joints.hinge`, and `joints.effector` from the skeleton (left/right hand entries).
2. **Draw upper arm** – Line from shoulder to elbow.
3. **Draw forearm** – Line from elbow to wrist.

## Inputs

```ts
type Point = [number, number] | { x: number; y: number } | { left: number; top: number };

type HandJoints = {
  attachment: Point; // shoulder position
  hinge: Point;      // elbow position
  effector: Point;   // wrist after IK clamping
};

// left/right refer to screen-left/screen-right hands even when the torso faces back.

type HandOptions = {
  joints: HandJoints;           // required: provide skeleton.hands.*.joints
  style?: { stroke?: string; width?: number }; // default stroke "#f97316", width 0.8
};

function hand(options?: HandOptions): { graphics: Graphic[] }; // returns the upper-arm + forearm segments
```

Always pass `joints` from the skeleton; the model no longer solves IK on its own.

## Outputs

```ts
type HandResult = {
  graphics: Graphic[]; // two line segments (upper arm + forearm)
};
```

Render `graphics` directly; all positional data (attachment, elbow, wrist) already lives inside the skeleton.

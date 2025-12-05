# Stickman Skeleton Model

## Overview

`stickman/skeleton.fs` performs the pose math for the cartoon character. Call `skeleton.build(options?)` with high-level measurements and target points to receive a normalized skeleton describing where the torso sits, how the head is attached, and how each limb bends to reach its effector. Rendering modules (`eval.js`, `hand.fs`, `leg.js`, `head.fs`) consume this data in `stickman/eval.js` to draw the final graphics.

## Construction Overview

1. **Normalize inputs** – Sanitize `position` and `measurements`, merging overrides with `defaultMeasurements`.
2. **Derive torso frame** – Compute torso width/height/shoulder extension and place head/limb attachment points relative to the torso center-bottom anchor.
3. **Solve head pose** – Carry the requested tilt/direction into the head frame so downstream renderers can orient the skull correctly.
4. **Solve limb IK** – For each arm and leg, clamp lengths, project the effector coordinates relative to the anchor, and determine elbow/knee bend (based on `positiveBend`). Store attachment points, target points, and foot metadata.
5. **Emit skeleton** – Bundle the resolved pose along with the merged measurements and normalized options.

## Inputs

```ts
type Direction = "front" | "back" | "left" | "right";
type FootDirection = "left" | "right" | "center";
type PointInput = [number, number] | { x: number; y: number } | { left: number; top: number };

type StickmanOptions = {
  position?: PointInput; // torso center-bottom anchor; defaults to [0, legLengthSum + footThickness] so toes sit on y = 0
  measurements?: {
    torso?: {
      width?: number;             // torso width (default 6)
      height?: number;            // torso height (default 11)
      shoulderExtension?: number; // distance between torso edge and shoulder joint (default width * 0.15)
      direction?: Direction;      // facing for torso/head defaults (default "front")
    };
    head?: {
      verticalExtent?: number;    // head height (default 4.5)
      angle?: number;             // head tilt in degrees (default 90 upright)
      direction?: Direction;      // head facing (defaults to torso direction)
    };
    hands?: {
      left?: HandSideConfig;
      right?: HandSideConfig;
    };
    legs?: {
      left?: LegSideConfig;
      right?: LegSideConfig;
    };
  };
};

// All "left"/"right" measurements refer to screen-left or screen-right limbs.
// Even when torso.direction = "back" the left entry still controls the screen-left arm/leg.

type HandSideConfig = {
  upperLength?: number;           // shoulder→elbow length (default 4)
  lowerLength?: number;           // elbow→hand length (default 3)
  effectorCoordinate?: PointInput;// IK target relative to StickmanOptions.position (defaults to ±(torsoWidth/2 + shoulderExtension), drop ≈ 2.35)
  positiveBend?: boolean;         // elbow rotation relative to the shoulder→effector vector (screen-left false, screen-right true)
};

type LegSideConfig = {
  upperLength?: number;           // hip→knee length (default 5.2)
  lowerLength?: number;           // knee→ankle length (default 4.8)
  effectorCoordinate?: PointInput;// IK target relative to StickmanOptions.position (defaults keep toes under the torso, e.g. [±1.5, -10])
  positiveBend?: boolean;         // knee rotation relative to the hip→effector vector (left false, right true)
  foot?: {
    length?: number | null;       // toe-line length (front/back facings shrink to a narrow line; otherwise defers to feet.fs base when null)
    direction?: FootDirection;    // toe direction (profile facings follow torso.direction; front/back default to "center" for a symmetric dash; otherwise defaults to bend direction: positive => "right")
  };
};
```

Supply only the branches that need customization; all other values inherit from `defaultMeasurements`.

## Output

```ts
type SkeletonTorso = {
  centerBottomPoint: [number, number];
  width: number;
  height: number;
  shoulderExtension: number;
  direction: Direction;
  headAttachmentPoint: [number, number];
  handAttachmentPoints: { left: [number, number]; right: [number, number] };
  legAttachmentPoints: { left: [number, number]; right: [number, number] };
};

type SkeletonHand = {
  attachmentPoint: [number, number];
  targetPoint: [number, number];   // requested wrist effector
  reachTarget: [number, number];   // clamped wrist after IK
  bendPoint: [number, number];     // elbow
  reachDirection: [number, number];
  bendDirection: 1 | -1;
  lengths: { upper: number; lower: number };
  positiveBend: boolean;
  joints: { attachment: [number, number]; hinge: [number, number]; effector: [number, number] };
};

type SkeletonLeg = SkeletonHand & {
  foot: { length: number | null; direction: FootDirection };
};

type SkeletonBuildResult = {
  skeleton: {
    position: [number, number];
    torso: SkeletonTorso;
    head: {
      attachmentPoint: [number, number];
      verticalExtent: number;
      angle: number;
      direction: Direction;
    };
    hands: Record<"left" | "right", SkeletonHand>;
    legs: Record<"left" | "right", SkeletonLeg>;
  };
  normalizedOptions: StickmanOptions;           // sanitized caller input (palette, etc.)
};
```

`skeleton.build(options?)` returns `SkeletonBuildResult`; `stickman/eval.js` forwards its `options` there and hands the data to the head/torso/hand/leg models while also reusing `normalizedOptions.palette` for colors. The module also re-exports `defaultMeasurements`, `normalizeInput`, and `mergeDeep` for callers that want to inspect the presets or run their own override logic. Downstream models primarily read `skeleton.torso`, `skeleton.head`, `skeleton.hands`, and `skeleton.legs` to drive their geometry.

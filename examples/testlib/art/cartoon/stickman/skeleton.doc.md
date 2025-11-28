# Stickman Skeleton Builder

`art/cartoon/stickman/skeleton.js` generates the neutral pose used by the cartoon stickman. The rest of the renderer (torso, head, limbs) consumes its output to know where limbs anchor, how long each segment is, and which direction the character faces. You can call it directly from FuncScript/JavaScript to drive your own renders or manipulate the IK targets before passing them back into the stock parts.

---

## Usage

```fs
skeleton:create(package("@funcdraw/testlib").cartoon.stickman.skeleton);

stance:skeleton.build({
  position:[5, 10.5];
  measurements:{
    torso:{ direction:"left"; height:12.5; };
    hands:{
      left:{ effectorCoordinate:[-6, 4]; positiveBend:true; };
      right:{ effectorCoordinate:[6, 4]; positiveBend:false; };
    };
    legs:{
      left:{ effectorCoordinate:[-2, -11]; };
      right:{ effectorCoordinate:[2, -11]; foot:{ length:2.6; direction:"right"; }; };
    };
  };
});

stance.skeleton.head.direction;
```

`build` accepts an options object, merges it with the defaults, and returns both the normalized configuration and the computed joint positions so downstream modules can render graphics without repeating IK math.

---

## Pseudo Schema

```ts
type Direction = "front" | "back" | "left" | "right";
type FootDirection = "left" | "right";
type PointInput =
  | [number, number]
  | { x: number; y: number }
  | { left: number; top: number }; // handy for FuncScript maps

type HandSideConfig = {
  upperLength?: number; // default 4
  lowerLength?: number; // default 3
  effectorCoordinate?: PointInput; // target relative to stickman.position; defaults to ±(torsoWidth/2 + shoulderExtension), drop ≈ 2.35
  positiveBend?: boolean; // left defaults false, right defaults true so elbows face outward
};

type LegSideConfig = {
  upperLength?: number; // default 5.2
  lowerLength?: number; // default 4.8
  effectorCoordinate?: PointInput; // reach relative to stickman.position; defaults keep feet under the torso
  positiveBend?: boolean; // left false, right true; swap to mirror a pose
  foot?: {
    length?: number | null; // overrides toe line length; null keeps default
    direction?: FootDirection; // defaults to bend direction (positive -> "right")
  };
  feetLength?: number | null; // legacy alias when `foot` is omitted
  feetDirection?: FootDirection;
};

type StickmanMeasurements = {
  torso?: {
    width?: number; // default 6
    height?: number; // default 11
    shoulderExtension?: number; // default width * 0.15; how far arms sit from torso edge
    direction?: Direction; // "front" by default
  };
  head?: {
    verticalExtent?: number; // default 4.5
    angle?: number; // default 90 degrees (upright)
    direction?: Direction; // inherits torso direction when omitted
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

type StickmanOptions = {
  position?: PointInput; // default [0, legLengthSum + footThickness] so toes rest on y = 0
  measurements?: StickmanMeasurements; // partial overrides merged with defaults
};

function build(options?: StickmanOptions): {
  skeleton: {
    position: [number, number]; // normalized anchor
    torso: {
      centerBottomPoint: [number, number];
      width: number;
      height: number;
      shoulderExtension: number;
      direction: Direction;
      headAttachmentPoint: [number, number];
      handAttachmentPoints: { left: [number, number]; right: [number, number]; };
      legAttachmentPoints: { left: [number, number]; right: [number, number]; };
    };
    head: {
      attachmentPoint: [number, number];
      verticalExtent: number;
      angle: number;
      direction: Direction;
    };
    hands: Record<"left" | "right", {
      attachmentPoint: [number, number];
      targetPoint: [number, number]; // effector in world space
      lengths: { upper: number; lower: number };
      positiveBend: boolean;
    }>;
    legs: Record<"left" | "right", {
      attachmentPoint: [number, number];
      targetPoint: [number, number];
      lengths: { upper: number; lower: number };
      positiveBend: boolean;
      foot: { length: number | null; direction: FootDirection };
    }>;
  };
  position: [number, number]; // mirrors skeleton.position
  measurements: StickmanMeasurements; // merged defaults + overrides
  normalizedOptions: StickmanOptions; // sanitized user input
};
```

---

## Helpers

The module also exposes a few utilities:

- `defaultMeasurements` – Deep object describing the pristine pose. Handy for cloning into custom rigs.
- `normalizeInput(value, fallback)` – Ensures any `null`/primitive input becomes `{}` (or a fallback), protecting callers from malformed FuncScript structures.
- `mergeDeep(target, source)` – Recursive merge used to overlay measurement overrides without losing nested objects. Exported so hosts can reuse the same merging behavior when composing presets.

All helpers are pure and side-effect free, making the module safe to reuse across scenes or animation frames.

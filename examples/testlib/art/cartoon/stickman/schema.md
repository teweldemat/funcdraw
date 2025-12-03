# Stickman Schema (shared types)

Common input types reused across the stickman helpers (`staticMan`, `steperManProfile`, `steperManZoom`).

## Core primitives

```ts
type Color = string;                     // CSS-compatible color string
type Direction = "front" | "back" | "left" | "right";
type Side = "left" | "right";
type FootDirection = "left" | "right" | "center";
type PointInput = [number, number] | { x: number; y: number } | { left: number; top: number };
```

All coordinates are relative to the torso anchor (`position`) unless a field is explicitly described as world-space. "Left"/"right" refer to screen-left/screen-right limbs regardless of facing.

## Measurements

```ts
type TorsoInput = {
  width?: number;
  height?: number;
  shoulderExtension?: number;
  direction?: Direction;
};

type HeadInput = {
  verticalExtent?: number;
  angle?: number;
  direction?: Direction;
};

type HandSideInput = {
  upperLength?: number;
  lowerLength?: number;
  effectorCoordinate?: PointInput; // IK target relative to the torso anchor
  positiveBend?: boolean;
};

type LegSideInput = {
  upperLength?: number;
  lowerLength?: number;
  effectorCoordinate?: PointInput; // IK target relative to the torso anchor
  positiveBend?: boolean;
  foot?: {
    length?: number | null;
    direction?: FootDirection;
  };
};

type StickmanMeasurements = {
  torso?: TorsoInput;
  head?: HeadInput;
  hands?: { left?: HandSideInput; right?: HandSideInput };
  legs?: { left?: LegSideInput; right?: LegSideInput };
};
```

Every field is optional. Defaults for lengths/offsets live in `skeleton.doc.md` and the individual limb docs.

# Stickman Model Reference

The `art/cartoon/stickman/eval.js` module exports a factory function that builds a fully posed stick figure. Import it from FuncScript with `package("@funcdraw/testlib").cartoon.stickman` (or require it directly from JavaScript). The function accepts a single _options_ argument and returns a structured object describing the rendered result.

---

## Usage

```fs
stickman:package("@funcdraw/testlib").cartoon.stickman;

hero:stickman({
  position:[20, 9];
  palette:{ torsoFill:"#0f172a"; headFill:"#fde68a"; };
  measurements:{
    legs:{
      left:{ positiveBend:true; effectorCoordinate:[-4,-4.5]; };
      right:{ positiveBend:false; effectorCoordinate:[4,-4.5]; };
    };
  };
});

graphics:hero.graphics;
```

---

## Options

All fields are optional—missing values fall back to the defaults baked into the model. Values may be plain JavaScript numbers/objects or FuncScript key-value collections/lists; the helpers normalize both forms automatically.

| Field | Type | Description |
| --- | --- | --- |
| `position` | `[x, y]` | Center-bottom anchor of the torso. Defaults to `[0, 10.5]` (leg length + foot thickness) so the feet rest with their bottom edge on `y = 0`. |
| `palette` | object | Overrides any of the colors/widths below. |
| `measurements` | object | Nested structure that tweaks body dimensions & limb targets. |

### Pseudo schema

```ts
type Color = string; // Any CSS-compatible color string
type Direction = "front" | "back" | "left" | "right";
type PointInput = [x: number, y: number] | { x: number; y: number } | { left: number; top: number }; // coordinates relative to StickmanOptions.position

type FootDirection = "left" | "right";
type Feet = {
  length?: number; // overrides the default toe-line length (≈ 2.2 units)
  direction?: FootDirection; // default mirrors positiveBend (true => "right", false => "left")
};

type StickmanOptions = {
  position?: [x: number, y: number]; // default [0, legLengthSum] (≈ [0, 10]) so the feet sit at y = 0
  palette?: {
    torsoFill?: Color; // default "#1f2937"
    torsoStroke?: Color; // default "#cbd5f5"
    torsoStrokeWidth?: number; // default 0.6
    headFill?: Color; // default "#fff7ed"
    headStroke?: Color; // default "#fdba74"
    headStrokeWidth?: number; // default 0.4
    headGazeColor?: Color; // default "#ea580c"
    skinStroke?: Color; // default "#f97316" (base stroke for limbs)
    handStroke?: Color; // defaults to skinStroke when omitted
    legStroke?: Color; // defaults to skinStroke when omitted
    handWidth?: number; // default 0.8
    legWidth?: number; // default 1.1
    footStroke?: Color; // default "#f97316" (foot line color)
    footStrokeWidth?: number; // default 0.5
    overlayHand?: Color; // default "#fb7185"
    overlayLeg?: Color; // default "#38bdf8"
  };
  measurements?: Measurements; // defaults rest the pose with limbs straight down, facing front
};

type Measurements = {
  torso?: {
    width?: number; // default 6
    height?: number; // default 11
    shoulderExtension?: number; // default width * 0.15 (≈ 0.9) to space out the arms
    direction?: Direction; // default "front"
  };
  head?: {
    verticalExtent?: number; // default 4.5
    angle?: number; // default 90 degrees
    direction?: Direction; // defaults to the torso direction
  };
  hands?: {
    left?: HandSide;
    right?: HandSide;
  };
  legs?: {
    left?: LegSide;
    right?: LegSide;
  };
};

type HandSide = {
  upperLength?: number; // default 4 (shoulder -> elbow)
  lowerLength?: number; // default 3 (elbow -> hand)
  effectorCoordinate?: PointInput; // default aligns straight under the shoulder (≈ [-3.9, 2.35] for left / [3.9, 2.35] for right)
  positiveBend?: boolean; // true = counter-clockwise bend around the limb direction; defaults keep the left elbow outward (false) and the right elbow outward (true, because CCW is +X for that side)
};

type LegSide = {
  upperLength?: number; // default 5.2 (hip -> knee)
  lowerLength?: number; // default 4.8 (knee -> ankle)
  effectorCoordinate?: PointInput; // default [±1.5, -10] so both legs drop straight down and feet reach y = 0
  positiveBend?: boolean; // true = counter-clockwise bend; defaults keep the left knee outward (false) and the right knee outward (true)
  foot?: Feet; // tweak the toe-line target (length + facing direction)
};
```

---

## Return Value

The function returns a plain object:

```ts
{
  graphics: DrawableShape[],
  overlays: { point:[x,y], color:string }[],
  skeleton: {
    position:[x,y],
    torso:{ centerBottomPoint, width, height, headAttachmentPoint, handAttachmentPoints, legAttachmentPoints },
    head:{ attachmentPoint, verticalExtent, angle },
    hands:{
      left/right:{ attachmentPoint, targetPoint, lengths:{ upper, lower }, positiveBend }
    },
    legs:{
      left/right:{ attachmentPoint, targetPoint, lengths:{ upper, lower }, positiveBend }
    }
  }
}
```

- `graphics` is the list you typically pass to FuncDraw for rendering (torso rectangle, head, limbs).
- `overlays` contains helper dots useful for debugging IK targets (they render only if you draw them yourself).
- `skeleton` exposes the resolved pose so you can inspect attachment points or reuse them in other expressions.

Use whichever parts your scene needs—many examples only consume `graphics`, while tooling/debuggers might also show `overlays` or read from `skeleton`.

### Return pseudo schema

```ts
type Color = string;
type Point = [x: number, y: number];
type DrawableShape = PrimitiveShape; // See docs/primitives-reference.md for built-in primitive fields

type StickmanResult = {
  graphics: DrawableShape[]; // draw these shapes in order
  overlays: OverlayPoint[]; // helper dots for debugging IK targets
  skeleton: SkeletonPose; // resolved pose data
};

type OverlayPoint = { point: Point; color: Color };

type SkeletonPose = {
  position: Point; // same anchor passed in options (resolved defaults applied)
  torso: TorsoFrame;
  head: HeadFrame;
  hands: HandSet;
  legs: LegSet;
};

type TorsoFrame = {
  centerBottomPoint: Point;
  width: number;
  height: number;
  shoulderExtension: number;
  direction: Direction;
  headAttachmentPoint: Point;
  handAttachmentPoints: { left: Point; right: Point };
  legAttachmentPoints: { left: Point; right: Point };
};

type HeadFrame = {
  attachmentPoint: Point;
  verticalExtent: number;
  angle: number;
  direction: Direction;
};

type LimbLengths = { upper: number; lower: number };

type HandSidePose = {
  attachmentPoint: Point;
  targetPoint: Point;
  lengths: LimbLengths;
  positiveBend: boolean;
};

type ResolvedFeet = { length: number | null; direction: FootDirection };

type LegSidePose = {
  attachmentPoint: Point;
  targetPoint: Point;
  lengths: LimbLengths;
  positiveBend: boolean;
  foot: ResolvedFeet;
};

type HandSet = { left: HandSidePose; right: HandSidePose };
type LegSet = { left: LegSidePose; right: LegSidePose };
```

Refer to `docs/primitives-reference.md` for full details on the primitive fields (`rect`, `line`, `polygon`, etc.) that make up each `DrawableShape`.

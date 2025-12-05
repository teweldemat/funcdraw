# Stickman Model
## Overview
`stickman/staticMan.js` builds the full cartoon character used throughout the examples, and `stickman/eval.js` exposes it under `package("@funcdraw/testlib").cartoon.stickman.static(options?)`. Call it with the torso center-bottom point—this anchor floats above the ground plane so the legs (and feet) can extend downward. Every limb target uses that same reference, so supplying a higher or lower `position` shifts the entire rig while keeping the ankles below the anchor. The model drops a rounded-rectangle torso at the anchor, attaches the `head` model, and draws limb segments (two-segment polylines) that reach toward their configured targets. Slim toe lines hint at feet, and optional overlay dots expose attachment/target points for debugging.

## Construction Overview

1. **Delegate to skeleton** – `eval.js` passes the caller’s `position`/`measurements` into `skeleton.fs`, which performs the IK math and returns normalized attachment points, limb lengths, bend directions, and effectors.
2. **Draw torso** – Using the skeleton’s torso frame, render the rounded rectangle and keep the attachment coordinates for downstream models.
3. **Render head** – Call `head.fs` with the skeleton’s head attachment and merge its graphics.
4. **Render arms/legs** – Feed the skeleton’s hand/leg entries into `hand.fs`/`leg.js` so they draw the two-segment limbs and toe lines toward each effector.
5. **Optional overlays** – Convert skeleton attachment/target points into small markers if you need IK debugging aids.

## Inputs

Pass a single `options` object (or omit it) when calling `stickman`. Values may be JavaScript objects or FuncScript key/value collections.

### Option schema

Shared types (`Direction`, `Side`, `FootDirection`, `PointInput`, and `StickmanMeasurements`) live in `schema.md`.

```ts
type Color = string; // CSS-compatible color

type StickmanOptions = {
  position?: PointInput; // torso center-bottom anchor; defaults to [0, 10.5] so toes land at y=0
  palette?: {
    torsoFill?: Color;        // torso rectangle interior color (default "#1f2937")
    torsoStroke?: Color;      // torso outline color (default "#cbd5f5")
    torsoStrokeWidth?: number;// torso outline width (default 0.6)
    headFill?: Color;         // head skull fill (default "#fff7ed")
    headStroke?: Color;       // head outline color (default "#fdba74")
    headStrokeWidth?: number; // head outline width (default 0.4)
    headGazeColor?: Color;    // nose accent color (default "#ea580c")
    skinStroke?: Color;       // base limb stroke color (default "#f97316")
    handStroke?: Color;       // overrides arm stroke color (defaults to skinStroke)
    handWidth?: number;       // arm stroke width (default 0.8)
    legStroke?: Color;        // overrides leg stroke color (defaults to skinStroke)
    legWidth?: number;        // leg stroke width (default 1.1)
    footStroke?: Color;       // toe-line stroke color (default "#f59e0b")
    footStrokeWidth?: number; // toe-line stroke width (default 0.5)
    overlayHand?: Color;      // debug marker color for hand targets (default "#fb7185")
    overlayLeg?: Color;       // debug marker color for leg targets (default "#38bdf8")
  };
  measurements?: StickmanMeasurements; // see schema.md for field shapes; defaults come from skeleton.doc.md
};
```

All fields are optional. Omitted palette colors fall back to the defaults inside `defaultPalette`. Missing measurement branches inherit the base pose documented in `skeleton.doc.md`; `schema.md` documents the measurement fields themselves. "Left" and "right" always refer to screen-left/screen-right limbs regardless of torso facing.

## Output

The factory returns a plain object:

```ts
type StickmanResult = {
  graphics: DrawableShape[]; // torso rectangle, head graphics, limb lines, feet
  overlays: OverlayPoint[];  // helper dots; render manually when debugging IK
  skeleton: SkeletonPose;    // resolved attachment points/targets for downstream use
};
```

- `graphics` is the list you typically hand to FuncDraw renderers or merge into larger scenes.
- `overlays` holds small circle markers describing limb targets and hips; consume them only when you need guides.
- `skeleton` exposes the computed pose (with precomputed limb joints under `skeleton.hands/legs`) so other expressions can align props, constraints, or effects without rerunning inverse-kinematics math.

See the sibling docs (`head.doc.md`, `skeleton.doc.md`, `hand.fs`, `leg.js`) for deeper geometry details reused by this model.

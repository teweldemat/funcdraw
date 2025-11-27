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
| `position` | `[x, y]` | Center-bottom anchor of the torso. Defaults to `[20, 6]`. |
| `palette` | object | Overrides any of the colors/widths below. |
| `measurements` | object | Nested structure that tweaks body dimensions & limb targets. |

### Palette fields

If omitted, the following defaults apply:

- `torsoFill:#1f2937`, `torsoStroke:#cbd5f5`, `torsoStrokeWidth:0.6`
- `headFill:#fff7ed`, `headStroke:#fdba74`, `headStrokeWidth:0.4`, `headGazeColor:#ea580c`
- `handStroke:#f97316`, `handWidth:0.8`
- `legStroke:#0ea5e9`, `legWidth:1.1`
- `overlayHand:#fb7185`, `overlayLeg:#38bdf8` (these only color the debug overlay points)

### Measurement structure

Each nested object tweaks proportions or IK targets. All numeric values are in scene units.

- `torso`
  - `width`, `height`: size of the torso rectangle and spacing between attachment points.
- `head`
  - `verticalExtent`: how tall the head ellipse is.
  - `angle`: rotation angle in degrees (0 = pointing right, 90 = upright).
- `hands`
  - `left` / `right`:
    - `upperLength`, `lowerLength`: segment lengths for shoulder→elbow and elbow→hand.
    - `effectorCoordinate:[x,y]`: world-space offset relative to `position`; `[0,0]` sits at the torso origin, `[10,10]` reaches 10 units to the right/up.
    - `positiveBend`: `true` bends the elbow using the left-hand rule (counter-clockwise from the arm direction), `false` bends the other way.
- `legs`
  - `left` / `right`:
    - `upperLength`, `lowerLength`: thigh and shin lengths.
    - `effectorCoordinate:[x,y]`: offset from `position` to the foot target (negative Y puts feet below the torso).
    - `positiveBend`: controls which side of the leg the knee bends toward (`true` = knee points inward for the left leg, outward for the right leg; swap to mirror the pose).

Default values come from `skeleton.js`:

- Torso width `6`, height `11`
- Head vertical extent `4.5`, angle `90`
- Hands: upper `4`, lower `3`, effector `[-11,11]` & `[11,11]`, both positive bends by default
- Legs: upper `4.5`, lower `4`, effector `[-4,-4.5]` & `[4,-4.5]`, both bend direction defaults to `false` (knees bending away from the body center line)

Adjust `effectorCoordinate` to move hand/foot targets, and lengths to keep limbs comfortable for the reach you want.

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

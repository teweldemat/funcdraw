# Stickman Head Model

## Overview

`head.js` renders the head used by the cartoon stickman. The model receives the neck attachment point plus a configuration object and returns the polygon/circle primitives that depict the skull, eyes, and facial details. Visually, the skull is a softly rounded polygon (many small edges approximating a circle), the eyes are circles layered on top (or an almond made from two overlapping circles for profile views), and the nose is a short line or right-angled pair of lines. The geometry adapts to four canonical facings (`front`, `back`, `left`, `right`):

- **Front** – two highlighted eyes plus a short vertical nose centered between them so it never touches the outline.
- **Profile (left/right)** – a single almond-shaped eye and a two-segment nose that points away from the face yet stays perpendicular to the skull tilt.
- **Back** – only the outline renders; the back view intentionally omits facial details.

## Construction Overview

1. **Skull** – Regular polygon (usually 20 sides) sized by `verticalExtent`. It reads as a circle with a colored fill and stroke.
2. **Eyes** – Front/back heads get two circular pupils plus a smaller highlight circle. Profile views place two overlapping circles to form an almond-eye silhouette, again with an optional highlight circle. Back heads omit eyes.
3. **Nose** – Single short line for the front view, two connected lines for profiles (one horizontal, one vertical) to suggest the bridge and tip. Back heads omit the nose.

All pieces are simple circles and lines so the model stays stylistically consistent with the rest of the stickman.

## Inputs

Most callers pass `skeleton.head.attachmentPoint` along with its measured configuration. The pseudo schema below describes the exact arguments and defaults.

```ts
type HeadDirection = "front" | "back" | "left" | "right"; // canonical facings
type PointInput =
  | [number, number]
  | { x: number; y: number }
  | { left: number; top: number }; // any 2D coordinate describing the neck joint

type HeadConfig = {
  verticalExtent?: number; // default 4.5; total head height (radius = verticalExtent / 2)
  angle?: number; // default 90; degrees, 0 = points right, 90 = upright
  fill?: string; // default "#fff7ed"; skull fill color
  stroke?: string; // default "#fdba74"; skull outline color
  strokeWidth?: number; // default 0.4; base width for outline + face details
  gazeColor?: string; // default "#ea580c"; accent color for the nose detail strokes
  segments?: number; // default 20, min 6; polygonal resolution for the head outline
  direction?: HeadDirection; // defaults to torso direction; controls which features render
  eyes?: {
    separationRatio?: number; // default 0.38, clamps to [0.1, 0.8]; lateral spacing in radii
    offsetRatio?: number; // default 0.2, clamps to [-0.2, 0.6]; moves eyes along the look vector
    radiusRatio?: number; // default 0.14, clamps to [0.05, 0.35]; pupil size relative to head
    fill?: string; // default "#0f172a"; pupil fill
    stroke?: string; // defaults to fill; pupil outline
    highlight?: string; // default "#fef9c3"; small specular highlight color
    highlightRatio?: number; // default 0.4, clamps to [0, 1]; highlight radius multiplier
  };
};

function head(attachmentPoint?: PointInput, config?: HeadConfig): {
  graphics: Graphic[]; // polygon, circles, and lines ready to merge into the scene
};
```

## Output

The call returns:

```ts
type HeadResult = {
  graphics: Graphic[];
};
```

- `graphics` – `{ type, ... }` primitives ready to be appended to the rest of the scene.

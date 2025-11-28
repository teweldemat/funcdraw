# Stickman Head Renderer

`art/cartoon/stickman/head.js` exposes a helper that paints a simple cartoon head anchored to a torso attachment point. It is consumed by the full stickman renderer but you can also call it directly from FuncScript or JavaScript whenever you need just the head graphics.

---

## Usage

```fs
head:create(package("@funcdraw/testlib").cartoon.stickman.head);

portrait:head([12, 18], {
  verticalExtent:5.2;
  angle:90; // upright
  direction:"left";
  fill:"#fde68a";
  stroke:"#f97316";
  eyes:{ separationRatio:0.45; highlight:"#fff"; };
});

graphics:portrait.graphics;
```

The factory accepts two arguments:

1. **Attachment point** – A `[x, y]` array or `{ x, y }`/`{ left, top }` object describing where the neck meets the torso. Defaults to `[20, 17]` when omitted.
2. **Config** – Optional object that tweaks the geometry, paint, and faced direction. Missing fields fall back to sane defaults.

The function returns `{ graphics, center }` where `graphics` is an array of FuncDraw primitives (polygon, circles, and lines) and `center` contains the computed head center. Consumers usually spread `graphics` into the rest of the character and reuse `center` to aim hair or accessories.

---

## Config Fields

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
  center: [number, number]; // ellipse center, handy for accessories
};
```

## Direction Rules

The renderer adapts to four canonical directions:

- **Front** – Draws two highlighted eyes plus a short vertical nose centered between them so it never touches the outline.
- **Profile (left/right)** – Swaps to a single almond-shaped eye and a two-segment nose that points away from the face yet stays perpendicular to the skull tilt.
- **Back** – Only the outline renders; the back view intentionally omits facial details.

---

## Return Value

The call returns:

```ts
type HeadResult = {
  graphics: Graphic[];
  center: [number, number];
};
```

- `graphics` – `{ type, ... }` primitives ready to be appended to the rest of the scene.
- `center` – The computed ellipse center, convenient for drawing hats or aligning the torso-top attachment point.

Use `graphics` as-is or merge it with other character parts:

```js
const { graphics: headGraphics, center } = head([0, 12], { direction: 'right' });
const heroGraphics = [...torsoGraphics, ...headGraphics];
```

Because the renderer never mutates shared state, you can instantiate multiple heads with different configs inside the same scene or animation frame.

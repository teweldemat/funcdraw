# FuncDraw primitives reference

`@funcdraw/core` consumes a FuncScript *package resolver* (see `funcscript/js-port/funcscript-js/src/core/package-loader.js`) and executes the exported expression through the FuncScript runtime. The evaluated value must describe your graphics model using the shapes below. You can control the output target when calling `loadGraphics` by setting `options.output` to `'raw'`, `'svg'`, or a list such as `['raw', 'svg']`. Raw output exposes the normalized scene graph (including `view`, `step`, and warnings). SVG output converts the primitives to SVG `<path>/<line>/<rect>` elements with text rendered as precise glyph outlines.

```js
const { loadGraphics } = require('@funcdraw/core');
const { createExpression } = require('@funcdraw/core');
const expression = createExpression(resolver, { font: './fonts/Inter-Regular.ttf' });

const { step, view, warnings, raw, svg } = expression.evaluate({
  output: ['raw', 'svg']
});

`step`, `view`, and `warnings` are always returned for convenience even if you only request a single output format. For one-off calls you can still use `loadGraphics(resolver, options)`, but expression handles let you reuse the parsed package with different evaluation options.
```

## Evaluation model

- **Lists** – An array (or nested arrays) represents the full scene. Inner arrays are preserved so you can emit layers such as `[background, foreground]`.
- **Objects** – Any object that exposes a `type` string is treated as a primitive. If the `type` matches one of the built-ins below it is rendered directly; otherwise, the object is considered *custom* whenever it also includes a `graphics` property that contains nested primitives.
- **Stepper functions** – When the resolved package exposes a `step` function, FuncDraw wraps it and returns a JavaScript function so hosts can advance state between frames. The stepper receives and returns plain values, and shares the same `fd` helpers that were available during the initial evaluation.
- **Viewports** – To declare the drawing viewport, return an object with a `view` property and a sibling `graphics` property. The `view` value is surfaced verbatim (e.g. `{ size: [16,9], unit: "in" }`) and the `graphics` collection is interpreted using the rules above.

FuncDraw injects an `fd` variable into every evaluation scope. Additional helpers will be added over time, but the core helper today is `fd.measureText` which mirrors the function documented below. You can provide your own `measureText` implementation (or extend the `fd` object) when calling `loadGraphics`.

> The primitives described below collectively form the `DrawableShape` alias referenced throughout the examples and helper docs.

## Fonts & glyphs

`loadGraphics` inspects glyph data from the font you pass via the `font` option. If you omit the option FuncDraw automatically loads the bundled [Inter](https://rsms.me/inter/) Regular cut, so text metrics remain deterministic across machines:

```js
loadGraphics(resolver, { font: './fonts/SpaceGrotesk-Regular.ttf' });
```

The value can be a filesystem path, a Buffer/TypedArray, or an object such as `{ path, buffer, data }`. When omitted, FuncDraw uses the packaged Inter Regular font stored under `assets/fonts/Inter-Regular.ttf`, so measurements still work in headless environments and match the SVG glyph outlines. The parsed font powers the default `fd.measureText` helper, ensuring your expressions see the same metrics that the renderer uses.

FuncDraw.Net mirrors this behavior using a local `fonts/` directory (tracked under `FuncDraw.Net/fonts/` and copied next to the executable). When a `text` primitive sets `font`, FuncDraw.Net requires the value to match an available font file in that folder (e.g. `Inter-Regular.ttf`); unknown font names error.

You can override `fd.measureText` entirely (for example to clamp to integer widths or include extra metadata) while still benefiting from the parsed font: pass your own function through `fd.measureText` and capture any state you need in the surrounding closure.

## Shared rules
- **Coordinates** – All positions are `[x, y]` pairs in the same units as your `view` expression.
- **Colors** – `stroke`, `fill`, and `color` accept either a CSS color string or an srgb color object: `{ type:"color"; space:"srgb"; r:<0-255>; g:<0-255>; b:<0-255>; a:<0-1>; }`.
- **Stroke width** – Expressed in world units and scaled at draw time so exports stay crisp at any resolution.
- **Layers** – Returning `[[...], [...]]` yields multiple layers; inner order is preserved exactly.
- **Compositing** – Any primitive may include `opacity` (number) and/or `blendMode` (string). `opacity` multiplies into the current alpha; `blendMode` is forwarded to the renderer (e.g. Canvas `globalCompositeOperation`, SVG `mix-blend-mode`).

## Primitive specs

### `line`
- `from`, `to`: required points.
- `stroke`: default `#38bdf8`.
- `width`: default `0.25`.
- `dash`: optional array of non‑negative numbers.

### `rect`
- `position`: bottom-left corner.
- `size`: `[width, height]`.
- `fill`: optional.
- `stroke`: optional (default `#38bdf8`).
- `width`: default `0.25`.

### `circle`
- `center`: point.
- `radius`: > 0.
- `fill`, `stroke`, `width` behave like `rect`.

### `ellipse`
- `center`: point.
- `radiusX`, `radiusY`: > 0.
- `fill`, `stroke`, `width` follow the same defaults.

### `polygon`
- `points`: array of ≥3 points.
- `fill`, `stroke`, `width` optional.

### `text`
- `position`: anchor point (baseline-middle in the renderer).
- `text`: required string.
- `color`: default `#e2e8f0`.
- `fontSize`: default `12` (world units).
- `font`: optional font file name from `fonts/` (FuncDraw.Net), defaults to `Inter-Regular.ttf`.
- `align`: `left` (default), `center`, or `right`.

### `transform`
`transform` applies an affine transform matrix to nested graphics.

- `matrix`: required affine matrix `[a, b, c, d, e, f]` where:
  - `x' = a*x + c*y + e`
  - `y' = b*x + d*y + f`
- `graphics`: a primitive, list of primitives, or nested layers to transform.

### `group`
`group` applies compositing settings to nested graphics without changing geometry.

- `graphics`: required nested primitives/layers.
- `opacity`: optional number multiplied into alpha.
- `blendMode`: optional string forwarded to the renderer (e.g. `"multiply"`).

### `debug`
`debug` entries don’t draw shapes—they emit overlays so you can inspect intermediate values while iterating.

- `text`: optional string shown verbatim.
- `value`: any value that will be JSON‑stringified when `text` is absent.
- `color`: optional highlight color (default `#f97316`).
- `data`: legacy payloads are preserved and treated like `value`, making it easy to dump whole objects.

Debug overlays never affect layout; they simply render on top and log nothing to the warnings list.

### Custom primitives

Unknown `type` values are treated as custom graphics when you also return a `graphics` array. FuncDraw will preserve your other properties under `props` so renderers can pass custom metadata to shaders, DOM nodes, etc.

`opacity` and `blendMode` are treated as compositing fields, so they are lifted onto the custom primitive wrapper (they are not stored under `props`).

```funcscript
{
  type: "heatmap";
  graphics: [
    { type: "rect"; position:[0,0]; size:[1,2]; fill:"#f00"; }
  ];
  palette:"thermal";
}
```

## `fd.measureText`

FuncDraw exposes `fd.measureText(text, fontSize?, options?)` so your expressions can respond to glyph-level metrics derived from the loaded font. The helper runs in the FuncScript runtime so you can call it from any expression.

- `text` (string) – required.
- `fontSize` (number) – optional, defaults to `12` if omitted or invalid.
- `options` (record) – optional tuning knobs:
  - `letterSpacing` (world units added between glyphs, default `0`)
  - `lineHeight` (multiplier applied to the font’s ascender/descender span, default `1.2`)

The function returns an object containing any mix of `width`, `lineHeight`, `height`, `lines`, `ascent`, `descent`, `baseline`, and `avgCharWidth` whenever those metrics can be computed. You can safely destructure just the properties you need.

When calling `loadGraphics` you may pass `{ fd: { measureText: customImpl } }` to override the built-in glyph math or feed the helper with extra properties through `{ fd: { expose: { ... } } }`.

SVG output uses the same font and measurement helper to convert every text primitive into glyph paths, keeping layout consistent between raw data and exported vectors.

## `fd.rotate` / `fd.translate` / `fd.scale`

FuncDraw exposes lightweight transform helpers that return a `transform` primitive.

- `fd.translate(graphics, dx, dy)` – Wrap `graphics` in a translation transform.
- `fd.rotate(graphics, origin, angleRadians)` – Wrap `graphics` in a rotation around `origin` (`[x, y]`).
- `fd.scale(graphics, origin, scaleX, scaleY)` – Wrap `graphics` in a scale transform around `origin` (`[x, y]`).
- `fd.traslate(...)` is a typo and will error; use `fd.translate`.

## `fd.color`

FuncDraw exposes `fd.color` helpers that return an srgb color object (so you don’t have to build `#RRGGBBAA` strings by hand).

- `fd.color.rgb(r, g, b)` – returns `{ type:"color"; space:"srgb"; r; g; b; a:1 }`.
- `fd.color.rgba(r, g, b, a)` – returns `{ type:"color"; space:"srgb"; r; g; b; a }`.
- `fd.color.hex("#RRGGBB")` – parses hex into an srgb color object (`#RGB`, `#RGBA`, `#RRGGBB`, `#RRGGBBAA` supported).
- `fd.color.parse(value)` – parses either a hex string or an existing srgb color object.
- `fd.color.alpha(value, a)` – sets alpha on a parsed color.
- `fd.color.mulAlpha(value, factor)` – multiplies alpha on a parsed color.

## `fd.boundingbox`

Returns the axis-aligned bounding box of a graphics value (primitive, list, nested layers, or custom nodes with a `graphics` payload), including any nested `transform` matrices.

The result is `{ left, bottom, right, top, width, height }` or `null` when no drawable primitives are found. For stroked primitives the box includes the stroke width.

## Layered output

Graphics expressions may return a single primitive, a flat array, or a list of lists. Each inner array is treated as a layer, drawn in sequence. Primitives with unsupported `type` values are skipped and reported in the warning sidebar/logs so you can catch typos quickly.

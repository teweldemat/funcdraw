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

You can override `fd.measureText` entirely (for example to clamp to integer widths or include extra metadata) while still benefiting from the parsed font: pass your own function through `fd.measureText` and capture any state you need in the surrounding closure.

## Shared rules
- **Coordinates** – All positions are `[x, y]` pairs in the same units as your `view` expression.
- **Colors** – `stroke` and `fill` accept any CSS color string; defaults are noted per primitive.
- **Stroke width** – Expressed in world units and scaled at draw time so exports stay crisp at any resolution.
- **Layers** – Returning `[[...], [...]]` yields multiple layers; inner order is preserved exactly.

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
- `fontSize`: default `1` (world units).
- `align`: `left` (default), `center`, or `right`.

### `debug`
`debug` entries don’t draw shapes—they emit overlays so you can inspect intermediate values while iterating.

- `text`: optional string shown verbatim.
- `value`: any value that will be JSON‑stringified when `text` is absent.
- `color`: optional highlight color (default `#f97316`).
- `data`: legacy payloads are preserved and treated like `value`, making it easy to dump whole objects.

Debug overlays never affect layout; they simply render on top and log nothing to the warnings list.

### Custom primitives

Unknown `type` values are treated as custom graphics when you also return a `graphics` array. FuncDraw will preserve your other properties under `props` so renderers can pass custom metadata to shaders, DOM nodes, etc.

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

## Layered output

Graphics expressions may return a single primitive, a flat array, or a list of lists. Each inner array is treated as a layer, drawn in sequence. Primitives with unsupported `type` values are skipped and reported in the warning sidebar/logs so you can catch typos quickly.

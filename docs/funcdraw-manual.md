# FuncDraw Manual

FuncDraw is a vector graphics authoring system where artwork, motion, and interaction are written as code.
Models are authored in **FuncScript** (a pure, expression-oriented language) and evaluated into a scene
made of simple vector primitives (rects, lines, text, transforms, …). Scenes can be **static**, **animated**
via a time value `t`, and **interactive** via a `step(event)` function.

This manual focuses on **how to author FuncDraw art packages** and scenes. For full language details, see
the FuncScript docs under `funcscript/docs/`.

---

## Quick Start (Local Preview)

1. Install the preview CLI:

```bash
npm install --save-dev @funcdraw/play
```

2. Add a script:

```json
{
  "scripts": {
    "play": "funcdraw-play"
  }
}
```

3. Create `art/view.fs`:

```funcscript
{
  left: -12;
  bottom: -9;
  right: 12;
  top: 9;
}
```

4. Create `art/graphics.fs`:

```funcscript
[
  { type: "rect"; position: [-12, -9]; size: [24, 18]; fill: "#0b1220"; stroke: "none"; width: 0; },
  { type: "circle"; center: [0, 0]; radius: 3; fill: "#38bdf8"; stroke: "#0f172a"; width: 0.3; },
  { type: "text"; text: "FuncDraw"; position: [0, -6]; align: "center"; fontSize: 2.4; color: "#e2e8f0"; }
]
```

5. Run:

```bash
npm run play
```

FuncDraw Play watches `art/` and hot-reloads on changes.
It evaluates the `art/` package root; to render, the root value must be a scene object (`view` + `graphics`)
or a primitive/list of primitives. Use `--exp` to evaluate a snippet with `art` bound to the loaded package.

Optional (this repo): the .NET preview server is used by the examples’ `nplay` script:

```bash
dotnet run --project FuncDraw.Net -- --root .
```

---

## Core Concepts

### Expressions
An **expression** is a `.fs` file in your `art/` tree. It evaluates to a value (often a drawable scene,
a function, a list of primitives, or a helper object).

FuncScript is expression-oriented: blocks are records unless you use `eval` to pick a single result.

```funcscript
{
  a: 2;
  b: 3;
  eval a + b; // => 5 (not {a:2; b:3})
}
```

### FuncScript patterns you'll use a lot

- **Blocks + `eval`**: declare intermediate bindings, then return one final value.
- **Lambdas**: `(x) => ...` or `(value, index) => { ... }`.
- **Lists**: `Range(...)`, `map`, `filter`, `reduce`, `Len(...)`, `First(...)`.
- **Merging**: `a + b` merges objects and concatenates lists (used heavily for composing graphics).
- **Null-safe access**: `x ?? fallback` and `x?.field`.

### Art Packages, Collections, and Modules
An **art package** is an npm package that contains an `art/` folder. FuncDraw loads that folder using a
filesystem-backed FuncScript package resolver.

Folders evaluate in one of two ways:

- **Collection** (no `eval.fs`): the folder evaluates to a key–value object where each key is a child file
  or folder name, and each value is the evaluated child.
- **Module** (contains `eval.fs`): the folder evaluates *only* to the result of `eval.fs`. Its internal
  files and folders are not directly accessible unless `eval.fs` returns them.

This is the core “filesystem-as-API” rule: **file and folder names are referenced directly in code and are
part of the public API of your art package**.

#### Encapsulation scenarios (collections vs modules)

The easiest way to understand the difference is to look at what a *consumer can access*.

##### Scenario 1: A collection exports its structure directly

Folder (no `eval.fs`):

```text
art/
  view.fs
  graphics.fs
```

The `art/` root evaluates to `{ view, graphics }`, so it is already a valid scene object (this is the
Quick Start pattern). If this were published as a dependency, a consumer could access:

```funcscript
{
  pkg: package("@yours/pkg");
  eval { view: pkg.view; graphics: pkg.graphics; };
}
```

##### Scenario 2: Collections are transparent (everything is public)

```text
art/
  ui/
    button.fs
    slider.fs
    _layout.fs
```

Because `ui/` is a collection, **all** of these are addressable:

```funcscript
{
  ui: package("@yours/pkg").ui;
  eval [ui.button, ui.slider, ui._layout];
}
```

If you want `_layout.fs` to be private, make `ui/` a module.

##### Scenario 3: A module hides internals and exports an explicit API (via `eval.fs`)

```text
art/
  ui/
    eval.fs
    button.fs
    slider.fs
    _layout.fs
```

`ui/eval.fs` decides what the folder exports:

```funcscript
{
  button: button;
  slider: slider;
  // _layout is intentionally not exported
}
```

Now consumers see only the exported surface:

```funcscript
{
  ui: package("@yours/pkg").ui;
  eval [ui.button, ui.slider];
}
```

And this is **not** accessible anymore:

```funcscript
package("@yours/pkg").ui._layout
```

##### Scenario 4: Facade module (indirect exposure without re-exporting)

Re-exporting a single child unchanged (for example `{ types; }`) can be useful when you want to hide other
siblings in the folder. But if the parent folder is just a thin wrapper around `types/`, it's redundant.
The more useful pattern is a *facade module* that exports a stable entrypoint while keeping the internal
implementation (and its helpers) private.

```text
art/
  house/              # module (facade)
    eval.fs
    types/            # module (private implementation)
      eval.fs
      cottage.fs
      igloo.fs
      _common.fs
```

`house/types/eval.fs` (exports only the supported constructors; hides `_common`):

```funcscript
{ cottage; igloo; }
```

`house/eval.fs` (exports one function; does not expose `types`):

```funcscript
(options) =>
{
  kind: options.kind ?? "cottage";
  builder:
    if kind == "cottage" then types.cottage
    else if kind == "igloo" then types.igloo
    else error("house: unknown kind " + kind);
  eval builder(options);
}
```

Consumer usage:

```funcscript
{
  house: package("@yours/pkg").house;
  eval house({ kind: "igloo"; anchor:[0,0]; width: 60; stories: 2; doorOpen: 0; lightColor: "#0f172a"; });
}
```

##### Scenario 5: Modules make internal refactors non-breaking

Because consumers only rely on what `eval.fs` returns, you can freely rename/restructure internals
as long as you keep the exported keys stable. For example, you can rename `button.fs` to
`primaryButton.fs` and keep the API by adjusting `eval.fs`:

```funcscript
{
  button: primaryButton;
  slider: slider;
}
```

### JavaScript expressions (`.js`)
FuncDraw loaders also accept `.js` files inside `art/`. They are evaluated as JavaScript snippets (no
`module.exports` wrapper) and should `return` the value for that expression. The snippet runs with the same
package scope available as in FuncScript (siblings, nested folders, `package(...)`, etc.).

### The `package("<name>")` function
`package("<npm-name>")` loads another FuncDraw art package and evaluates its `art/` root. The returned value
is whatever that root evaluates to (a collection object, or a module result if it has `eval.fs`).

---

## The Scene Model

FuncDraw ultimately renders **graphics primitives**. A scene can be either:

1. A *scene object*:

```funcscript
{
  view: { left: -10; bottom: -10; right: 10; top: 10; };
  graphics: [ /* primitives or nested lists */ ];
  step: (event) => null; // optional (see “Interactivity”)
}
```

2. Or directly a primitive / list of primitives (no explicit `view`).

### `view` and coordinates
`view` is the world-space rectangle mapped to the canvas:

- `{ left, bottom, right, top }` (recommended)
- or shorthand `[width, height]` meaning `{ left:0; bottom:0; right:width; top:height }`

If `view` is omitted or invalid, renderers fall back to a default view (typically `1920×1080` from
`(0,0)` to `(1920,1080)`).

World coordinates are **Y-up** (increasing `y` goes upward). `rect.position` is the **bottom-left** corner.

### Layering and grouping
`graphics` is rendered in order: earlier items are “behind” later ones.

To group primitives you can either:

- nest lists: `[background, [cloud1, cloud2], foreground]`
- or use `{ type:"group"; graphics:[...] ; opacity: ...; blendMode: ... }`

Groups matter when you want shared `opacity` / `blendMode` or when you want a stable subtree to attach
metadata to.

---

## Graphics Primitives (Built-ins)

All primitives are plain objects with a `type` field. Extra fields (like `name`, `index`, etc.) are allowed
and are useful for filtering and composition.

Defaults to know:

- For stroked primitives, `stroke` defaults to `"#38bdf8"` when omitted.
- `fill` defaults to `"none"` (transparent) when omitted.
- `width` defaults to `0.25` in most renderers (set `stroke:"none"` or `width:0` to disable stroke).

Common optional fields on many nodes:

- `opacity`: number (multiplies alpha; `1` is default)
- `blendMode`: string (canvas `globalCompositeOperation` / SVG `mix-blend-mode`; e.g. `"multiply"`)
- `name`: string (author-defined identifier)

### `line`
```funcscript
{ type:"line"; from:[x1,y1]; to:[x2,y2]; stroke:"#38bdf8"; width:0.25; dash:[4,2]; }
```

Note: SVG export supports `dash`; the current canvas preview renderer may ignore it.

### `rect` / `rectangle`
```funcscript
{ type:"rect"; position:[x,y]; size:[w,h]; fill:"#0b1220"; stroke:"#38bdf8"; width:0.25; }
```

### `circle`
```funcscript
{ type:"circle"; center:[x,y]; radius:r; fill:"#38bdf8"; stroke:"#0f172a"; width:0.25; }
```

### `ellipse`
```funcscript
{ type:"ellipse"; center:[x,y]; radiusX:rx; radiusY:ry; /* or rx/ry */ }
```

### `polygon`
```funcscript
{ type:"polygon"; points:[[x1,y1],[x2,y2],[x3,y3]]; fill:"none"; stroke:"#38bdf8"; width:0.25; }
```

### `polyline`
```funcscript
{ type:"polyline"; points:[[x1,y1],[x2,y2],[x3,y3]]; stroke:"#38bdf8"; width:0.25; }
```

Note: SVG export supports `polyline`; the current canvas preview renderer may skip it.

### `path`
```funcscript
{ type:"path"; d:"M0 0 L10 0 L10 10 Z"; fill:"none"; stroke:"#38bdf8"; width:0.25; }
```

Note: SVG export supports `path`; the current canvas preview renderer may skip it.

### `text`
```funcscript
{ type:"text"; text:"Hello"; position:[x,y]; fontSize:2.4; align:"left"|"center"|"right"; color:"#e2e8f0"; }
```

- `position` is a baseline point for the first line.
- Multi-line text is supported via `"\n"`.

### `transform`
```funcscript
{
  type: "transform";
  matrix: [a, b, c, d, e, f];     // affine matrix
  graphics: [ /* primitives */ ];
}
```

Matrix matches SVG’s `matrix(a b c d e f)` and is applied to child graphics.

### `group`
```funcscript
{ type:"group"; graphics:[ /* primitives */ ]; opacity:0.8; blendMode:"multiply"; }
```

### `debug`
`{ type:"debug"; ... }` is a reserved node that is not rendered. Some runtimes may log a warning when it is
present.

---

## Custom / Composite Nodes

If you return an object with:

- an unknown `type` **and**
- a `graphics` field containing primitives,

FuncDraw treats it as a **custom composite**: it renders `graphics` while keeping your extra properties.
This is useful for building higher-level components that carry metadata without losing renderability.

```funcscript
{
  type: "bus";
  doorOpen: 0.4;
  graphics: [ /* primitives */ ];
}
```

---

## Colors

Paint fields (`fill`, `stroke`, `color`) accept:

- Hex strings: `#RGB`, `#RGBA`, `#RRGGBB`, `#RRGGBBAA`
- Or `fd.color.*` values (see below)

For transparency, prefer:

```funcscript
fd.color.alpha("#93c5fd", 0.28)
```

---

## The `fd` Helper (FuncDraw Runtime Functions)

FuncDraw injects a global `fd` object with helpers commonly used in graphics authoring:

### Text measurement
```funcscript
metrics: fd.measureText("Hello", 2.4);
// metrics.width, metrics.ascent, metrics.descent, metrics.lineHeight, metrics.lines
```

### Bounding boxes
```funcscript
box: fd.boundingBox(graphics); // returns {left,bottom,right,top,width,height} or null
```

### Convenience transforms
```funcscript
fd.translate(graphics, dx, dy)
fd.rotate(graphics, origin /* [x,y] */, angleRadians)
fd.scale(graphics, origin /* [x,y] */, sx, sy)
```

### Color helpers
```funcscript
fd.color.rgb(r, g, b)
fd.color.rgba(r, g, b, a)
fd.color.hex("#38bdf8")
fd.color.parse("#38bdf8")        // accepts hex or fd.color objects
fd.color.alpha("#38bdf8", 0.5)
fd.color.mulAlpha("#38bdf8", 0.5)
```

---

## Animation (Time Hook)

FuncDraw Play injects a value hook named `t` (time in seconds). If your scene reads `t`, the preview UI
enables play/pause and reset controls and re-evaluates the model as `t` changes.

Typical pattern:

```funcscript
{
  angle: t * 0.8;
  spinner:
    fd.rotate(
      { type:"rect"; position:[-1,-1]; size:[2,2]; fill:"#fbbf24"; stroke:"none"; width:0; },
      [0,0],
      angle);
  eval { view:{ left:-6; bottom:-6; right:6; top:6; }; graphics:[spinner]; };
}
```

---

## Responsive Scenes (Canvas Size Hook)

FuncDraw Play injects `canvas.size.width` and `canvas.size.height`. If your scene reads `canvas`, the
preview automatically re-evaluates on window resize.

Use this to preserve aspect ratio or choose framing dynamically (see `examples/testcompose/.../common.fs`).

---

## Interactivity (Steppers, State, and Events)

### Stateful scenes
A scene can be **a function** that receives a model `state` and returns a scene object:

```funcscript
(state) =>
{
  count: if state == null then 0 else state.count;
  eval
  {
    view: { left:-10; bottom:-10; right:10; top:10; };
    graphics: [{ type:"text"; text:"count: " + count; position:[0,0]; align:"center"; fontSize:2; color:"#e2e8f0"; }];
    step: (event) => null;
  };
}
```

The host (e.g. FuncDraw Play) re-evaluates the scene after steps, passing your returned `state` back in.

### The `step(event)` contract
If your scene (or component) exposes a stepper:

- `step(event)` returns either `null` (no changes) or:

```funcscript
{
  state: { /* new state (can be any value) */ };
  events: [ /* optional follow-up events */ ];
}
```

FuncDraw Play also accepts `[nextState, events]` as a shorthand step result shape.

Returning `null` is important for performance and for avoiding unnecessary re-renders.

### Pointer events (FuncDraw Play)
FuncDraw Play sends pointer events in world coordinates:

```funcscript
{
  type: "pointer";
  action: "down"|"move"|"up"|"cancel"|"gotcapture"|"lostcapture";
  point: { x:number; y:number; };
  pointer: { id:number; type:string; isPrimary:bool; /* more fields */ };
  modifiers: { alt:bool; ctrl:bool; meta:bool; shift:bool; };
  button:number;
  buttons:number;
  time:number; // current timeline time
}
```

### Component pattern (options + state)
A common pattern (used throughout `examples/testlib`) is:

```funcscript
(options, state) =>
{
  // read options + previous state (state is null initially)
  // compute graphics
  // define stepper that updates internal state + emits events
  eval { graphics: /* list */; step: (event) => /* null or {state; events} */; };
}
```

The parent scene composes components by:

1. calling each component with its `options` and its last `state`
2. collecting the returned `graphics`
3. combining `step` functions into one top-level `step`

See `examples/testcompose/art/characterMeasurementEditor/stepper.fs` for a full example of composing
multiple child steppers (sliders, toggles, drag handles) into one interactive scene.

---

## Testing FuncDraw Packages

FuncDraw uses the FuncScript package test framework:

- Put tests next to an expression as `<name>.test.fs`
- Tests return `eval [ { name; test; cases?; }, ... ]`

Example (manual function invocation):

```funcscript
{
  eval [
    {
      name: "adds correctly";
      test: (fn) => [
        assert.equal(fn(2, 3), 5),
        assert.equal(fn(-1, 4), 3)
      ];
    }
  ];
}
```

Run tests with:

```bash
funcdraw-play --test
```

---

## Debugging and CLI Tips (FuncDraw Play)

Useful flags:

- `--debug`: print evaluated scene payloads and warnings
- `--dump`: evaluate once and exit (good for CI)
- `--svg` / `--svg output.svg`: include SVG output (print or write)
- `--t <seconds>`: seed the `t` hook (especially useful with `--dump`)
- `--canvas <w> <h>`: seed the canvas size (drives the `canvas` hook and projection)
- `--trace`: emit FuncScript package trace information
- `--exp <snippet>`: evaluate an expression snippet with `art` bound to the loaded package

Example:

```bash
funcdraw-play --dump --svg output.svg --t 2.5 --exp "art.wakeUpRoutine"
```

---

## Authoring Guidelines

- Treat file/folder names under `art/` as API: rename carefully.
- Prefer small, parametric functions that return graphics (and optional steppers).
- Attach `name`/`index` metadata to primitives when downstream code needs to filter or restyle them.
- Use `fd.measureText` and `fd.boundingBox` to keep layouts robust across dynamic content.
- When you can, return `null` from `step` if nothing changed.

## Examples in This Repo

- `examples/testlib`: reusable UI (`art/ui/*`) + drawing helpers (`art/cartoon/*`) with `.test.fs` suites.
- `examples/testcompose`: composition demos (animated `wakeUpRoutine`, interactive `characterMeasurementEditor`).

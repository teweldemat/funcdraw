# Creating Art with FuncDraw

This tutorial walks through building a small FuncDraw scene from scratch. We'll re-create the JavaScript version of the "star showcase" example, but we'll pretend we're crafting our own piece called **`my-art`**. By the end you'll understand how FuncDraw packages are structured, how reusable helpers live alongside your main expression, and how `funcdraw-play` stitches everything together.

We'll tackle the project in bite-sized steps. Each section builds on the previous one, so open a terminal in the repo root and follow along.

## 1. Understand the Package Layout

FuncDraw expects each scene to live in an `art/` folder. Inside that folder you can have:

- `eval.js` (or `.fs`) – the entry point that returns your final scene object.
- Any number of helper files – JavaScript or FuncScript – that the entry expression can reference directly (no imports required within the same package).

The CLI (`funcdraw-play`) resolves this structure automatically, so you don't need a build system. We'll use the existing `examples/star-showcase-js` project as our playground and rename the concept to `my-art` inside the tutorial.

## 2. Inspect the Example

Navigate into the example and open the art folder:

```bash
cd examples/star-showcase-js
ls art
# eval.js  scene.js
```

`art/eval.js` simply returns whatever `scene.js` exports, so the real work happens in `scene.js`. Open it to see the building blocks:

```js
// art/scene.js
const view = { left: 0, bottom: 0, right: 60, top: 40 };

const colors = ["#facc15", "#4ade80", "#38bdf8", "#fb7185", "#f472b6"];

function pickColor(index) {
  return colors[index % colors.length];
}

function buildStar(centerX, centerY, size, color) {
  return {
    type: "polygon",
    points: createStarPoints(centerX, centerY, size),
    fill: color,
    stroke: "#0f172a",
    width: 0.4
  };
}

function createStarPoints(cx, cy, radius) {
  const points = [];
  for (let idx = 0; idx < 5; idx++) {
    const tipAngle = (idx * 72 * Math.PI) / 180;
    const innerAngle = ((idx + 0.5) * 72 * Math.PI) / 180;
    points.push([cx + Math.cos(tipAngle) * radius, cy + Math.sin(tipAngle) * radius]);
    points.push([cx + Math.cos(innerAngle) * radius * 0.5, cy + Math.sin(innerAngle) * radius * 0.5]);
  }
  return points;
}

const stars = Array.from({ length: 5 }, (_, idx) => {
  const centerX = 8 + idx * 10;
  const centerY = 6 + (idx % 2 === 0 ? 8 : 14);
  const size = 4 + idx * 0.5;
  const color = pickColor(idx);
  return buildStar(centerX, centerY, size, color);
});

return {
  view,
  graphics: stars
};
```

This is our template for `my-art`:

1. Set up a viewport.
2. Define some palette helpers.
3. Build reusable geometry functions (`buildStar`, `createStarPoints`).
4. Produce an array of primitives and return them from the entry expression.

## 3. Rename the Concept (optional)

For clarity, imagine we want to brand this as `my-art`. Nothing changes technically—we'll just keep that name in mind when describing the steps.

## 4. Explain the Scene Structure

Break the scene into three responsibilities:

1. **View** – the coordinate system and overall canvas.
2. **Palette & Helpers** – color choices, geometry, utility functions.
3. **Graphics** – the array of primitives FuncDraw will render.

We'll rebuild each piece and explain why it matters.

### 4.1 Define the View

Every FuncDraw scene should return a `view` describing the min/max x/y. In JavaScript, you can simply set a constant object:

```js
const view = { left: 0, bottom: 0, right: 60, top: 40 };
```

This tells FuncDraw Play how to scale the canvas. You can use any numbers—stick to a scale that makes sense for your artwork.

### 4.2 Build a Palette Helper

Reusable helpers keep the main scene tidy. Here we define a color array and a simple accessor:

```js
const colors = ["#facc15", "#4ade80", "#38bdf8", "#fb7185", "#f472b6"];

function pickColor(index) {
  return colors[index % colors.length];
}
```

If you prefer FuncScript, you could declare a `components/palette.fs` or inline `func palette()` block. The principle is the same: isolate logic that you can reuse or tweak.

### 4.3 Create a Star Primitive Helper

`buildStar` returns a FuncDraw polygon primitive with fill/stroke/width settings. Keeping this in a function means you can experiment with different strokes or shapes without touching the main list.

```js
function buildStar(centerX, centerY, size, color) {
  return {
    type: "polygon",
    points: createStarPoints(centerX, centerY, size),
    fill: color,
    stroke: "#0f172a",
    width: 0.4
  };
}
```

### 4.4 Generate the Star Points

`createStarPoints` is the only "math heavy" helper. It maps over five tips, inserting both the outer tip and the inner notch to form a ten-point polygon.

```js
function createStarPoints(cx, cy, radius) {
  const points = [];
  for (let idx = 0; idx < 5; idx++) {
    const tipAngle = (idx * 72 * Math.PI) / 180;
    const innerAngle = ((idx + 0.5) * 72 * Math.PI) / 180;
    points.push([cx + Math.cos(tipAngle) * radius, cy + Math.sin(tipAngle) * radius]);
    points.push([cx + Math.cos(innerAngle) * radius * 0.5, cy + Math.sin(innerAngle) * radius * 0.5]);
  }
  return points;
}
```

You could swap this for any other geometry (rectangles, text, arcs). The key takeaway: the helper returns raw coordinates, which makes testing easier.

### 4.5 Assemble the Graphics Array

Now iterate and generate the star data. This version uses deterministic positions so the layout is stable, but you could also call `Math.random` if you want randomness.

```js
const stars = Array.from({ length: 5 }, (_, idx) => {
  const centerX = 8 + idx * 10;
  const centerY = 6 + (idx % 2 === 0 ? 8 : 14);
  const size = 4 + idx * 0.5;
  const color = pickColor(idx);
  return buildStar(centerX, centerY, size, color);
});
```

### 4.6 Return the Scene

FuncDraw expects the entry expression to return `{ view, graphics }`. You can also include overlays, animations, or hooks, but for `my-art` we keep it minimal:

```js
return {
  view,
  graphics: stars
};
```

## 5. Wire Up `art/eval.js`

`art/eval.js` can stay tiny—just export the result of your helper file:

```js
return scene;
```

(`scene` is the name we exported from `scene.js`. You can rename it to `myArt` if you prefer; just make sure `eval.js` returns it.)

## 6. Preview with FuncDraw Play

From `examples/star-showcase-js`, run:

```bash
npm install   # only needed once
npm run play  # launches FuncDraw Play
```

The browser opens to the preview interface. You can play/pause animations (if your scene uses the `t` hook), toggle overlays, and see warnings in the console.

Want to script the output instead? Use dump mode:

```bash
npm run play -- --dump
```

This prints the evaluated scene (including warnings and value-hook usage) without launching a server—handy for automated checks.

## 7. Customize Further

With the basics in place, try these variations:

1. **Randomize Layout** – replace the deterministic `centerX/centerY` values with `Math.random` to scatter stars each run.
2. **Add Animations** – introduce a `getTimeHook()` helper (see `examples/stick-man-js/art/eval.js`) and adjust star rotation/scale based on `t`.
3. **Split Helpers** – move palette or geometry helpers into their own files (e.g., `art/palette.js`) and reference them directly.
4. **Use FuncScript** – rewrite the scene in `.fs` if you prefer FuncScript syntax; the structural ideas stay the same.

## 8. Recap

To create `my-art` you:

1. Defined a view and helper functions in `art/scene.js`.
2. Exposed the scene from `art/eval.js`.
3. Previewed it with `funcdraw-play`.

That's the standard workflow for any FuncDraw piece. Iterate on helpers, compose primitives, and lean on the folder-as-module pattern to keep files small and discoverable.

# Creating Art with FuncDraw

This tutorial shows how to craft a FuncDraw package completely from scratch—no starter templates or example folders required. By the end you'll have a working scene, a repeatable project structure, and enough context to keep iterating on your own ideas.

We'll build a small JavaScript scene named **`my-art`** that paints a row of colorful stars. JavaScript is the on-ramp here because most newcomers already know it. Once you're comfortable with the workflow, consider switching to FuncScript—it offers functional purity.

---

## 1. Initialize a Workspace

Pick any empty directory for your artwork and create a Node package there:

```bash
mkdir my-art && cd my-art
npm init -y
npm install --save-dev @funcdraw/play
```

Update `package.json` so FuncDraw Play is easy to launch:

```jsonc
{
  "name": "my-art",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "play": "funcdraw-play"
  },
  "devDependencies": {
    "@funcdraw/play": "^0.1.0"
  }
}
```

FuncDraw organizes artwork inside an `art/` folder, so create it now (we'll add files in the next section):

```bash
mkdir art
```

---

## 2. Plan the Composition

The scene is a skyline advertisement: a dark canvas with a title caption near the top and stylized stars sprinkled across the sky. Each star shares the same geometry but varies in color, size, and position. To keep the code tidy, we'll create one star model (`art/star.js`) and invoke it multiple times.

Drop the star helper into `art/star.js`:

```js
// art/star.js
const PI = 3.141592653589793;
const palette = ["#facc15", "#4ade80", "#38bdf8", "#fb7185", "#f472b6"];

function pickColor(index) {
  return palette[index % palette.length];
}

function createStarPoints(cx, cy, radius) {
  const points = [];
  for (let idx = 0; idx < 5; idx++) {
    const tipAngle = (idx * 72 * PI) / 180;
    const innerAngle = ((idx + 0.5) * 72 * PI) / 180;
    points.push([cx + Math.cos(tipAngle) * radius, cy + Math.sin(tipAngle) * radius]);
    points.push([cx + Math.cos(innerAngle) * radius * 0.5, cy + Math.sin(innerAngle) * radius * 0.5]);
  }
  return points;
}

function buildStar(centerX = 0, centerY = 0, size = 3, paletteIndex = 0) {
  const cx = centerX;
  const cy = centerY;
  const radius = Math.max(size, 0.1);
  const colorIdx = Math.max(0, Math.floor(paletteIndex));

  return {
    type: "polygon",
    points: createStarPoints(cx, cy, radius),
    fill: pickColor(colorIdx),
    stroke: "#0f172a",
    width: 0.4
  };
}

return buildStar;
```

Then wire up the scene directly inside `art/eval.js`:

```js
// art/eval.js
const view = { left: 0, bottom: 0, right: 60, top: 40 };

const graphics = Array.from({ length: 5 }, (_, idx) => {
  const centerX = 8 + idx * 10;
  const centerY = idx % 2 === 0 ? 10 : 18;
  const size = 4 + idx * 0.5;
  return star(centerX, centerY, size, idx);
});

return {
  view,
  graphics
};
```

FuncDraw only needs `{ view, graphics }`, but you're free to include overlays, animation hooks (`valueHooks`), and additional metadata objects. The helper functions live alongside `star.js` and can be split into more files as the project grows.

---

## 3. Run FuncDraw Play

From the project root, run:

```bash
npm run play
```

FuncDraw Play watches the `art/` directory, recompiles whenever you save, and serves an interactive preview. You can toggle overlays, scrub through animation time (`t`), and inspect the raw scene data from the CLI by using dump mode:

```bash
npm run play -- --dump
```

Dump mode prints the JSON payload to stdout—handy for scripting or debugging without the browser.

---

## 4. Extend the Scene

Once the basic layout works, try the following enhancements:

1. **Parameterize colors or layout** – expose helper functions such as `buildRow(count, spacing)` so you can reuse the scene in other packages.
2. **Add FuncScript helpers** – drop a `.fs` file into `art/` and call it from JavaScript (and vice versa). All files in the folder share the same namespace.
3. **Reference libraries** – install another FuncDraw package and call it with `package("@scope/library-name")`. The return value is a plain FuncScript key/value collection, so use regular `.` access to reach nested helpers.
4. **Wire value hooks** – if your scene needs animation time, check `typeof t !== "undefined"` inside your JS. FuncDraw injects time as a variable when the preview animates.

Keep committing incremental changes so you can compare past scenes with new experiments. When you're ready to publish, treat the folder like any other npm package: run tests, bump the version, and share it via the registry of your choice.

Happy drawing!

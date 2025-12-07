# @funcdraw/play

Preview FuncDraw graphics locally by running a single command. The CLI loads FuncScript expressions from your project's `art/` directory, evaluates them through `@funcdraw/core`, and renders the result on an HTML canvas with a slim toolbar for stats and future controls.

## Installation

```bash
npm install @funcdraw/play --save-dev
```

Add a script to your project:

```json
{
  "scripts": {
    "play": "funcdraw-play"
  }
}
```

Place your FuncScript models inside an `art/` directory. Each `.fs` file is treated as a FuncScript expression and each `.js` file is treated as a JavaScript snippet. You don't need to wrap the snippet in `module.exports`—just write the code that should run and return the value for that package. Nested folders become nested keys in the resolver. For example:

```
art/
  scene.fs
  components/
    background.fs
    label.js
```

Run `npm run play` (or `pnpm play`, etc.) to open a browser window that renders your graphics. Edit files in the `art/` folder and the preview will automatically reload the canvas whenever the file changes. `.fs` files are evaluated as FuncScript, while `.js` files are automatically wrapped as ```javascript fenced blocks before evaluation.

## Scene resolution

`funcdraw-play` automatically builds a resolver from the `art/` folder at your project root. Each file becomes addressable by FuncScript `package` and `use` statements, and the preview server watches the entire directory tree for changes. When the `art/` folder is missing, the CLI falls back to a baked-in sample expression so you can confirm the tool is working.

By default the CLI renders `art/scene.fs` if it exists. If `scene.fs` is missing, the first available expression in the root of `art/` is used. Your scene should expose a `view` object with `{ left, bottom, right, top }` coordinates so the browser can preserve aspect ratios and project your world coordinates to fit the available canvas.

CLI options:

```
funcdraw-play --port 9000 --host 0.0.0.0 --no-open --debug --exp art.alternative
```

Use `funcdraw-play --help` to see the full list. Defaults listen on `127.0.0.1:5173` and automatically open your browser.

Use `--debug` when you want the server to print evaluated scene payloads (including warnings) directly to the terminal for troubleshooting.

Use `--test` to run FuncScript package tests (pairs like `scene.fs` and `scene.test.fs`) through the runtime `testPackage` helper. The CLI reports failing cases, sets a non-zero exit code when any test fails, and exits without starting the preview server.

Use `--dump` to skip server/browse launching altogether, evaluate the configured scene once (with SVG output), print the payload to the console, and exit. This is handy for CI pipelines or quick inspection without spinning up the preview UI.

Use `--trace` to emit a hierarchical FuncScript package trace (paths, snippets, results) using the runtime's package tracing hook. Pair it with `--dump` to see both payload and trace, run `--trace` alone for a trace-only evaluation, or pass `--trace step-into [filter]` to include every traced step (optionally filtered by substring). Add `--trace-file trace.json` to write the trace tree to disk as JSON.

Use `--exp <expression>` to evaluate a FuncScript snippet without editing `art/eval.*`. The snippet runs with `art` bound to the loaded package, so `--exp art.altScene --dump --t 1` dumps the `art/altScene.*` expression at `t = 1`—perfect for debugging alternate compositions.

## Time value hook & animation

FuncDraw Play automatically injects a `t` value hook into every scene. If your FuncScript references `t`, the browser HUD exposes play/pause and reset controls that stream incremental `t` values back to the server so your model can animate over time. When the scene never touches `t`, the UI hides the controls and FuncDraw evaluates your expression once, just like before.

When running in `--dump` mode you can seed the hooks manually: pass `--t 2.5` to set the initial time and `--canvas 800 600` to mimic a particular viewport. Add `--svg` if you still want SVG output in the dump payload.

## Canvas size hook

In addition to `t`, FuncDraw Play injects a `canvas` value hook. The hook exposes the actual pixel dimensions of the preview canvas: `canvas.size.width` and `canvas.size.height`. When your model reads `canvas`, the client automatically re-evaluates the scene whenever the browser window resizes so your geometry can react to the available space.

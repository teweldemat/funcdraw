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
    "play": "funcdraw-play",
    "share": "npx fd-share"
  }
}
```

Place your FuncScript models inside an `art/` directory. Each `.fs` file is treated as a FuncScript expression, and nested folders become nested keys in the resolver. For example:

```
art/
  view.fs
  graphics.fs
  components/
    background.fs
```

Run `npm run play` (or `pnpm play`, etc.) to open a browser window that renders your graphics. Edit files in the `art/` folder and the preview will automatically reload the canvas whenever the file changes.

## Scene resolution

`funcdraw-play` automatically builds a resolver from the `art/` folder at your project root. Each file becomes addressable by FuncScript `package` and `use` statements, and the preview server watches the entire directory tree for changes. When the `art/` folder is missing, the CLI falls back to a baked-in sample expression so you can confirm the tool is working.

By default the CLI evaluates the `art/` package root (i.e. the FuncScript package result). To render anything, the root value should be either:

- A scene object that exposes `view: { left, bottom, right, top }` and `graphics: [...]` (optionally `step`), or
- A primitive / list of primitives (no explicit `view`).

A simple authoring pattern is to define `art/view.fs` and `art/graphics.fs` so the root collection evaluates to `{ view, graphics }`.

CLI options:

```
funcdraw-play --port 9000 --host 0.0.0.0 --no-open --debug --exp art.alternative
```

Use `funcdraw-play --help` to see the full list. Defaults listen on `127.0.0.1:5173` and automatically open your browser.

Use `--debug` when you want the server to print evaluated scene payloads (including warnings) directly to the terminal for troubleshooting.

Use `--test` to run FuncScript package tests (pairs like `<name>.fs` and `<name>.test.fs`) through the runtime `testPackage` helper. The CLI reports failing cases, sets a non-zero exit code when any test fails, and exits without starting the preview server.

Use `--dump` to skip server/browse launching altogether, evaluate the configured scene once (with SVG output), print the payload to the console, and exit. This is handy for CI pipelines or quick inspection without spinning up the preview UI.

Use `--trace` to emit a hierarchical FuncScript package trace (paths, snippets, results) using the runtime's package tracing hook. Pair it with `--dump` to see both payload and trace, run `--trace` alone for a trace-only evaluation, or pass `--trace step-into [filter]` to include every traced step (optionally filtered by substring). Add `--trace-file trace.json` to write the trace tree to disk as JSON.

Use `--exp <expression>` to evaluate a FuncScript snippet with `art` bound to the loaded package (useful for dumping alternate compositions without changing the package root).

## Sharing

`fd-share` uploads a zip snapshot of your package (plus a browser bootstrap payload) to a FuncDraw share server and prints a playable link.

```bash
npx fd-share login
npx fd-share --name "my-first-scene"
```

Restrict access to specific Google account emails:

```bash
npx fd-share --restrict alice@gmail.com,bob@gmail.com
```

Share a specific expression (with `art` bound to the loaded package):

```bash
npx fd-share --exp "art.ui.badge"
```

You can still run the legacy CLI:

```bash
funcdraw-share --server http://localhost:8787
```

## Time (`t`) & animation

FuncDraw Play provides a `t` context value (seconds) to every scene. If your FuncScript reads `t`, the browser HUD exposes play/pause and reset controls that update `t` and re-evaluate the model over time. When the scene never touches `t`, the UI hides the controls and FuncDraw evaluates your expression once.

When running in `--dump` mode you can seed the context values manually: pass `--t 2.5` to set the initial time and `--canvas 800 600` to mimic a particular viewport. Add `--svg` if you still want SVG output in the dump payload, or `--svg output.svg` to write the SVG to disk.

## Canvas size (`canvas`)

In addition to `t`, FuncDraw Play provides a `canvas` context value with the actual pixel dimensions of the preview canvas: `canvas.size.width` and `canvas.size.height`. When your model reads `canvas`, the client automatically re-evaluates the scene whenever the browser window resizes so your geometry can react to the available space.

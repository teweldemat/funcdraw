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

Place your FuncScript models inside an `art/` directory. Each `.fs` file is treated as a FuncScript expression, and each `.js` file may export a string or `{ expression, language }`. Nested folders become nested keys in the resolver. For example:

```
art/
  scene.fs
  components/
    background.fs
    label.js
```

Run `npm run play` (or `pnpm play`, etc.) to open a browser window that renders your graphics. Edit files in the `art/` folder and the preview will automatically reload the canvas whenever the file changes.

## Scene resolution

`funcdraw-play` automatically builds a resolver from the `art/` folder at your project root. Each file becomes addressable by FuncScript `package` and `use` statements, and the preview server watches the entire directory tree for changes. When the `art/` folder is missing, the CLI falls back to a baked-in sample expression so you can confirm the tool is working.

By default the CLI renders `art/scene.fs` if it exists. If `scene.fs` is missing, the first available expression in the root of `art/` is used. Your scene should expose a `view` object with `{ left, bottom, right, top }` coordinates so the browser can preserve aspect ratios and project your world coordinates to fit the available canvas.

CLI options:

```
funcdraw-play --port 9000 --host 0.0.0.0 --no-open --debug
```

Use `funcdraw-play --help` to see the full list. Defaults listen on `127.0.0.1:5173` and automatically open your browser.

Use `--debug` when you want the server to print evaluated scene payloads (including warnings) directly to the terminal for troubleshooting.

Use `--dump` to skip server/browse launching altogether, evaluate the configured scene once (with SVG output), print the payload to the console, and exit. This is handy for CI pipelines or quick inspection without spinning up the preview UI.

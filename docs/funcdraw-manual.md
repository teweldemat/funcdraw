# FuncDraw manual

## Cheat sheet

- `npm run nplay` starts the FuncDraw.Net preview server (recommended).
- Use `--dump` to evaluate your composition once and print the resulting scene payload (headless; no port).
- Use `--test` to run the package’s FuncScript tests (note: `npm run test` does not run these tests).
- Optional: `npm run play` starts the parallel JS preview server (requires `@funcdraw/play`).

## Terminology

- **expression** – FuncScript code under the `art` folder (`.fs`/`.fx`). Name files without spaces, dots, or dashes.
- **collection** – folder containing one or more expressions or folders. If no child is named `eval`, every child expression is directly addressable via dot navigation.
- **module** – folder that contains an expression named `eval` (any supported extension). The folder exports only what `eval` returns; sibling expressions in that folder are implementation details and are not addressable directly.
  - To expose an internal expression, re-export it from `eval.fs`:
    ```funcscript
    // art/widgets/button/eval.fs
    { draw; hitTest; } // consumers can access `art.widgets.button.draw` and `.hitTest`
    ```
- **package** – the entire `art` tree for a project. Packages are Node-style (they live alongside `package.json`) and can be consumed from other FuncDraw packages with `package("<name>")`.
- **model** – an expression/module intended to render a graphical object. Anything that returns graphics (e.g., `cartoon/stickman/head.fs`) is a model.
- **component** – an expression/module used as a building block for larger models.

Example of loading a library package from FuncScript:
```funcscript
man:package("@funcdraw/testlib").cartoon.stickman.static;
```

## Documentation guidelines

Every model/component that gets its own `.doc.md` file should follow a predictable structure so readers immediately know what to expect:

- **File name** – mirror the expression path, e.g. `art/cartoon/stickman/head.doc.md` documents `head.fs`.
- **Overview** – first section that states what the model renders in plain graphical terms (shapes, palette roles, facings) and why/where it is used.
- **Construction Overview** – short, ordered list describing the major building steps (e.g. calls `skeleton.fs`, draws torso, invokes limb helpers). Mention any delegated helpers here.
- **Inputs** – use pseudo schema to describe every argument (no ad-hoc prose). Include units, coordinate frames, defaults, and what visual outcome each field controls.
- **Outputs** – explain the returned structure (graphics arrays, overlay helpers, metadata) so consumers know which pieces to render or inspect.

Keep these sections concise and focused on the model’s behaviour; avoid repeating general FuncDraw rules in every document.
Always title them exactly as `## Overview`, `## Construction Overview`, `## Inputs`, and `## Outputs` so every doc reads the same at a glance.

## FuncScript runtime
FuncDraw runs inside the FuncScript runtime. Think of the `art` folder as a FuncScript package that the resolver loads from disk (and from `node_modules` when you call `package("<name>")`).

Key points:
- **No imports/exports** – a FuncScript file is evaluated and its returned value is the “export.” Refer to sibling expressions by name; the resolver injects them automatically.
- **Modules vs. collections** – when a folder has `eval.fs`, only that return value is exported. Otherwise each file is reachable via dot navigation (`cartoon.stickman.head`).
- **Stateless by default** – expressions can be re-evaluated many times during a render; rely on function arguments instead of mutable globals.

### Best Practice Workflow
- Prepare a test composition first, starting with an empty output so you have a safe harness to grow into; when authoring a library, build that composition in a separate package that depends on the library so you exercise the consumer path.
- Plan the expressions, modules, and collections you will need before writing code; name files without spaces/dashes and decide up front which folders are collections vs. modules.
- Build incrementally: sketch the interface (inputs/outputs) for each expression, then fill in behaviour in small passes.
- As expressions grow with added detail, convert them into modules and break the work into smaller expressions; aim to keep individual expressions under ~200 lines (shorter is better). Name folders/files to hint at how the art is decomposed.
- For each expression, write a `.test.fs` (simple sanity for small pieces, richer validation for complex ones) and keep `npm run nplay -- --test` passing as you iterate; prefer fast, deterministic tests over visual checks. If you’re authoring a library, put the test composition in a separate package that depends on the library so you exercise the real consumer path.
- Assume inputs are already validated; keep functions pure (no module-level mutation) so reruns are stable. If an input should never be missing, prefer `error("expected ...")` over silent fallbacks.
- Once a model is complete, add it to the test composition and verify with `--dump`; use `--trace` (and `--trace step-into` when needed) to chase resolver/evaluation issues.
- Keep docs in sync: update the model’s `.doc.md` after interfaces change, and record construction steps and inputs/outputs concisely.
- When multiple helpers share behaviour, refactor to shared collections to avoid duplication; keep palettes and constants near their consumers unless reused broadly.

## Common mistakes (and how to avoid them)

- **Modules vs. collections** – if a folder contains `eval.*`, only what `eval` returns is exported. Sibling expressions are not addressable unless you re-export them from `eval.*`.
- **Boolean operators** – FuncScript uses `and`/`or`/`not` (not `&&`/`||`/`!`). Treat parse errors as real errors even if something renders.
- **Binding vs. equality** – `name: expr;` binds a value; `=`/`==` compare values. There is no assignment operator.
- **Interactive scenes must be stateful** – export a function like `(state) => { ...; eval { view; graphics; step; }; }`. The initial `state` is `null`.
- **`step(event)` return contract** – return `null` to ignore an event; otherwise return `{ state: <newState>; events: []; }`. Only return a step when something actually changed.
- **Event shapes in tests** – if your stepper reads `event.point.x/y`, your test event must include `point: { x; y; }` (no implicit defaults).
- **Work proportional to `t`** – for animations, keep per-frame work roughly constant; compute indices from `fd.valueHooks.t` instead of looping/recursing `t` times.

## FuncDraw.Net CLI (nplay)
Most packages include an `nplay` script that runs the .NET previewer (example: `dotnet run --project ../../FuncDraw.Net -- --root .`).

- `npm run nplay` starts the preview server.
- `npm run nplay -- --dump` evaluates once and prints the payload (headless; no port).
- `npm run nplay -- --test` runs the package’s FuncScript tests and exits (headless; no port).
- `npm run nplay -- --trace [step-into [filter]]` prints FuncScript trace output; add `--dump` when you also want the payload.

Notes:
- Server mode defaults to port `5177`. If it’s already in use, FuncDraw.Net falls back to an available port.
- `--dump` and trace-only `--trace` (without `--dump`) run headless and do not bind a port.
- `--svg` includes SVG output in the dumped JSON payload (flag only; no file argument).

Common flags:
- `--root <path>` project root containing `art/` (default: current directory).
- `--host <address>` interface to bind in server mode (default: `127.0.0.1`).
- `--port <number>` preferred port in server mode (default: `5177`).
- `--exp <expression>` evaluate a snippet with `art` bound to the loaded package.
- `--t/--time <seconds>` seed `fd.valueHooks.t`.
- `--state <json>` set the initial scene state before evaluation (dump/trace-only).
- `--event <json>` push a single event before evaluation (dump/trace-only).
- `--trace-file <path>` write trace entries to a JSON file.

### Tracing with `--trace`

- `npm run nplay -- --trace` runs a trace-only evaluation and prints trace entries.
- Add `--trace step-into [filter]` to log every traced step and optionally filter by substring (e.g., `--trace step-into palette`).
- Use `--dump --trace [--svg]` when you want both the payload and the trace.

### Working with `--test`

`npm run nplay -- --test` runs FuncScript test pairs from the loaded package and exits.

- **Ensure dependencies are installed** – tests execute against the current package; run `npm install` so packages referenced via `package("<name>")` resolve.
- **Limit scope with `--root`** – run from the package root or pass `--root` to point at the package you want to test.
- **Debug failures with `--dump --trace`** – combine with `--exp` to focus evaluation on a single expression while inspecting trace output.

## JavaScript preview server (optional)
FuncDraw also has a parallel JS preview server (`funcdraw-play`). Only add it when you want to run `npm run play`; FuncDraw.Net (`nplay`) does not require it.

To enable it in a package, add `@funcdraw/play` and a `play` script:
```json
{
  "scripts": {
    "play": "funcdraw-play"
  },
  "devDependencies": {
    "@funcdraw/play": "file:../../packages/funcdraw-play"
  }
}
```

## JavaScript expressions (optional)
FuncScript is the default, but JavaScript bindings remain available when needed. Keep JS files stateless, end them with a `return` of the value you want to export, and refer to siblings the same way you would from FuncScript (they are injected into scope). Avoid `require`/`module.exports`; just use `package("<name>")` for external packages and direct identifiers for local helpers. Use JS sparingly—prefer `.fs`/`.fx` for new work.

## Latest lessons (keep for next session)
- FuncDraw.Net must mirror the JS loader: default action is `interpret(loadPackage())`; when `--exp` is provided use `art:loadPackage()` then `interpret(evaluate(<expression>))`, allowing the expression to reference `art`.
- Package loading is lazy: the .NET `PackageLoader` now evaluates expressions on demand through a `KeyValueCollection` instead of concatenating a giant expression. Folders with an `eval` child are treated as modules and evaluated when accessed; nested eval modules now work (this was the bug that broke the .NET player).
- The package test runner also follows the JS build-expression path so `eval`/`eval.test` pairs execute correctly. Tests must return `eval [ { name, test, cases? } ]`—missing `test` will fail the run.
- Character sample: `art/cartoon/character/eval.fs` takes `(anchor, measurements, palette)`, merges `measurements` with `defaultMeasurements` (kept local to the folder), and returns body + two hands + two legs. Palette has `body` and `limb`. `examples/testcompose/art/characterTest.fs` calls `package("@funcdraw/testlib").cartoon.character([0,0], {}, palette)`.
- Useful comparisons when debugging: `npm run nplay -- --exp art.characterTest --dump --trace step-into` (dotnet) vs. `npm run play -- --exp art.characterTest --dump --trace step-into` (JS, if enabled).

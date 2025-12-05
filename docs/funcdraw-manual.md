# FuncDraw manual

## Terminology
**expression** FuncScript code under the `art` folder (`.fs`/`.fx`). Name files without spaces, dots, or dashes.
**collection** folder containing one or more expressions or folders. If no child is named `eval`, every child expression is directly addressable via dot navigation.
**module** folder that contains an expression named `eval` (any supported extension). The folder exports only what `eval` returns.
**package** the entire `art` tree for a project. Packages are Node-style (they live alongside `package.json`) and can be consumed from other FuncDraw packages with `package("<name>")`.
**model** an expression/module intended to render a graphical object. Anything that returns graphics (e.g., `cartoon/stickman/head.fs`) is a model.
**component** an expression/module used as a building block for larger models.

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
- For each expression, write a `.test.fs` (simple sanity for small pieces, richer validation for complex ones) and keep `npm run play -- --test` passing as you iterate; prefer fast, deterministic tests over visual checks. If you’re authoring a library, put the test composition in a separate package that depends on the library so you exercise the real consumer path.
- Normalize inputs early (`helpers.normalizeInput`/`resolveNumber`, etc.) and keep functions pure (no module-level mutation) so reruns are stable.
- Once a model is complete, add it to the test composition and verify with `--dump`; use `--trace` (and `--trace step-into` when needed) to chase resolver/evaluation issues.
- Keep docs in sync: update the model’s `.doc.md` after interfaces change, and record construction steps and inputs/outputs concisely.
- When multiple helpers share behaviour, refactor to shared collections to avoid duplication; keep palettes and constants near their consumers unless reused broadly.

## FuncDraw Play CLI
Run `npm run play -- [options]` from a FuncDraw package to start the preview server. Common flags:

- `--port, -p <number>` choose preferred HTTP port (default: auto-pick).
- `--host <address>` bind to a specific interface, e.g. `0.0.0.0` for LAN access.
- `--open` / `--no-open` toggle automatic browser launch (default: on).
- `--debug` print every evaluated scene payload to the terminal; helpful when inspecting warnings or raw output.
- `--test` run FuncScript package tests (pairs like `scene.fs` with `scene.test.fs`) and exit with non-zero status on failures; skips starting the preview server.
- `--dump` evaluate once, print the scene payload, and exit (no UI server).
- `--trace` print FuncScript package trace info; with `--dump` it includes the payload, without `--dump` it runs a trace-only evaluation and prints just the trace. Add `--trace step-into [filter]` to include every traced step (not just per-expression summaries) and optionally filter by substring.
- `--exp <expression>` temporarily evaluate a FuncScript snippet with `art` bound to the loaded package (e.g., `art.altScene`), handy for debugging alternates without touching `art/eval.*`. just `art` will evaluate the loaded package. If the package has `eval` at the root that will be evaluated as per the funscript package convension.
- `--svg` (dump mode only) also emit the rendered SVG payload when using `--dump`.
- `--t <seconds>` seed the timeline hook (`fd.valueHooks.t`) before evaluation.
- `--canvas <width> [height]` set the initial preview canvas size in pixels; omit height to keep the previous value.

All options can be combined. For example, `npm run play -- --dump --svg --trace --t 12.5` quickly inspects the scene at `t = 12.5s`, prints raw data and SVG, and includes the FuncScript trace without starting the dev server. Use `--test` alone when you just want to run the package’s `.test.fs` suites and exit.

### Tracing with `--trace`

Use `--trace` when you need to inspect how FuncScript resolves and evaluates your package without running the preview server.

- `npm run play -- --trace` runs a single trace-only evaluation, printing per-expression trace entries (path, optional source span, snippet, and a preview of the returned value or error) and then exiting.
- `npm run play -- --dump --trace [--svg]` prints both the scene payload and the trace, which is helpful in CI or when debugging headless renders.
- Add `--trace step-into` to log every traced step instead of just per-expression summaries; append a substring to filter noisy output (e.g., `--trace step-into palette`).
- Trace entries are emitted directly to the terminal via the runtime tracing hook, so they reflect the exact resolver path and values that were produced during evaluation.

### Working with `--test`

`funcdraw-play --test` runs FuncScript test pairs from the loaded package and exits. The tool prints a summary of scripts, suites, and case counts; a non-zero exit means at least one case failed. A few useful tips:

- **Ensure dependencies are installed** – tests execute against the current package. From the package root, run `npm install` first so `@funcdraw/core`, `@funcdraw/play`, and your local packages resolve.
- **Limit scope with the config resolver** – `funcdraw-play` loads the same resolver it uses for rendering. If you want to test a different package, run the CLI from that package root or point your config there.
- **Debug failures with `--debug`** – combine `--test --debug` to dump full test payloads and errors to the console. This is handy when assertions bubble FuncScript errors (`fsError` blocks) or you need to see intermediate values.
- **Focus on a single case** – narrow a failing suite by temporarily adding guard logic in the `.test.fs`/`.test.js` to return only the suite you’re debugging. Tests must return an array of suite objects; pruning to one suite is allowed.
- **Pairing rules** – FuncScript looks for `*.test.fs` alongside `*.fs` expressions. JS tests (`*.test.js`) can also be returned from `eval` of a module. Ensure each test file `return`s an array of suite objects: `{ name, cases, test }`.
- **Non-rendering environments** – `--test` never starts the preview server, so it’s safe in CI and headless environments.
- **Inspect warnings** – if tests pass but you suspect silent issues, run without `--test` and add `--debug` to inspect any warnings emitted during evaluation.

## Using JavaScript (optional)
FuncScript is the default, but JavaScript bindings remain available when needed. Keep JS files stateless, end them with a `return` of the value you want to export, and refer to siblings the same way you would from FuncScript (they are injected into scope). Avoid `require`/`module.exports`; just use `package("<name>")` for external packages and direct identifiers for local helpers. Use JS sparingly—prefer `.fs`/`.fx` for new work.

# FuncDraw manual

## Terminology
**expression** any funscript or javascript code inside the 'art' folder. They should be named with out spaces, dotes, dashes
**collection** a folder containing one ore more expressions or folders. All the expressions must not be named 'eval' other wise the folder will represent a module
**module** a folder containing an expression named 'eval' (ignoring the extension)
**package** is a collection of expressions, modules and collections that reside in 'art' folder. Packages can be designed with the purpose of providing re-usable components in other packages in which case we refer to them library packages. Packages are structured as node packages hence will require package.json at the root folder. To call a library package from expressions you call the `package` function with the package name only, then navigate its collections via `.`.  
Example:
```funcscript
man:package("@funcdraw/testlib").cartoon.stickman.static;
```
**model** is a loose term that is used to describe an expression or a module that is meant to model a graphical object. An expression, a module or a package can be models. When a file outputs tangible graphics (for example `examples/testlib/art/cartoon/stickman/head.js` or `hand.js`) refer to it as a model so readers understand it paints something concrete.
**component** is a term that is used to describe an expression or module that is meant to be used as component of larger models

## Documentation guidelines

Every model/component that gets its own `.doc.md` file should follow a predictable structure so readers immediately know what to expect:

- **File name** – mirror the expression path, e.g. `art/cartoon/stickman/head.doc.md` documents `head.js`.
- **Overview** – first section that states what the model renders in plain graphical terms (shapes, palette roles, facings) and why/where it is used.
- **Construction Overview** – short, ordered list describing the major building steps (e.g. calls `skeleton.js`, draws torso, invokes limb helpers). Mention any delegated helpers here.
- **Inputs** – use pseudo schema to describe every argument (no ad-hoc prose). Include units, coordinate frames, defaults, and what visual outcome each field controls.
- **Outputs** – explain the returned structure (graphics arrays, overlay helpers, metadata) so consumers know which pieces to render or inspect.

Keep these sections concise and focused on the model’s behaviour; avoid repeating general FuncDraw rules in every document.
Always title them exactly as `## Overview`, `## Construction Overview`, `## Inputs`, and `## Outputs` so every doc reads the same at a glance.

## FuncScript runtime
FuncDraw runs withing FuncScript runtime, a FuncDraw package is abstracted as FuncScript package that resolved using file system. Packages are loaded from node_modules.
The most important point to remember is:
- there is no require or export key word:
  * a FuncScript expression in a file is evaluted and its return play the 'export' of convensional node packagtes
  * expression are referred to by their name within a packge without explicitly importing or requiring them
  * package keyword is used to load a package from node_modules

## JavaScript expressions
JavaScript language is avilable through FuncScript language binding. When writing javascript expression care should be taken not to retain state.
- **Stateless execution** – a JS file can be re-evaluated at any time, often multiple times per render. Do not mutate module-level variables or cache mutable objects between runs. Prefer local variables or recreate values on demand so repeated evaluations yield identical results for the same inputs.
- **Referencing siblings and packages** – every expression/module in scope is exposed as a property when your JavaScript runs. Call other models or helpers directly (`return star(row, col)`), or reach into folders using dot notation when the folder is a collection (no `eval.js`) (`scene.background()`). If a folder defines `eval.js`, it exports only that module, so nested helpers or models such as `cartoon.stickman.static.head` are not surfaced unless you re-export them yourself. To reference another npm FuncDraw package, use the regular FuncScript helper: `const tree = package("@funcdraw/testlib").cartoon.tree;`.
- **Returning results** – finish the file with a `return` statement. For primitives, return the object or array representing the graphics payload. For reusable helpers, return a function or collection. 
- **No CommonJS boilerplate** – `require`, `module.exports`, and `export`/`import` are unnecessary because FuncDraw injects the available bindings through the provider scope. Simply reference functions by name and `return` the final value.

Example:

```js
const palette = ["#facc15", "#38bdf8", "#fb7185"];

function badge(centerX, centerY, size, index) {
  const radius = size ?? 4;
  return {
    type: "circle",
    center: [centerX ?? 0, centerY ?? 0],
    radius,
    fill: palette[index % palette.length],
    stroke: "#0f172a",
    width: 0.4
  };
}

return badge;
```

## FuncDraw Play CLI
Run `npm run play -- [options]` from a FuncDraw package to start the preview server. Common flags:

- `--port, -p <number>` choose preferred HTTP port (default: auto-pick).
- `--host <address>` bind to a specific interface, e.g. `0.0.0.0` for LAN access.
- `--open` / `--no-open` toggle automatic browser launch (default: on).
- `--debug` print every evaluated scene payload to the terminal; helpful when inspecting warnings or raw output.
- `--test` run FuncScript package tests (pairs like `scene.fs` with `scene.test.fs`) and exit with non-zero status on failures; skips starting the preview server.
- `--dump` evaluate once, print the scene payload, and exit (no UI server).
- `--exp <expression>` temporarily evaluate a FuncScript snippet with `art` bound to the loaded package (e.g., `art.altScene`), handy for debugging alternates without touching `art/eval.*`.
- `--svg` (dump mode only) also emit the rendered SVG payload when using `--dump`.
- `--t <seconds>` seed the timeline hook (`fd.valueHooks.t`) before evaluation.
- `--canvas <width> [height]` set the initial preview canvas size in pixels; omit height to keep the previous value.

All options can be combined. For example, `npm run play -- --dump --svg --t 12.5` quickly inspects the scene at `t = 12.5s` and prints both raw data and SVG without starting the dev server. Use `--test` alone when you just want to run the package’s `.test.fs` suites and exit.

### Working with `--test`

`funcdraw-play --test` runs FuncScript test pairs from the loaded package and exits. The tool prints a summary of scripts, suites, and case counts; a non-zero exit means at least one case failed. A few useful tips:

- **Ensure dependencies are installed** – tests execute against the current package. From the package root, run `npm install` first so `@funcdraw/core`, `@funcdraw/play`, and your local packages resolve.
- **Limit scope with the config resolver** – `funcdraw-play` loads the same resolver it uses for rendering. If you want to test a different package, run the CLI from that package root or point your config there.
- **Debug failures with `--debug`** – combine `--test --debug` to dump full test payloads and errors to the console. This is handy when assertions bubble FuncScript errors (`fsError` blocks) or you need to see intermediate values.
- **Focus on a single case** – narrow a failing suite by temporarily adding guard logic in the `.test.fs`/`.test.js` to return only the suite you’re debugging. Tests must return an array of suite objects; pruning to one suite is allowed.
- **Pairing rules** – FuncScript looks for `*.test.fs` alongside `*.fs` expressions. JS tests (`*.test.js`) can also be returned from `eval` of a module. Ensure each test file `return`s an array of suite objects: `{ name, cases, test }`.
- **Non-rendering environments** – `--test` never starts the preview server, so it’s safe in CI and headless environments.
- **Inspect warnings** – if tests pass but you suspect silent issues, run without `--test` and add `--debug` to inspect any warnings emitted during evaluation.

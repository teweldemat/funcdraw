# Import Test Example

This example installs `@funcdraw/testlib` from `examples/testlib` and calls its `square` expression via FuncScript `package()` so the main scene can reuse shared primitives. The package behaves like any other FuncDraw project: its art lives under `art/` and `funcdraw-play` evaluates `art/scene.fs`.

## Setup

```bash
cd examples/importtest
npm install
```

## Run

```bash
npm run play
```

The scene renders four squares: two created directly via `squareLib.square(...)` and another pair using a helper reference (`squareFn`) that points at the imported library's function (`package("@funcdraw/testlib").square`).

# @funcdraw/testlib

A minimal FuncDraw library that exposes reusable FuncScript helpers:

- `art/square.fs` – square primitive factory for simple geometry demos.
- `art/cartoon/stickman/` – the stick man model from the JavaScript example, now consumable via FuncScript (`package("@funcdraw/testlib").cartoon.stickman`).
- `art/cartoon/hWalker.js` – horizontal walker helper that slides a stickman configuration between two X coordinates (`package("@funcdraw/testlib").cartoon.hWalker`).
- `art/cartoon/house.js` – configurable house builder that accepts `{ type, position, width, doorOpenLevel }` (`package("@funcdraw/testlib").cartoon.house`).
- `art/cartoon/tree.js` – tree helper with multiple canopy styles and `{ type, position, height }` options (`package("@funcdraw/testlib").cartoon.tree`).
- `art/cartoon/cloud.js` – fluffy cloud made of overlapping circles with `{ position, width, lobes }` controls (`package("@funcdraw/testlib").cartoon.cloud`).
- `art/cartoon/sun.js` – radiant sun with customizable radius, ray count, and palette (`package("@funcdraw/testlib").cartoon.sun`).

Install it as a dependency of another FuncDraw package and call `package("@funcdraw/testlib")` to compose your scenes. This package is file-based only and ships the `art/` directory, so it works anywhere `@funcdraw/play` or `@funcdraw/core` expects a FuncScript resolver.

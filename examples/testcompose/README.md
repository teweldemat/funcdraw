# @funcdraw/testcompose

Composition that exercises the `cartoon/character` helper from `@funcdraw/testlib`.

- `art/characterTest.fs` draws a single character line at the origin with height 10 via `package("@funcdraw/testlib")`.
- `art/diagonal.fs` draws a simple diagonal from `[-10,-10]` to `[10,10]`.
- Use `npm run play` (Node player) or `npm run nplay` (FuncDraw.Net) from this folder to preview. Pass `--exp art.diagonal` or `--exp art.characterTest` to target a specific scene.

Run `npm install` before either player command.

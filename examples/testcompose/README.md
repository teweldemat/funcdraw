# @funcdraw/testcompose

Composition that exercises the `cartoon/character` helper from `@funcdraw/testlib`.

- `art/characterTest.fs` draws a single character at the origin with the default proportions via `package("@funcdraw/testlib")`.
- `art/bodybendtest.fs` animates the torso angle over time using `fd.valueHooks.t`.
- `art/neckbendtest.fs` animates the neck angle over time using `fd.valueHooks.t`.
- `art/clickToggle.fs` toggles a rectangle's fill color on pointer clicks using the stepper model.
- `art/buttonDemo.fs` highlights a rectangle on hover and increments a click counter inside the box.
- `art/testlibButtonDemo.fs` uses `package("@funcdraw/testlib").ui.button` to render a reusable button component and count clicks.
- `art/busModelTest.fs` exercises the `cartoon/transport.bus` model (door opening + driver behind the windshield).
- `art/diagonal.fs` draws a simple diagonal from `[-10,-10]` to `[10,10]`.
- `art/mathTest.fs` draws a line from `[0,0]` to `[10 * cos(pi/3), 10 * sin(pi/3)]` using the math helpers.
- Use `npm run play` (Node player) or `npm run nplay` (FuncDraw.Net) from this folder to preview. Pass `--exp art.<scene>` to target a specific scene.

Run `npm install` before either player command.

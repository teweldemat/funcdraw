# man-test

Renders the stock cartoon stickman from `@funcdraw/testlib` with all default measurements. It is handy for sanity-checking pose changes without any extra scenery.

## Run it

```bash
cd examples/man-test
npm install
npm run play
```

`funcdraw-play` will open the scene that simply instantiates `package("@funcdraw/testlib").cartoon.stickman.static()` and draws the returned `graphics` layers over a small baseline guide.

## steperMan tester

`art/steperManTester.js` exercises `package("@funcdraw/testlib").cartoon.stickman.steperMan` by animating both the left-foot-planted and right-foot-planted cases side by side, complete with arc guides, anchor markers, and progress readouts. Swap the export in `art/eval.js` to `return steperManTester;` (or temporarily rename the file to `scene.js`) when you want to preview the step harness with `npm run play`.

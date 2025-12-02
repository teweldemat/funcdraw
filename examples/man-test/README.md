# man-test

Renders the stock cartoon stickman from `@funcdraw/testlib` with all default measurements. It is handy for sanity-checking pose changes without any extra scenery.

## Run it

```bash
cd examples/man-test
npm install
npm run play
```

`funcdraw-play` will open the scene that simply instantiates `package("@funcdraw/testlib").cartoon.stickman.static()` and draws the returned `graphics` layers over a small baseline guide.

## steperManProfile tester

`art/steperManProfileTester.js` exercises `package("@funcdraw/testlib").cartoon.stickman.steperManProfile` by animating both the left-foot-planted and right-foot-planted cases side by side, complete with arc guides, anchor markers, and progress readouts. Swap the export in `art/eval.js` to `return steperManProfileTester;` (or temporarily rename the file to `scene.js`) when you want to preview the step harness with `npm run play`.

## steperManProfile walking test

`art/steperManProfileWalkingTest.js` pushes the same helper further by chaining individual steps into a continuous walk from the left edge of the viewport to the right. The scene shows the planted ankle, the moving foot’s arc targets, lane markers, a travel-progress readout, and swinging arms driven by the new `handSwing` settings so you can verify that repeated `steperManProfile` calls keep the character sliding smoothly across the stage. Point `art/eval.js` at `steperManProfileWalkingTest` when you want to watch the looping walk cycle inside `npm run play`.

## steperManProfile single map

`art/steperManProfileSingleMap.js` renders only the walker itself so you can inspect a single step without any guides or debug shapes. The expression clamps `progress` directly to `t`, so evaluating it at `t=0` shows the leg lift at the start of the step and `t=1` shows the end pose once the moving foot hits its target. To preview snapshots without editing `art/eval.js`, run the new CLI expression override:

```bash
cd examples/man-test
npm run play -- --exp art.steperManProfileSingleMap --dump --t 0
npm run play -- --exp art.steperManProfileSingleMap --dump --t 1
```

The `--exp` flag temporarily evaluates that FuncScript expression (with `art` bound to the package), which makes it easy to compare specific frames. Remove `--dump` if you want to keep the preview UI open and scrub through `t` manually.

# man-test

Renders the stock cartoon stickman from `@funcdraw/testlib` with all default measurements. It is handy for sanity-checking pose changes without any extra scenery.

## Run it

```bash
cd examples/man-test
npm install
npm run play
```

`funcdraw-play` will open the scene that simply instantiates `package("@funcdraw/testlib").cartoon.stickman()` and draws the returned `graphics` layers over a small baseline guide.

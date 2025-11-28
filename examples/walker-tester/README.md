# Walker Example

This scene renders a single stick figure that repeatedly walks from one side
of the view to the other using the shared `walker` helper from the cartoon
city example. The canvas is locked to `800 x 400`, so the only motion you see
is the walker itself.

## Getting started

```bash
cd examples/walker
npm install
npm run play
```

You can also dump frames at specific times while tuning the animation:

```bash
npm run play -- --dump --t 3.25
```

That command prints the raw stick-man skeleton and graphics payload, which is
useful for inspecting the limb targets that come out of the walker helper.

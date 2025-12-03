# the-man-with-two-houses

Standalone copy of the man-house scene. Run with:

```bash
npm install
npm run play -- --exp art
```

`art/scene1.js` is the same composition as `manHouse` (door animation plus zoom-walk hero). Adjust parameters via `art/constants.js` (or a FuncScript `constants.fs`) if desired (e.g., `manHouse` overrides for torso/legs).

The `art` entry point is `art/eval.js`, which chains the scenes together. Scrub the timeline in the UI or jump with `--t`:

- `t ~0–4.5`: scene1 (exit + zoom in at first house)
- `t ~4.5–7.5`: scene2 (walk across to second house)
- `t ~7.5–12`: scene3 (play scene1 in reverse at the second house: start zoomed-in/open, zoom out, close door)

To jump directly to a phase, pass `--t` when launching, e.g. `npm run play -- --exp art --t 5` to start near scene2.

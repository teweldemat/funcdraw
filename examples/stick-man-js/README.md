# Stick Man (JavaScript) Example

This example composes a simple stick figure out of reusable JavaScript helpers and models. The torso helper returns attachment points for limbs and a head, while the `hand`, `leg`, and `head` models draw articulated segments that plug into those locations. A top-level `stickman` model (see `art/stickman.js`) can position an entire character with a single call:

```js
const stickManBuilder = stickman;
const figure = stickManBuilder({
  position: [20, 6], // bottom-center of the torso
  measurements: {
    hands: { positiveBend: true }, // counter-clockwise elbows
    legs: { positiveBend: false }  // clockwise knees
  },
  palette: {
    torsoFill: "#111827",
    handStroke: "#f97316",
    overlayHand: "#fb7185"
  }
});
```

`measurements` can override limb lengths, head size, ground offset, or even the relative hand/leg target offsets. Setting `positiveBend` forces the limb to bend counter-clockwise relative to the attachment→target vector; setting it to `false` forces clockwise bending.

## Setup

```bash
cd examples/stick-man-js
npm install
```

## Run

```bash
npm run play
```

`funcdraw-play` evaluates `art/eval.js` and renders the resulting scene.

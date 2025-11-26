# Time Hook Line Example

This sample demonstrates how to animate a FuncScript scene using FuncDraw Play's built-in `t` value hook. The only scene file (`art/eval.fs`) draws a line from the origin to a point that rotates around the circle as `t` increases.

## Setup

```bash
cd examples/time-hook-line
npm install
```

## Run

```bash
npm run play
```

When the browser preview loads you'll see play/pause and reset controls in the toolbar. Press play and the line will sweep around the origin as FuncDraw continuously re-evaluates the scene with growing `t` values.

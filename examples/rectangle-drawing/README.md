# Rectangle Drawing Example

This example shows how to preview a simple FuncDraw scene using `@funcdraw/play`. The FuncScript expression lives inside the `art/` directory (`art/scene.fs`) and defines a `view` object with `{ left, bottom, right, top }` bounds to establish the world coordinate system.

## Setup

```bash
cd examples/rectangle-drawing
npm install
```

## Run

```bash
npm run play
```

This launches `funcdraw-play`, which reads the FuncScript scene from `art/scene.fs`, evaluates it, and opens a browser window that renders the rectangle on a canvas. Edit `art/scene.fs` and refresh the page to iterate on the design.

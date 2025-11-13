# FuncDraw Authoring Tutorial: Draw a Circle at the Origin

This tutorial walks through authoring the smallest possible FuncDraw project—one circle centered at `(0, 0)` with radius `10`—using nothing but npm packages. Everything happens in a brand-new folder, so you do **not** need to clone the FuncDraw repository.

## Prerequisites
- Node.js 20+ and npm 10+ installed on your machine.
- A terminal pointed at whatever directory you use for side projects (the examples below use `~/funcdraw-simple-circle`).

## 1. Initialize an empty npm workspace
Create a fresh folder, switch into it, and scaffold `package.json` with the default npm settings:

```bash
mkdir -p ~/funcdraw-simple-circle
cd ~/funcdraw-simple-circle
npm init -y
```

At this point you have a plain npm package with no FuncDraw-specific files.

## 2. Install FuncDraw dependencies and configure `@funcdraw/fd-play`
`fd-play` is the CLI that packages your FuncDraw expressions and serves them to a browser-based player. We’ll also pull in the published `@funcdraw/cat-cartoon` package so our circle scene can borrow the evergreen tree module.

Install the packages and add a `play` script to `package.json`:

```bash
npm install @funcdraw/cat-cartoon
npm install --save-dev @funcdraw/fd-play
npm pkg set scripts.play="fd-play --root ."
```

After those commands, your `package.json` should look similar to this (npm may add extra metadata such as `description` or `keywords`, which is fine):

```json
{
  "name": "funcdraw-simple-circle",
  "version": "1.0.0",
  "dependencies": {
    "@funcdraw/cat-cartoon": "^0.1.0"
  },
  "scripts": {
    "play": "fd-play --root ."
  },
  "devDependencies": {
    "@funcdraw/fd-play": "^0.1.0"
  }
}
```

## 3. Author the FuncDraw expressions
All FuncDraw workspaces need at least one graphics expression (`main.fs`). Optionally, you can add `view.fs` for camera bounds and `funcdraw.json` for metadata.

### `main.fs` — draw the circle
Create `main.fs` in the project root with the following content:

```fs
{
  cartoon: import('@funcdraw/cat-cartoon');

  circle: {
    type: 'circle';
    data: {
      center: [0, 0];
      radius: 10;
      fill: '#0ea5e9';
      stroke: '#0f172a';
      width: 0.4;
    };
  };

  evergreen: cartoon.landscape.tree('evergreen', [0, -6], 8).graphics;

  eval [evergreen, circle];
}
```

The imported tree module contributes layered foliage, while the local circle still anchors the composition at the origin.

### `view.fs` — set the camera
This keeps the circle framed nicely. Create `view.fs` next to `main.fs`:

```fs
{
  eval {
    minX: -20;
    minY: -20;
    maxX: 20;
    maxY: 20;
  };
}
```

### `funcdraw.json` — optional metadata
Drop in a simple manifest so the player shows a friendly title:

```json
{
  "title": "Simple Origin Circle",
  "description": "Example workspace that draws one circle centered at (0, 0).",
  "persistState": false
}
```

Your folder is now structured like this:

```
funcdraw-simple-circle
├─ funcdraw.json
├─ main.fs
├─ package-lock.json
├─ package.json
└─ view.fs
```

## 4. Launch the FuncDraw Player
Run the play script you added earlier:

```bash
npm run play
```

`fd-play` bundles every `.fs` (or `.js`) expression under the current folder, starts a dev server on port `4123`, and opens your default browser to the FuncDraw Player. The player evaluates `main.fs` with the framing from `view.fs`, so you will see the teal circle framed by the imported evergreen tree. Leave the server running: each time you edit `main.fs` or `view.fs`, the browser automatically reloads, giving you instant feedback as you iterate on shapes, colors, or animation parameters.

That’s all it takes to author and preview FuncDraw scenes using standard npm tooling—no repo clone required.

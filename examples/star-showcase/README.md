# Star Showcase Example

This project demonstrates how to build a complete FuncDraw scene purely in FuncScript. The `art/scene.fs` module defines the view, palette, pseudo-random helpers, and star polygon generator without falling back to JavaScript or external tooling.

## Usage

```bash
cd examples/star-showcase
npm install
npm run play
```

Each time you reload the preview, the expression is re-evaluated entirely in FuncScript, so you can tweak the helper functions in `art/scene.fs` (palette, seed list, shape logic, etc.) to explore different constellations or layout rules.

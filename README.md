# FuncDraw

FuncDraw is a vector graphics authoring toolkit that treats every shape, color, and animation cue as the result of a function. Instead of pushing pixels around, you define small reusable functions and compose them to generate complete scenes. The runtime can evaluate those definitions from JavaScript or FuncScript, a lightweight functional DSL designed for deterministic rendering.

FuncScript documentation & player: https://teweldemat.github.io/funcscript/player/

## Project Pieces
- Core runtime – powers both the player and CLI experiences by turning expression trees into typed values.
- Command line tools (`@tewelde/fd-cli`) – evaluate expressions against a workspace on disk and emit JSON, SVG, or PNG snapshots.
- FuncDraw Player – available on the FuncScript documentation site and designed for interactive authoring in the browser.

## Language Support
- **JavaScript** for direct embedding in Node.js and front-end projects.
- **FuncScript** for pure functional authoring with predictable evaluation semantics.

## Status
Early prototype. APIs and file layouts may change quickly while the author experiments with better ergonomics for functional graphics workflows.

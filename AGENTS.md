# FuncDraw: FuncScript-based Vector Graphics Authoring Tool

This repository contains the core framework for FuncDraw.

## Focus Areas
- **packages/funcdraw-core** — Core FuncDraw engine.
- **packages/funcdraw-play** — Web-based renderer implementation.

## Repository Layout Notes
- **funcscript/** — FuncScript repository included as a submodule.
- **examples/** — Art projects and supporting libraries used to stress-test capabilities. A curated set will be included in releases.

## Quick Start Reading List
- **FuncScript references:**  
  `funcscript/docs/index.md`,  
  `funcscript/docs/examples.md`,  
  `funcscript/docs/reference/built-in-symbols.md`
- **FuncScript developer guides:**  
  `funcscript/docs/developers/test-framework.md`,  
  `funcscript/docs/developers/fs-package.md`
- **FuncDraw manual:**  
  `docs/funcdraw-manual.md`

## No Defensive Code in This Round
During this phase of framework development, do **not** write defensive code. We want quirks and bugs to surface.  
Because we control the entire stack—from the FuncScript runtime to the example art projects—we know what to expect at each stage.

Avoid fallbacks and null-coalescing for situations that should never occur. If a function is guaranteed to exist or a value is guaranteed to be provided, rely on that guarantee.  
Where necessary,  evaluate to `error("expected X and Y")` to signal incorrect usage rather than silently masking issues.
# @funcdraw/testlib

Helper library used by the sample compositions in `examples/`. Everything lives under `art/` so it can be pulled in with `package("@funcdraw/testlib")`.

- `cartoon/character` — module exporting a simple stick figure skeleton (body, limbs, and head) from an anchor and a small measurement set.
  - `defaultMeasurements` and `paletteDefaults` sit beside it for convenience when you want quick defaults.
- `ui/button` — reusable button model that returns `{ graphics; step; }` for hover + click handling.

Run `npm install` followed by `npm run play` to use the Node preview, or `npm run nplay` to start the FuncDraw.Net player from this folder. For now, we are using the .NET version of FuncDraw across all packages.

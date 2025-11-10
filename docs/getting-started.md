# Getting Started

## 1. Launch the hosted FuncDraw Player
- Open **https://teweldemat.github.io/funcscript/player/** to use the official hosted build.
- Working offline? Download the site from the FuncScript documentation portal and open `player/index.html` locally to get the same experience.

## 2. Pick an example workspace
- The player ships with curated example workspaces. Use the file tree on the left side of the UI to load one, e.g. **Bicycle City** (`examples/bicycle_city/main.fs`).
- Each `.fs` file is a FuncScript expression. Clicking a file tab opens its source in the editor panes.

## 3. Fiddle with the code
- Modify expressions directly in the browser: change constants, tweak helper functions, or add new primitives. The canvas re-renders automatically whenever the main `graphics` expression returns a new list of primitives.
- Use the built-in [Reference] button to recall primitive shapes, or keep the [Primitives Reference](primitives-reference.md) page open for quick field definitions.

## 4. Save (or export) your workspace
- Press **Download Workspace** in the player toolbar to export every edited `.fs` file as a `.fdmodel` archive (a zip that mirrors the folder hierarchy). Import that archive back into the player or point `fd-cli` at it to render on the command line.
- Alternatively, share the `.fdmodel` archive with teammates so they can load it directly in the hosted player.

With the hosted player plus the CLI (`fd-cli --root ./workspace --expression graphics/main`) you can iterate on FuncDraw scenes completely in the open.

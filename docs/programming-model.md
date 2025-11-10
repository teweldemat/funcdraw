# Programming Model

A FuncDraw project is just a collection of pure expressions that describe what to render and how to frame it. Understanding how the player breaks those expressions into **main**, **view**, **collections**, and **modules** makes it easy to reuse components or render the same model from the CLI.

## Model anatomy
- **Main expression** – the entry point whose `eval` block emits the list (or lists) of primitives you want on screen. The player evaluates this tab every time you edit.
- **View expression** – an optional tab whose `eval` block emits `{ minX, minY, maxX, maxY }`, giving the renderer deterministic camera bounds. When it’s missing, the CLI falls back to a neutral framing.
- **Collections** – folders that gather expressions, other collections, or modules into a single namespace. Everything inside can be referenced with dot syntax (`collections.hero`, `collections.metrics.cards`, etc.), making it easy to wire helpers together.
- **Modules** – folders that act like callable helpers. Each module exposes a single expression named `eval`; referencing the module runs `eval`, which can use any private expressions in that folder before yielding a value.


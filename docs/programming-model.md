# Programming Model

A FuncDraw project consists of pure expressions that define what to render and how to frame it. Understanding how the player organizes these expressions into **main**, **view**, **collections**, and **modules** makes it easy to reuse components or render the same model from the CLI.

## Model anatomy
- **Main expression** – The entry point whose `eval` block emits the list (or lists) of primitives to display on screen. The player evaluates this tab every time you edit.
- **View expression** – An optional tab whose `eval` block emits `{ left, bottom, right, top }`, specifying deterministic camera bounds for the renderer. If it’s missing, the CLI defaults to a neutral framing.
- **Collections** – Folders that organize expressions, other collections, or modules into a single namespace. Everything inside can be referenced using dot syntax (`collections.hero`, `collections.metrics.cards`, etc.), making it easy to compose and reuse helpers.
- **Modules** – Folders that act like callable helpers. Each module exposes a single expression named `eval`; referencing the module runs that `eval`, which can use any private expressions inside the same folder before yielding a value.

## Collections vs. modules in practice

- When you reference a folder like `scene.scene1.expressionText`, you’re traversing a collection. Nothing in the folder runs automatically—you explicitly call the expression you need.

- When a folder contains an `eval` expression (for example, `scene/scene1/eval.fs`), the folder becomes a module. Referencing `scene.scene1` executes `eval` and returns whatever object it emits. This is ideal for bundling related data (like a title, code sample, and preview metadata) without forcing the caller to know every file name.  
  If the folder also contains an expression such as `scene/scene1/title.fs`, you cannot call it externally as `scene.scene1.title`. Instead, `title` is only visible within `scene/scene1/eval.fs` or other expressions inside the same `scene/scene1` folder.

## Example folder structure containg collections, expressions and modules

├─ main.fs                      # Main expression
├─ view.fs                      # View expression (optional)
├─ scene/                       # COLLECTION
│  ├─ hero/                     # MODULE (has eval.fs)
│  │  ├─ eval.fs                # public entry for hero
│  │  ├─ title.fs               # private to hero/*
│  │  └─ code.fs                # private to hero/*
│  ├─ metrics/                  # COLLECTION
│  │  ├─ cards/                 # MODULE
│  │  │  ├─ eval.fs
│  │  │  └─ card.fs            # private to metrics/cards/*
│  │  └─ table.fs               # plain expression inside collection
│  └─ scene1.fs                 # plain expression inside collection
├─ components/                  # COLLECTION
│  └─ button/                   # MODULE
│     ├─ eval.fs
│     ├─ label.fs              # private to components/button/*
│     └─ icon.fs               # private to components/button/*
└─ utils/                       # COLLECTION
   ├─ colors.fs                 # plain expression
   └─ layout/                   # COLLECTION
      ├─ grid.fs
      └─ clamp.fs

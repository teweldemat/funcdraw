# Programming Model

FuncDraw treats every file on disk as a node inside a functional evaluation graph. Understanding three core building blocks—expressions, collections, and modules—makes it easier to reason about what the player and CLI are doing.

## Expressions (`*.fs`)
- Use [FuncScript](https://teweldemat.github.io/funcscript/) syntax inside `.fs` files. Each file must evaluate to a value; most scene files return either a list of primitives or a helper object with fields.
- The player loads expressions as tabs. The active `graphics` tab must eventually return a list (or list of lists) of primitives, while the optional `view` tab must return `{ minX, minY, maxX, maxY }`.
- Example (see the **Plotter** workspace inside the player):

```funcscript
{
  let samples = range(-30, 30, 0.5) map (x) => {
    let y = sin(x / 4) * 10;
    return { type: 'line'; data: { from: [x, 0]; to: [x, y]; stroke: '#38bdf8' } };
  };
  return samples;
}
```

## Collections (folders)
- Physical folders map to collection nodes inside the resolver graph that `funcdraw/src/index.js` builds via `CollectionGraph`. Every folder can contain other folders (`kind: 'folder'`) and expression files; paths such as `graphics/plotter/main` are just folder segments joined with `/`.
- The CLI lists and evaluates expressions using these folder paths (`fd-cli/src/index.js` expects `graphics/main`, `view/default`, etc.). Keeping folders small and focused makes it easy to evaluate subsets in isolation.
- Use folders to model logical groupings: e.g. the **Bicycle City** workspace keeps helpers under `lib/` while its entry points remain near the top level (`main.fs`, `view.fs`).

## Modules (folders that `return`)
- When a folder needs to behave like a value, add a `return.fs` file in that folder. The file should `return` a record built from sibling expressions, effectively turning the folder into a module-like namespace.
- Example (from the **Bicycle City** workspace):

```funcscript
{
  return {
    wheel: wheel,
    gear: gear,
    tree: tree,
    cloud: cloud,
    sun: sun,
    bird: bird,
    house: house,
    stick_man: stick_man
  };
}
```

  Importing `lib` elsewhere yields an object with those fields, so `lib.wheel(...)` just works without bespoke wiring.
- This pattern keeps helpers colocated while letting higher-level expressions (`model.fs`, `main.fs`) depend on a single module reference instead of chasing multiple relative paths.

Armed with these three concepts you can navigate any FuncDraw workspace: folders define the namespace, `.fs` files define pure values, and `return.fs` bridges the two when you need module-style composition.

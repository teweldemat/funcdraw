# Import Test JS Example

JavaScript version of the FuncScript import demo. This package depends on `@funcdraw/testlib-js`, loads it through `package("@funcdraw/testlib-js")`, and renders a handful of squares using the shared helper.

## Setup

```bash
cd examples/importtest-js
npm install
```

## Run

```bash
npm run play
```

The preview should show the same grid of squares as the FuncScript example, but all geometry is orchestrated from `art/scene.js`.

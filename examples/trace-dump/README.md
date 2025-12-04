# Trace Dump Example

This example runs `funcdraw-play` in dump mode with FuncScript package tracing enabled so you can see which expressions executed. The scene evaluates `art/eval.fs`, which pulls in `helpers/z.fs` and concatenates the string `"this is a "` with the helper value `"test"` so the trace shows both the helper and the main expression.

## Setup

```bash
cd examples/trace-dump
npm install
```

## Run

```bash
npm run play -- --dump --trace step-into
```

The terminal prints the evaluated value plus trace entries (paths, snippets, and results). Feel free to edit `art/eval.fs` and rerun to watch the trace change.

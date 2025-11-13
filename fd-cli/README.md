# fd-cli

Command line interface for running FuncDraw workspaces directly from filesystem folders that contain `.fs` expressions. The CLI walks folders, exposes them through FuncDraw's resolver, and can evaluate any expression tree.

## Installation

```bash
npm install -g @tewelde/fd-cli
```

Once installed globally you can invoke `fd-cli` from any FuncDraw workspace folder.

## Usage

```bash
fd-cli <package-folder> --expression <name> [options]
fd-cli <package-folder> <name> [view-name]
```

Arguments:

- `<package-folder>`: folder that contains `package.json` and `funcdraw.json` (defaults to `cwd`).
- `<name>`: FuncDraw module or expression name; do not pass file paths such as `main.fs`.

Key options:

- `-e, --expression <name>`: expression name using collection segments (e.g. `graphics/main`).
- `--view <name>`: optional view expression name; falls back to the built-in default viewport when omitted.
- `-f, --format <raw|svg|png>`: choose between JSON output, SVG markup, or PNG bitmap rendering.
- `-o, --out <file>`: write SVG/PNG output to a file (PNG uses `./fd-output.png` when omitted).
- `--width`, `--height`, `--padding`: configure render dimensions.
- `--time`, `--time-name`: control the injected FuncDraw time variable (seconds).
- `--list`: print all discovered expressions.

SVG output is printed to stdout when `--out` is omitted, while PNG output always writes to a file.

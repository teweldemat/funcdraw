# FuncDraw Share Server

Python API server that stores uploaded FuncDraw packages and serves a playable link.

## Quickstart

From `share-server/`:

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --reload --port 8787
```

Then upload from a package folder (with `@funcdraw/play` installed):

```bash
npm run share
```

Share a specific expression (with `art` bound to the loaded package):

```bash
npm run share -- --exp "art.ui.badge"
```

## Configuration

Environment variables:

- `FUNCDRAW_SHARE_DATA_DIR` (optional): storage directory (default: `./data`)
- `FUNCDRAW_BASE_URL` (optional): public base URL used when constructing links (default: inferred from request)
- `FUNCDRAW_RUNTIME_JS_PATH` (optional): serve a prebuilt runtime JS bundle from this path; otherwise the server tries to bundle it using Node + esbuild from this repo
- `FUNCDRAW_FONT_PATH` (optional): path to `Inter-Regular.ttf` (default: uses `../funcdraw.js/funcdraw-core/assets/fonts/Inter-Regular.ttf`)

Google auth (required only for restricted models):

- `FUNCDRAW_GOOGLE_CLIENT_ID`
- `FUNCDRAW_GOOGLE_CLIENT_SECRET`
- `FUNCDRAW_SESSION_SECRET`

Redirect URI to register in Google Cloud Console:

- `<baseUrl>/auth/google/callback`

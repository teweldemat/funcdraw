# FuncDraw Share Server + `npm run share` (Technical Spec)

## Goal

Provide a simple “share” workflow for a FuncDraw art package:

1. `npm run share` bundles the current package into a single compressed artifact.
2. The artifact is uploaded to a local/hosted Python API server.
3. The command prints a **play link** that renders the uploaded model at `GET /play/<modelId>`.

This spec intentionally favors simplicity and local-first development; hardening (auth, quotas, multi-tenant) can be layered on later.

---

## Concepts

### Model
An uploaded snapshot of a package, identified by a server-generated GUID (`modelId`).

### Model ID
A GUID string (UUIDv4) used in storage paths and in URLs:

- Storage: `data/<modelId>/...`
- Play URL: `/play/<modelId>`

### Artifact
A **zip** archive containing the sharable package contents.

---

## Server Overview

### Technology
- Python 3.11+
- Web framework: FastAPI (recommended) or Flask (acceptable)
- Uvicorn for dev/prod serving (if FastAPI)

### Directory Layout (on server)

```
share-server/
  app.py                  # API + play route
  data/
    <modelId>/
      package.zip          # uploaded archive
      meta.json            # server-generated + client-supplied metadata
```

The server must create `data/` automatically if missing.

---

## Data Model

### `meta.json` schema (v1)

`meta.json` is stored alongside the uploaded archive to support listing/debugging and future features.

```jsonc
{
  "schemaVersion": 1,
  "modelId": "uuid-v4",
  "createdAt": "2025-01-01T12:34:56.789Z",

  "package": {
    "name": "@scope/my-art-package",
    "version": "1.2.3"
  },

  "artifact": {
    "filename": "package.zip",
    "contentType": "application/zip",
    "bytes": 123456,
    "sha256": "hex-string"
  },

  "client": {
    "tool": "funcdraw-share",
    "toolVersion": "0.1.0",
    "node": "v20.10.0",
    "platform": "darwin",
    "cwdBasename": "my-art-package"
  },

  "source": {
    "git": {
      "commit": "optional",
      "branch": "optional",
      "dirty": true
    }
  }
}
```

Notes:
- Only `schemaVersion`, `modelId`, `createdAt`, and `artifact` are strictly required by the server.
- Client fields are best-effort and must not be trusted for security decisions.

---

## HTTP API

All API endpoints are versioned under `/api`.

### Authentication + Access Control (v1.1)

The server supports **optional, per-model access restriction** based on an **allowlist of Google account emails**. When enabled for a model, users must sign in with Google and their verified email must be in the allowlist to view `/play/<modelId>` or download artifacts/metadata for that model.

This is designed to be driven by the client flag `--restrict <gmail accounts>` (see `npm run share` section).

#### Access rules
- If a model is **public** (default): no authentication required.
- If a model is **restricted**:
  - Unauthenticated users are redirected to login.
  - Authenticated users whose email is not in the allowlist receive `403`.
  - Authenticated users whose email is in the allowlist can access the model.

#### Allowed email format
For v1.1, the allowlist accepts **Google account emails**. The flag name says “gmail accounts”, but the server should accept:
- `...@gmail.com`
- `...@googlemail.com`
- Google Workspace domains (optional but recommended), as long as Google returns `email_verified=true`.

The server must enforce `email_verified=true` from Google before granting access.

### `POST /api/models`
Upload a zipped package and create a model.

**Request**
- `Content-Type: multipart/form-data`
- Form fields:
  - `file` (required): the zip archive
  - `meta` (optional): JSON string with client-supplied metadata (merged into `meta.json` under `client`/`package`/`source`)
  - `bootstrap` (recommended for v1, required for `/play/<modelId>` in current implementation): JSON string with the FuncDraw browser bootstrap payload (snapshot) used by the web player
  - `restrict` (optional): comma-separated list of allowed emails; if present and non-empty, the model becomes restricted

**Response (201)**
```jsonc
{
  "modelId": "uuid-v4",
  "playUrl": "http://<host>/play/<modelId>",
  "downloadUrl": "http://<host>/api/models/<modelId>/package.zip"
}
```

**Errors**
- `400`: missing file, invalid multipart
- `413`: payload too large (if server enforces)

### `GET /api/models/<modelId>/meta.json`
Return the stored metadata for the model.

**Response (200)**: JSON from `data/<modelId>/meta.json`  
**Errors**: `404` if model not found

### `GET /api/models/<modelId>/package.zip`
Download the uploaded zip archive.

**Headers**
- `Content-Type: application/zip`
- `Content-Disposition: attachment; filename="package.zip"`

**Errors**: `404` if model not found

### `GET /api/health`
Health check.

**Response (200)**
```json
{ "ok": true }
```

---

## Play Route

### `GET /play/<modelId>`
Serve a playable experience for a model.

**Behavior**
- Returns an HTML document that loads a FuncDraw player frontend.
- The player fetches a FuncDraw **bootstrap snapshot** for the model (server-stored) and evaluates it in the browser runtime.
- If the model is restricted, access is gated by Google authentication (see Auth section).

**Implementation options**
1. **Server-served static player**: bundle a prebuilt web player (e.g. from `funcdraw.js/funcdraw-play`) and have Python serve it.
2. **Client-only embed**: HTML includes minimal JS that redirects/embeds a separately hosted player with query params.

This spec assumes option (1) for best “single link” sharing.

**Errors**
- `404` if model not found (recommended), or show a “not found” UI.
- `403` (or an HTML “Access denied” page) if authenticated but not allowed.

---

## `npm run share` (Client Workflow)

### Intended usage
Run in the root of an art package (folder containing a `package.json` that defines `name`/`version` and contains the FuncDraw art root).

### Steps
1. Determine server base URL (default: `http://localhost:8787`).
2. Create a zip archive from the package directory.
3. Build a FuncDraw browser bootstrap snapshot from the package (including referenced `package("...")` dependencies).
4. POST the zip + bootstrap to `POST /api/models`.
5. Print the returned `playUrl` to stdout.

### Zipping rules (v1)
Include:
- `package.json`
- the art root folder(s) required to evaluate and play the model

Exclude by default:
- `node_modules/`
- `.git/`
- `dist/`, `build/`, `.next/`, `.turbo/` (if present)
- OS/editor junk (`.DS_Store`, etc.)

If a package needs custom include/exclude behavior, allow override via config file later (out of scope for v1).

### CLI contract
- Exit code `0` on success and print only the play URL (optionally prefixed by a short label).
- Non-zero exit code on failure with a clear error message.

### Configuration
Environment variables (v1):
- `FUNCDRAW_SHARE_SERVER`: base URL, e.g. `https://share.funcdraw.io`

CLI flags (optional for v1, recommended for v2):
- `--server <url>`
- `--restrict <emails>`: comma-separated list (or repeatable flag) of allowed Google emails; enables restricted access for the uploaded model
- `--exp <expression>`: FuncScript expression to evaluate as the shared entrypoint; `art` is bound to the uploaded package

Examples:
- `npm run share -- --restrict alice@gmail.com`
- `npm run share -- --restrict alice@gmail.com,bob@gmail.com`
- `npm run share -- --exp "art.ui.badge"`

---

## Security + Operational Notes (v1)

- **No auth** by default (local dev). Add auth tokens later.
- Enforce maximum upload size (recommended default: 50–200MB).
- Store uploads as opaque blobs; do not execute untrusted code on the server.
- If the play route needs to evaluate content, do so strictly in the browser sandbox.
- Enable CORS for `GET` of artifacts/metadata if the player runs on a different origin.

---

## Google Authentication (Restricted Models) (v1.1)

### Overview

Restricted models require a user to authenticate with Google (OAuth 2.0 / OpenID Connect). The server uses the returned ID token to obtain a verified email address and stores an authenticated session in an HTTP-only cookie.

### Server configuration

Environment variables:
- `FUNCDRAW_GOOGLE_CLIENT_ID` (required for restricted models)
- `FUNCDRAW_GOOGLE_CLIENT_SECRET` (required for restricted models)
- `FUNCDRAW_BASE_URL` (recommended): e.g. `http://localhost:8787` or `https://share.funcdraw.io`
- `FUNCDRAW_SESSION_SECRET` (required): used to sign/encrypt session cookies

Redirect URI (registered in Google Cloud Console):
- `<baseUrl>/auth/google/callback`

### Auth endpoints (server)

These endpoints are not versioned (browser-facing):

- `GET /auth/google/login?next=<url>`
  - Redirects to Google consent screen
  - `next` is a relative URL to return to after login (default `/`)

- `GET /auth/google/callback`
  - Exchanges auth code for tokens
  - Validates ID token, requiring:
    - `aud` matches `FUNCDRAW_GOOGLE_CLIENT_ID`
    - `iss` is Google issuer
    - `email_verified == true`
  - Creates a session cookie, then redirects to `next`

- `POST /auth/logout`
  - Clears the session cookie

Optional helper:
- `GET /api/me`
  - Returns current authenticated user info (e.g. `{ "email": "..." }`) for UI display/debugging

### Session/cookie requirements
- Cookie is `HttpOnly`, `SameSite=Lax`
- Use `Secure` when served over HTTPS
- Session contains at minimum: `email`, `iat` (issued-at), and optionally `name/picture`

### Authorization checks

For restricted models, the following routes MUST require authorization:
- `GET /play/<modelId>`
- `GET /api/models/<modelId>/package.zip`
- `GET /api/models/<modelId>/meta.json`

### Storage of restrictions

When restriction is enabled, store it in `meta.json`:

```jsonc
{
  "access": {
    "mode": "google-allowlist",
    "allowedEmails": ["alice@gmail.com", "bob@gmail.com"]
  }
}
```

The server must treat this allowlist as authoritative for gating access to that model.

---

## Acceptance Criteria (v1)

- Uploading a zip returns a `modelId` and a working `playUrl`.
- `GET /api/models/<modelId>/package.zip` returns the exact bytes uploaded.
- `GET /api/models/<modelId>/meta.json` returns metadata including `sha256` and `bytes`.
- Visiting `/play/<modelId>` loads a player UI that attempts to render the uploaded model.

## Acceptance Criteria (v1.1: Restricted Share)

- Uploading with `restrict` creates `meta.json.access` with the allowlist.
- Visiting `/play/<modelId>` for a restricted model redirects unauthenticated users to Google login and returns to the play page after login.
- Authenticated users not in the allowlist receive `403` (or an “Access denied” page).
- Authenticated allowed users can play and download artifacts for the restricted model.

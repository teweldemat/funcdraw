from __future__ import annotations

import hashlib
import json
import os
import secrets
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

import requests
from fastapi import FastAPI, File, Form, HTTPException, Request, UploadFile
from fastapi.responses import FileResponse, HTMLResponse, JSONResponse, RedirectResponse, Response
from google.auth.transport import requests as google_requests
from google.oauth2 import id_token as google_id_token
from starlette.middleware.sessions import SessionMiddleware


REPO_ROOT = Path(__file__).resolve().parents[1]


def _env(name: str, default: Optional[str] = None) -> Optional[str]:
    value = os.environ.get(name)
    if value is None:
        return default
    value = value.strip()
    return value if value else default


DATA_DIR = Path(_env("FUNCDRAW_SHARE_DATA_DIR", str(Path(__file__).resolve().parent / "data"))).resolve()
BASE_URL = _env("FUNCDRAW_BASE_URL")

GOOGLE_CLIENT_ID = _env("FUNCDRAW_GOOGLE_CLIENT_ID")
GOOGLE_CLIENT_SECRET = _env("FUNCDRAW_GOOGLE_CLIENT_SECRET")
SESSION_SECRET = _env("FUNCDRAW_SESSION_SECRET")

RUNTIME_JS_PATH = _env("FUNCDRAW_RUNTIME_JS_PATH")
FONT_PATH = _env("FUNCDRAW_FONT_PATH")


def _default_font_path() -> Path:
    candidate = REPO_ROOT / "funcdraw.js" / "funcdraw-core" / "assets" / "fonts" / "Inter-Regular.ttf"
    return candidate.resolve()


def _get_font_path() -> Path:
    if FONT_PATH:
        return Path(FONT_PATH).expanduser().resolve()
    return _default_font_path()


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def _safe_next(value: Optional[str]) -> str:
    if not value:
        return "/"
    value = str(value).strip()
    if not value.startswith("/"):
        return "/"
    if value.startswith("//"):
        return "/"
    return value


def _compute_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def _ensure_data_dir() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)


def _normalize_model_id(model_id: str) -> str:
    try:
        UUID(str(model_id))
    except Exception as e:
        raise HTTPException(status_code=404, detail="model not found") from e
    return str(model_id)


def _model_dir(model_id: str) -> Path:
    normalized = _normalize_model_id(model_id)
    return (DATA_DIR / normalized).resolve()


def _load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text("utf-8"))
    except FileNotFoundError:
        return None


def _write_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload, indent=2, sort_keys=False) + "\n", "utf-8")


def _parse_restrict_list(value: Optional[str]) -> List[str]:
    if value is None:
        return []
    text = str(value).strip()
    if not text:
        return []
    parts: List[str] = []
    for chunk in text.split(","):
        email = chunk.strip()
        if email:
            parts.append(email)
    seen: set[str] = set()
    unique: List[str] = []
    for email in parts:
        key = email.lower()
        if key in seen:
            continue
        seen.add(key)
        unique.append(email)
    return unique


def _is_model_restricted(meta: Dict[str, Any]) -> bool:
    access = meta.get("access")
    return bool(access and isinstance(access, dict) and access.get("mode") == "google-allowlist")


def _allowed_emails(meta: Dict[str, Any]) -> List[str]:
    access = meta.get("access")
    if not access or not isinstance(access, dict):
        return []
    emails = access.get("allowedEmails")
    if not isinstance(emails, list):
        return []
    normalized: List[str] = []
    for entry in emails:
        if entry is None:
            continue
        value = str(entry).strip()
        if value:
            normalized.append(value.lower())
    return normalized


def _current_user_email(request: Request) -> Optional[str]:
    session = getattr(request, "session", None)
    if not session or not isinstance(session, dict):
        return None
    email = session.get("email")
    if not email:
        return None
    return str(email).strip()


def _require_google_config() -> None:
    if not GOOGLE_CLIENT_ID or not GOOGLE_CLIENT_SECRET:
        raise HTTPException(status_code=500, detail="Google auth is not configured on this server")
    if not SESSION_SECRET:
        raise HTTPException(status_code=500, detail="Session secret is not configured on this server")


def _authorize_request_for_model(request: Request, model_id: str, *, html_redirect: bool) -> Dict[str, Any]:
    meta_path = _model_dir(model_id) / "meta.json"
    meta = _load_json(meta_path)
    if not meta:
        raise HTTPException(status_code=404, detail="model not found")

    if not _is_model_restricted(meta):
        return meta

    _require_google_config()

    email = _current_user_email(request)
    if not email:
        query = f"?{request.url.query}" if request.url.query else ""
        next_url = _safe_next(f"{request.url.path}{query}")
        login_url = _build_login_url(next_url)
        if html_redirect:
            raise _RedirectToLogin(login_url)
        raise HTTPException(status_code=401, detail={"error": "authentication required", "loginUrl": login_url})

    allowed = _allowed_emails(meta)
    if allowed and email.lower() not in allowed:
        if html_redirect:
            raise HTTPException(status_code=403, detail="Access denied")
        raise HTTPException(status_code=403, detail={"error": "forbidden"})

    return meta


class _RedirectToLogin(Exception):
    def __init__(self, location: str):
        super().__init__(location)
        self.location = location


_runtime_cache: Optional[str] = None


def _load_runtime_js() -> str:
    global _runtime_cache
    if _runtime_cache is not None:
        return _runtime_cache

    if RUNTIME_JS_PATH:
        path = Path(RUNTIME_JS_PATH).expanduser().resolve()
        _runtime_cache = path.read_text("utf-8")
        return _runtime_cache

    # Best-effort: bundle from the repo using Node + esbuild, matching @funcdraw/play behavior.
    node_script = """
    (async () => {
      const { bundleBrowserRuntime } = require('./funcdraw.js/funcdraw-play/src/runtime-bundler');
      const text = await bundleBrowserRuntime();
      process.stdout.write(text);
    })().catch((err) => {
      console.error(err && err.stack ? err.stack : String(err));
      process.exit(1);
    });
    """.strip()

    try:
        proc = subprocess.run(
            ["node", "-e", node_script],
            cwd=str(REPO_ROOT),
            check=True,
            capture_output=True,
            text=True,
        )
    except FileNotFoundError as e:
        raise HTTPException(
            status_code=500,
            detail="Node is required to bundle the runtime (set FUNCDRAW_RUNTIME_JS_PATH to a prebuilt bundle).",
        ) from e
    except subprocess.CalledProcessError as e:
        stderr = (e.stderr or "").strip()
        raise HTTPException(
            status_code=500,
            detail=f"Failed to bundle runtime via Node (set FUNCDRAW_RUNTIME_JS_PATH). {stderr or ''}".strip(),
        ) from e

    _runtime_cache = proc.stdout
    return _runtime_cache


def _render_play_html(*, model_id: str, title: str) -> str:
    base_href = f"/play/{model_id}/"
    safe_title = title.strip() if title and title.strip() else "FuncDraw Share"

    opts = {
        "initialTime": None,
        "baseHref": base_href,
        "title": safe_title,
        "embed": False,
    }
    payload = json.dumps(opts)
    node_script = """
    const { createHtmlTemplate } = require('./funcdraw.js/funcdraw-play/src/html-template');
    const opts = JSON.parse(process.env.FUNCDRAW_PLAY_HTML_OPTS || '{}');
    process.stdout.write(createHtmlTemplate(opts));
    """.strip()

    env = os.environ.copy()
    env["FUNCDRAW_PLAY_HTML_OPTS"] = payload
    try:
        proc = subprocess.run(
            ["node", "-e", node_script],
            cwd=str(REPO_ROOT),
            check=True,
            capture_output=True,
            text=True,
            env=env,
        )
        return proc.stdout
    except Exception:
        # Fallback: minimal error page.
        return (
            "<!DOCTYPE html><html><head><meta charset='utf-8'/>"
            f"<title>{safe_title}</title></head><body>"
            "<h1>FuncDraw Share</h1>"
            "<p>Failed to render player HTML template. Ensure Node is installed.</p>"
            "</body></html>"
        )


app = FastAPI()

if SESSION_SECRET:
    app.add_middleware(
        SessionMiddleware,
        secret_key=SESSION_SECRET,
        same_site="lax",
        https_only=bool(BASE_URL and BASE_URL.startswith("https://")),
    )


@app.exception_handler(_RedirectToLogin)
async def _redirect_handler(_req: Request, exc: _RedirectToLogin) -> Response:
    return RedirectResponse(url=exc.location, status_code=302)


@app.get("/api/health")
async def health() -> Dict[str, bool]:
    return {"ok": True}


@app.post("/api/models")
async def create_model(
    request: Request,
    file: UploadFile = File(...),
    meta: Optional[str] = Form(None),
    bootstrap: Optional[str] = Form(None),
    restrict: Optional[str] = Form(None),
) -> Dict[str, str]:
    _ensure_data_dir()

    model_id = str(uuid4())
    folder = _model_dir(model_id)
    folder.mkdir(parents=True, exist_ok=False)

    zip_path = folder / "package.zip"
    bytes_written = 0
    with zip_path.open("wb") as out:
        while True:
            chunk = await file.read(1024 * 1024)
            if not chunk:
                break
            out.write(chunk)
            bytes_written += len(chunk)

    sha256 = _compute_sha256(zip_path)

    client_meta: Dict[str, Any] = {}
    if meta:
        try:
            parsed = json.loads(meta)
            if isinstance(parsed, dict):
                client_meta = parsed
        except json.JSONDecodeError:
            client_meta = {}

    allowlist = _parse_restrict_list(restrict)
    access: Optional[Dict[str, Any]] = None
    if allowlist:
        _require_google_config()
        access = {"mode": "google-allowlist", "allowedEmails": allowlist}

    stored_meta: Dict[str, Any] = {
        "schemaVersion": 1,
        "modelId": model_id,
        "createdAt": _now_iso(),
        "package": client_meta.get("package") if isinstance(client_meta.get("package"), dict) else None,
        "client": client_meta.get("client") if isinstance(client_meta.get("client"), dict) else None,
        "source": client_meta.get("source") if isinstance(client_meta.get("source"), dict) else None,
        "artifact": {
            "filename": "package.zip",
            "contentType": file.content_type or "application/zip",
            "bytes": bytes_written,
            "sha256": sha256,
        },
    }
    if access:
        stored_meta["access"] = access

    stored_meta = {k: v for k, v in stored_meta.items() if v is not None}

    _write_json(folder / "meta.json", stored_meta)

    if bootstrap:
        try:
            parsed_bootstrap = json.loads(bootstrap)
            _write_json(folder / "bootstrap.json", parsed_bootstrap)
        except json.JSONDecodeError as e:
            raise HTTPException(status_code=400, detail="Invalid bootstrap JSON") from e

    base = BASE_URL.rstrip("/") if BASE_URL else str(request.base_url).rstrip("/")
    play_url = f"{base}/play/{model_id}"
    download_url = f"{base}/api/models/{model_id}/package.zip"
    return {
        "modelId": model_id,
        "playUrl": play_url,
        "downloadUrl": download_url,
    }


@app.get("/api/models/{model_id}/meta.json")
async def get_meta(request: Request, model_id: str) -> Response:
    _authorize_request_for_model(request, model_id, html_redirect=False)
    path = _model_dir(model_id) / "meta.json"
    if not path.exists():
        raise HTTPException(status_code=404, detail="model not found")
    return FileResponse(path, media_type="application/json")


@app.get("/api/models/{model_id}/package.zip")
async def get_zip(request: Request, model_id: str) -> Response:
    _authorize_request_for_model(request, model_id, html_redirect=False)
    path = _model_dir(model_id) / "package.zip"
    if not path.exists():
        raise HTTPException(status_code=404, detail="model not found")
    return FileResponse(
        path,
        media_type="application/zip",
        filename="package.zip",
    )


@app.get("/api/models/{model_id}/bootstrap.json")
async def get_bootstrap(request: Request, model_id: str) -> Response:
    _authorize_request_for_model(request, model_id, html_redirect=False)
    path = _model_dir(model_id) / "bootstrap.json"
    if not path.exists():
        raise HTTPException(status_code=404, detail="bootstrap not found")
    return FileResponse(path, media_type="application/json")


@app.get("/api/me")
async def me(request: Request) -> Dict[str, Optional[str]]:
    email = _current_user_email(request)
    return {"email": email}


@app.get("/play/{model_id}")
async def play(request: Request, model_id: str) -> Response:
    meta = _authorize_request_for_model(request, model_id, html_redirect=True)
    title = "FuncDraw Share"
    pkg = meta.get("package") if isinstance(meta.get("package"), dict) else None
    if pkg and pkg.get("name"):
        title = f"{pkg.get('name')} · FuncDraw Share"
    html = _render_play_html(model_id=model_id, title=title)
    return HTMLResponse(html, headers={"Cache-Control": "no-store"})


@app.get("/play/{model_id}/__funcdraw/runtime.js")
async def play_runtime(request: Request, model_id: str) -> Response:
    _authorize_request_for_model(request, model_id, html_redirect=False)
    source = _load_runtime_js()
    return Response(
        content=source,
        media_type="application/javascript; charset=utf-8",
        headers={"Cache-Control": "no-store"},
    )


@app.get("/play/{model_id}/__funcdraw/assets/fonts/Inter-Regular.ttf")
async def play_font(request: Request, model_id: str) -> Response:
    _authorize_request_for_model(request, model_id, html_redirect=False)
    path = _get_font_path()
    if not path.exists():
        raise HTTPException(status_code=500, detail=f"Font file not found: {path}")
    return FileResponse(
        path,
        media_type="font/ttf",
        filename="Inter-Regular.ttf",
        headers={"Cache-Control": "no-store"},
    )


@app.get("/play/{model_id}/__funcdraw/bootstrap")
async def play_bootstrap(request: Request, model_id: str) -> Response:
    _authorize_request_for_model(request, model_id, html_redirect=False)
    path = _model_dir(model_id) / "bootstrap.json"
    if not path.exists():
        raise HTTPException(status_code=404, detail="bootstrap not found (upload must include bootstrap payload)")
    return FileResponse(path, media_type="application/json", headers={"Cache-Control": "no-store"})


@app.get("/auth/google/login")
async def google_login(request: Request, next: Optional[str] = None) -> Response:
    _require_google_config()
    next_url = _safe_next(next)
    state = secrets.token_urlsafe(24)
    request.session["oauth_state"] = state
    request.session["oauth_next"] = next_url

    base = BASE_URL.rstrip("/") if BASE_URL else str(request.base_url).rstrip("/")
    redirect_uri = f"{base}/auth/google/callback"

    params = {
        "client_id": GOOGLE_CLIENT_ID,
        "redirect_uri": redirect_uri,
        "response_type": "code",
        "scope": "openid email profile",
        "state": state,
        "prompt": "select_account",
    }
    url = "https://accounts.google.com/o/oauth2/v2/auth"
    return RedirectResponse(url=f"{url}?{_encode_query(params)}", status_code=302)


@app.get("/auth/google/callback")
async def google_callback(request: Request, code: Optional[str] = None, state: Optional[str] = None) -> Response:
    _require_google_config()
    expected_state = request.session.get("oauth_state")
    next_url = _safe_next(request.session.get("oauth_next"))
    request.session.pop("oauth_state", None)
    request.session.pop("oauth_next", None)

    if not code:
        raise HTTPException(status_code=400, detail="Missing code")
    if not state or not expected_state or state != expected_state:
        raise HTTPException(status_code=400, detail="Invalid state")

    base = BASE_URL.rstrip("/") if BASE_URL else str(request.base_url).rstrip("/")
    redirect_uri = f"{base}/auth/google/callback"
    token_resp = requests.post(
        "https://oauth2.googleapis.com/token",
        data={
            "code": code,
            "client_id": GOOGLE_CLIENT_ID,
            "client_secret": GOOGLE_CLIENT_SECRET,
            "redirect_uri": redirect_uri,
            "grant_type": "authorization_code",
        },
        timeout=15,
    )
    if token_resp.status_code != 200:
        raise HTTPException(status_code=401, detail="Token exchange failed")

    token_payload = token_resp.json()
    raw_id_token = token_payload.get("id_token")
    if not raw_id_token:
        raise HTTPException(status_code=401, detail="Missing id_token")

    try:
        info = google_id_token.verify_oauth2_token(
            raw_id_token,
            google_requests.Request(),
            GOOGLE_CLIENT_ID,
        )
    except Exception as e:
        raise HTTPException(status_code=401, detail="Invalid id_token") from e

    email = info.get("email")
    verified = info.get("email_verified")
    if not email or not verified:
        raise HTTPException(status_code=401, detail="Email not verified")

    request.session["email"] = email
    request.session["name"] = info.get("name")
    request.session["picture"] = info.get("picture")
    request.session["iat"] = _now_iso()

    return RedirectResponse(url=next_url, status_code=302)


@app.post("/auth/logout")
async def logout(request: Request) -> Response:
    if getattr(request, "session", None):
        request.session.clear()
    return JSONResponse({"ok": True})


def _encode_query(params: Dict[str, str]) -> str:
    from urllib.parse import urlencode

    return urlencode({k: v for k, v in params.items() if v is not None})


def _build_login_url(next_url: str) -> str:
    from urllib.parse import urlencode

    return f"/auth/google/login?{urlencode({'next': next_url})}"

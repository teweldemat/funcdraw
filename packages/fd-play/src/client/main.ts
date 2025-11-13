import { FuncdrawPlayer, MemoryExpressionResolver, ModuleRegistry, type ExpressionEntry } from '@funcdraw/fd-player';

declare global {
  interface Window {
    __fdPlayer?: FuncdrawPlayer;
  }
}

const canvas = document.getElementById('fd-canvas') as HTMLCanvasElement | null;
const statusEl = document.getElementById('fd-status');
const reloadButton = document.getElementById('fd-reload');

const setStatus = (message: string, isError = false) => {
  if (!statusEl) {
    return;
  }
  statusEl.textContent = message;
  statusEl.setAttribute('data-error', isError ? 'true' : 'false');
};

if (!canvas) {
  setStatus('Unable to locate target canvas element.', true);
  throw new Error('Missing #fd-canvas element');
}

const player = new FuncdrawPlayer({ canvas });
window.__fdPlayer = player;

type WorkspacePayload = {
  entries: ExpressionEntry[];
  modules: Record<string, ExpressionEntry[]>;
};

const loadModel = async () => {
  setStatus('Loading workspace…');
  try {
    const response = await fetch(`/workspace.json?ts=${Date.now()}`);
    if (!response.ok) {
      throw new Error(await response.text());
    }
    const payload = (await response.json()) as WorkspacePayload;
    const resolver = new MemoryExpressionResolver(payload.entries ?? []);
    const modulesEntries = new Map<string, ExpressionEntry[]>(
      Object.entries(payload.modules ?? {})
    );
    const moduleRegistry = modulesEntries.size > 0 ? new ModuleRegistry(modulesEntries) : null;
    player.setResolver(resolver, moduleRegistry);
    player.render();
    const warnings = player.getWarnings();
    if (warnings.length > 0) {
      setStatus(warnings.join(' • '), false);
    } else {
      setStatus('Rendered current workspace.');
    }
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    setStatus(message, true);
    console.error(err);
  }
};

reloadButton?.addEventListener('click', () => {
  loadModel();
});

void loadModel();

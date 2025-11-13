import { FuncdrawPlayer } from '@funcdraw/fd-player';

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

const loadModel = async () => {
  setStatus('Loading workspace…');
  try {
    await player.loadArchiveFromUrl(`/model.fdmodel?ts=${Date.now()}`);
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

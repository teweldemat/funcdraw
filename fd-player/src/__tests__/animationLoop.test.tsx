import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';

vi.mock('../App.css', () => ({}));

vi.mock('../ExpressionTree', () => ({
  ExpressionTree: () => null
}));

vi.mock('../components/ExamplePopup', () => ({
  ExamplePopup: () => null
}));

vi.mock('../components/ReferencePopup', () => ({
  ReferencePopup: () => null
}));

vi.mock('../components/StatusMessage', () => ({
  StatusMessage: () => null
}));

vi.mock('../components/ExpressionEditor', () => ({
  ExpressionEditor: () => null
}));

vi.mock('../components/LibraryImportDialog', () => ({
  LibraryImportDialog: () => null
}));

vi.mock('../reference', () => ({
  PRIMITIVE_REFERENCE: []
}));

vi.mock('../utils/importWorkspace', () => ({
  importWorkspaceFromArchive: vi.fn(() =>
    Promise.resolve({
      tabs: [],
      folders: []
    })
  )
}));

vi.mock('../workspaceExport', () => ({
  buildWorkspaceFiles: vi.fn(() => new Map())
}));

vi.mock('../utils/zip', () => ({
  createZipBlob: vi.fn(async () => new Blob()),
  MODEL_ARCHIVE_EXTENSION: '.fd',
  MODEL_ARCHIVE_MIME: 'application/funcdraw'
}));

vi.mock('../examples', () => ({
  __esModule: true,
  default: [
    {
      id: 'mock-example',
      name: 'Mock Example',
      view: 'mock-view',
      viewLanguage: 'funcscript',
      graphics: 'mock-graphics',
      graphicsLanguage: 'funcscript',
      customTabs: [],
      customFolders: []
    }
  ]
}));

const { default: App } = await import('../App');

const createMockContext = () => {
  const target: Record<string, unknown> = {
    canvas: document.createElement('canvas')
  };
  return new Proxy(target, {
    get(obj, key) {
      if (!(key in obj)) {
        obj[key as string] = vi.fn();
      }
      return obj[key as string];
    },
    set(obj, key, value) {
      obj[key as string] = value;
      return true;
    }
  }) as unknown as CanvasRenderingContext2D;
};

describe('fd-player animation loop', () => {
  let consoleError: ReturnType<typeof vi.spyOn>;
  let requestAnimationFrameSpy: ReturnType<typeof vi.spyOn>;
  let cancelAnimationFrameSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    vi.stubGlobal('ResizeObserver', class {
      observe() {}
      unobserve() {}
      disconnect() {}
    });

    window.matchMedia = vi.fn().mockImplementation(() => ({
      matches: false,
      media: '',
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      onchange: null,
      dispatchEvent: vi.fn()
    }));

    window.alert = vi.fn();
    window.prompt = vi.fn();
    window.confirm = vi.fn();

    HTMLCanvasElement.prototype.getContext = vi.fn(() => createMockContext());

    let frameId = 0;
    requestAnimationFrameSpy = vi
      .spyOn(window, 'requestAnimationFrame')
      .mockImplementation((callback: FrameRequestCallback) => {
        frameId += 1;
        callback(performance.now());
        return frameId;
      });
    cancelAnimationFrameSpy = vi
      .spyOn(window, 'cancelAnimationFrame')
      .mockImplementation(() => {});
  });

  afterEach(() => {
    requestAnimationFrameSpy.mockRestore();
    cancelAnimationFrameSpy.mockRestore();
    consoleError.mockRestore();
    vi.unstubAllGlobals();
  });

  it('does not recurse infinitely when starting playback', () => {
    render(<App />);
    const playButton = screen.getByRole('button', { name: /play/i });
    expect(() => {
      fireEvent.click(playButton);
    }).not.toThrow();

    const errorMessages = consoleError.mock.calls.map(([message]) => String(message));
    expect(
      errorMessages.some((message) =>
        message.includes('Maximum update depth exceeded') ||
        message.includes('Maximum call stack size exceeded')
      )
    ).toBe(false);
  });
});

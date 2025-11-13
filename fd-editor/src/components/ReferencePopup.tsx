import { MouseEvent as ReactMouseEvent, useEffect, useMemo, useState } from 'react';
import type { JSX } from 'react';
import { PRIMITIVE_REFERENCE } from '../reference';

type ReferencePopupProps = {
  open: boolean;
  selection: string;
  onSelect: (value: string) => void;
  onClose: () => void;
};

export function ReferencePopup({ open, selection, onSelect, onClose }: ReferencePopupProps): JSX.Element | null {
  const current = useMemo(
    () => PRIMITIVE_REFERENCE.find((entry) => entry.name === selection) ?? PRIMITIVE_REFERENCE[0] ?? null,
    [selection]
  );
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!copied || typeof window === 'undefined') {
      return;
    }
    const timer = window.setTimeout(() => setCopied(false), 2000);
    return () => window.clearTimeout(timer);
  }, [copied]);

  if (!open) {
    return null;
  }

  const handleBackgroundClick = (event: ReactMouseEvent<HTMLDivElement>) => {
    if (event.currentTarget === event.target) {
      onClose();
    }
  };

  const handleTopicClick = (value: string) => {
    if (value !== selection) {
      onSelect(value);
    }
  };

  const handleCopy = async () => {
    if (!current?.example) {
      return;
    }
    const text = current.example;
    try {
      if (typeof navigator !== 'undefined' && navigator?.clipboard?.writeText) {
        await navigator.clipboard.writeText(text);
      } else {
        throw new Error('Clipboard API unavailable');
      }
    } catch {
      const textarea = document.createElement('textarea');
      textarea.value = text;
      textarea.style.position = 'fixed';
      textarea.style.opacity = '0';
      document.body.appendChild(textarea);
      textarea.focus();
      textarea.select();
      try {
        document.execCommand('copy');
      } finally {
        document.body.removeChild(textarea);
      }
    }
    setCopied(true);
  };

  const topicButtons = PRIMITIVE_REFERENCE.map((entry) => {
    const selected = entry.name === (current?.name ?? '');
    return (
      <button
        key={entry.name}
        type="button"
        role="option"
        aria-selected={selected}
        className={`reference-topic-button${selected ? ' reference-topic-button-selected' : ''}`}
        onClick={() => handleTopicClick(entry.name)}
      >
        <span className="reference-topic-title">{entry.title}</span>
        <span className="reference-topic-name">{entry.name}</span>
      </button>
    );
  });

  return (
    <div
      className="dialog-overlay"
      onClick={handleBackgroundClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby="reference-title"
    >
      <div className="dialog reference-dialog">
        <header className="dialog-header">
          <h2 id="reference-title">Reference</h2>
          <button type="button" className="dialog-close" onClick={onClose} aria-label="Close reference">
            ×
          </button>
        </header>
        <div className="dialog-body reference-body">
          <div className="reference-layout">
            <aside
              className="reference-topic-list"
              role="listbox"
              aria-label="Reference topics"
              aria-activedescendant={current?.name ?? undefined}
            >
              {topicButtons}
            </aside>
            <div className="reference-content">
              {current ? (
                <article className="reference-details" aria-live="polite">
                  <p className="reference-description">{current.description}</p>
                  <div className="reference-example-header">
                    <span>Example</span>
                    <div className="reference-copy-wrapper">
                      <button
                        type="button"
                        className="reference-copy-button"
                        onClick={handleCopy}
                        disabled={!current.example}
                      >
                        Copy
                      </button>
                      {copied ? <span className="reference-copy-status">Copied!</span> : null}
                    </div>
                  </div>
                  <pre className="dialog-example reference-example">
                    <code>{current.example}</code>
                  </pre>
                </article>
              ) : (
                <p className="dialog-empty">No topics available.</p>
              )}
            </div>
          </div>
          <div className="dialog-reference-link">
            <a href="/docs/index.html" target="_blank" rel="noopener noreferrer">
              FuncDraw Documentation
            </a>
            <a href="https://www.funcscript.org/" target="_blank" rel="noopener noreferrer">
              Open the full FuncScript language reference
            </a>
          </div>
        </div>
        <footer className="dialog-footer">
          <button type="button" className="control-button" onClick={onClose}>
            Close
          </button>
        </footer>
      </div>
    </div>
  );
}

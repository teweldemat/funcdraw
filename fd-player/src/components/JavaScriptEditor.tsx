import { useEffect, useRef } from 'react';
import type { CSSProperties } from 'react';
import { EditorState } from '@codemirror/state';
import { EditorView, keymap } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap } from '@codemirror/commands';
import { javascript } from '@codemirror/lang-javascript';
import { basicSetup } from 'codemirror';

export type JavaScriptEditorProps = {
  value: string;
  onChange: (value: string) => void;
  minHeight?: number;
  style?: CSSProperties;
  ariaLabel?: string;
};

export const JavaScriptEditor = ({ value, onChange, minHeight = 0, style, ariaLabel }: JavaScriptEditorProps) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const viewRef = useRef<EditorView | null>(null);
  const onChangeRef = useRef(onChange);

  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  useEffect(() => {
    if (!containerRef.current) {
      return undefined;
    }

    const updateListener = EditorView.updateListener.of((update) => {
      if (update.docChanged) {
        const docValue = update.state.doc.toString();
        onChangeRef.current(docValue);
      }
    });

    const state = EditorState.create({
      doc: value,
      extensions: [
        basicSetup,
        history(),
        keymap.of([...defaultKeymap, ...historyKeymap]),
        javascript(),
        EditorView.lineWrapping,
        updateListener,
        EditorView.theme({
          '&': { height: '100%' },
          '.cm-editor': { fontSize: '14px' },
          '.cm-scroller': {
            fontFamily: '"JetBrains Mono", "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace',
            lineHeight: '1.5'
          }
        })
      ]
    });

    const view = new EditorView({ state, parent: containerRef.current });
    viewRef.current = view;

    return () => {
      view.destroy();
      viewRef.current = null;
    };
  }, []);

  useEffect(() => {
    const view = viewRef.current;
    if (!view) {
      return;
    }
    const currentValue = view.state.doc.toString();
    if (value !== currentValue) {
      view.dispatch({
        changes: { from: 0, to: view.state.doc.length, insert: value }
      });
    }
  }, [value]);

  return (
    <div
      className="js-editor-host"
      style={{ minHeight, ...style }}
      aria-label={ariaLabel}
      ref={containerRef}
    />
  );
};

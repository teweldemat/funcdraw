import type { CSSProperties } from 'react';
import { FuncScriptEditor } from '@tewelde/funcscript-editor';
import type { ExpressionLanguage } from '@funcdraw/core';
import { JavaScriptEditor } from './JavaScriptEditor';

export type ExpressionEditorProps = {
  value: string;
  language: ExpressionLanguage;
  onChange: (value: string) => void;
  onLanguageChange: (language: ExpressionLanguage) => void;
  minHeight?: number;
  style?: CSSProperties;
  ariaLabel?: string;
};

const languages: Array<{ id: ExpressionLanguage; label: string }> = [
  { id: 'funcscript', label: 'FuncScript' },
  { id: 'javascript', label: 'JavaScript' }
];

export const ExpressionEditor = ({
  value,
  language,
  onChange,
  onLanguageChange,
  minHeight = 0,
  style,
  ariaLabel
}: ExpressionEditorProps) => {
  const handleLanguageClick = (next: ExpressionLanguage) => {
    if (next !== language) {
      onLanguageChange(next);
    }
  };

  return (
    <div className="editor-inner" style={style}>
      <div className="editor-language-bar" role="group" aria-label="Expression language">
        {languages.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            className={`language-button${language === id ? ' language-button-active' : ''}`}
            onClick={() => handleLanguageClick(id)}
            aria-pressed={language === id}
          >
            {label}
          </button>
        ))}
      </div>
      <div className="editor-body">
        {language === 'javascript' ? (
          <JavaScriptEditor value={value} onChange={onChange} minHeight={minHeight} ariaLabel={ariaLabel} />
        ) : (
          <FuncScriptEditor value={value} onChange={onChange} minHeight={minHeight} style={{ flex: 1 }} />
        )}
      </div>
    </div>
  );
};

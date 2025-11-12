import type { JSX } from 'react';
import type { FuncDrawErrorDetail } from '@tewelde/funcdraw';

type StatusMessageProps = {
  error?: string | null;
  errorDetails?: FuncDrawErrorDetail[] | null;
  warning?: string | null;
  info?: string | string[] | null;
  success?: string | null;
};

const describeErrorKind = (kind: FuncDrawErrorDetail['kind']): string => {
  switch (kind) {
    case 'funcscript-parser':
      return 'FuncScript parser';
    case 'funcscript-runtime':
      return 'FuncScript runtime';
    case 'javascript':
      return 'JavaScript';
    case 'funcdraw':
      return 'FuncDraw';
    default:
      return typeof kind === 'string' && kind.trim() ? kind : 'Error';
  }
};

const renderErrorDetails = (details: FuncDrawErrorDetail[]): JSX.Element | null => {
  if (!Array.isArray(details) || details.length === 0) {
    return null;
  }
  return (
    <ul className="status-error-details">
      {details.map((detail, index) => {
        const location = detail.location
          ? `line ${detail.location.line}, column ${detail.location.column}`
          : null;
        return (
          <li key={`error-detail-${index}`} className="status-error-detail">
            <div className="status-error-detail-header">
              <span className="status-error-kind">{describeErrorKind(detail.kind)}</span>
              {location ? <span className="status-error-location">{location}</span> : null}
            </div>
            <p className="status-error-detail-message">{detail.message}</p>
            {detail.context ? (
              <pre className="status-error-context">
                <code>
                  {detail.context.lineText}
                  {'\n'}
                  {detail.context.pointerText}
                </code>
              </pre>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
};

export const StatusMessage = ({
  error,
  errorDetails,
  warning,
  info,
  success
}: StatusMessageProps): JSX.Element | null => {
  const hasErrorDetails = Array.isArray(errorDetails) && errorDetails.length > 0;
  if (error || hasErrorDetails) {
    return (
      <div className={`status status-error${hasErrorDetails ? ' status-rich' : ''}`}>
        {error ? <p className="status-error-summary">{error}</p> : null}
        {hasErrorDetails ? renderErrorDetails(errorDetails!) : null}
      </div>
    );
  }
  if (warning) {
    return <p className="status status-warning">{warning}</p>;
  }
  if (info && Array.isArray(info) && info.length > 0) {
    return (
      <ul className="status status-info">
        {info.map((entry, index) => (
          <li key={index}>{entry}</li>
        ))}
      </ul>
    );
  }
  if (info && typeof info === 'string') {
    return <p className="status status-info">{info}</p>;
  }
  if (success) {
    return <p className="status status-success">{success}</p>;
  }
  return null;
};

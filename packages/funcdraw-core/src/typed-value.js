'use strict';

const { isPlainObject } = require('./utils');

function createValueConverter(funcscript, options = {}) {
  const { typeOf, valueOf, FSDataType } = funcscript;
  const logger = options.logger || null;

  const logLine = (text) => {
    if (!text) {
      return;
    }
    if (typeof logger === 'function') {
      logger(text);
      return;
    }
    if (logger && typeof logger.log === 'function') {
      logger.log(text);
    }
  };

  function safeStringify(value) {
    try {
      return JSON.stringify(
        value,
        (_key, val) => {
          if (typeof val === 'bigint') {
            return val.toString();
          }
          if (typeof val === 'function') {
            return '<function>';
          }
          if (val === undefined) {
            return null;
          }
          return val;
        },
        0
      );
    } catch {
      return String(value);
    }
  }

  function formatPreview(value) {
    if (value === null) {
      return 'null';
    }
    if (value === undefined) {
      return 'undefined';
    }
    const type = typeof value;
    if (type === 'function') {
      return '<function>';
    }
    if (type === 'object') {
      return safeStringify(value);
    }
    return String(value);
  }

  function formatPath(path) {
    if (!Array.isArray(path) || path.length === 0) {
      return '';
    }
    let label = '';
    for (const segment of path) {
      if (typeof segment === 'number') {
        label += `[${segment}]`;
      } else {
        label += (label ? '.' : '') + segment;
      }
    }
    return label;
  }

  function logKey(path, key, kind, value) {
    if (!logger) {
      return;
    }
    const base = formatPath(path);
    const prefix = base ? `${base}.` : '';
    const placeholders = {
      list: '[list]',
      kvc: '[kvc]',
      function: '[function]',
      error: '[error]',
      object: '[object]'
    };
    const rendered =
      kind === 'atomic'
        ? formatPreview(value)
        : placeholders[kind] || `[${kind}]`;
    logLine(`-[${prefix}${key}]: ${rendered}`);
  }

  function logKvc(path, value) {
    if (!logger) {
      return;
    }
    const base = formatPath(path);
    const prefix = base ? `${base}: ` : '';
    logLine(`${prefix}${safeStringify(value)}`);
  }

  function classifyTyped(typed) {
    const t = typeOf(typed);
    switch (t) {
      case FSDataType.List:
        return 'list';
      case FSDataType.KeyValueCollection:
        return 'kvc';
      case FSDataType.Function:
        return 'function';
      case FSDataType.Error:
        return 'error';
      default:
        return 'atomic';
    }
  }

  function toPlain(value, path = []) {
    if (!value) {
      return null;
    }
    const typed = funcscript.assertTyped ? funcscript.assertTyped(value) : value;
    const dataType = typeOf(typed);

    switch (dataType) {
      case FSDataType.Null:
        return null;
      case FSDataType.Boolean:
      case FSDataType.Integer:
      case FSDataType.Float:
      case FSDataType.String:
      case FSDataType.BigInteger:
      case FSDataType.Guid:
      case FSDataType.DateTime:
      case FSDataType.ByteArray:
        return valueOf(typed);
      case FSDataType.List: {
        const items = valueOf(typed);
        const result = [];
        let index = 0;
        for (const item of items) {
          result.push(toPlain(item, path.concat(index)));
          index += 1;
        }
        return result;
      }
      case FSDataType.KeyValueCollection: {
        const collection = valueOf(typed);
        const result = {};
        const entries = typeof collection.getAll === 'function' ? collection.getAll() : [];
        for (const [key, entryValue] of entries) {
          const entryKind = classifyTyped(entryValue);
          const converted = toPlain(entryValue, path.concat(key));
          logKey(path, key, entryKind, converted);
          result[key] = converted;
        }
        logKvc(path, result);
        return result;
      }
      case FSDataType.Function:
        return valueOf(typed);
      case FSDataType.Error: {
        const errorValue = valueOf(typed);
        if (errorValue && isPlainObject(errorValue)) {
          return { ...errorValue };
        }
        return errorValue;
      }
      default:
        return valueOf(typed);
    }
  }

  function logAccess(path, key, typedValue) {
    if (!logger) {
      return;
    }
    let kind = 'atomic';
    let preview = typedValue;
    try {
      const typed = funcscript.assertTyped ? funcscript.assertTyped(typedValue) : typedValue;
      kind = classifyTyped(typed);
      if (kind === 'atomic') {
        preview = toPlain(typed, path.concat(key));
      } else {
        preview = null;
      }
    } catch {
      // ignore log failures
    }
    logKey(path, key, kind, preview);
  }

  return {
    toPlain,
    logAccess
  };
}

module.exports = {
  createValueConverter
};

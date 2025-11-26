'use strict';

const { toArray, isPlainObject } = require('./utils');

function createValueConverter(funcscript) {
  const { typeOf, valueOf, FSDataType } = funcscript;

  function toPlain(value) {
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
        const items = toArray(valueOf(typed));
        return items.map((item) => toPlain(item));
      }
      case FSDataType.KeyValueCollection: {
        const collection = valueOf(typed);
        const result = {};
        const entries = typeof collection.getAll === 'function' ? collection.getAll() : [];
        for (const [key, entryValue] of entries) {
          result[key] = toPlain(entryValue);
        }
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

  return {
    toPlain
  };
}

module.exports = {
  createValueConverter
};

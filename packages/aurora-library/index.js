const fs = require('node:fs');
const path = require('node:path');
const {
  Engine,
  DefaultFsDataProvider,
  FSDataType,
  FsList,
  KeyValueCollection
} = require('@tewelde/funcscript/browser');

const sourcePath = path.join(__dirname, 'library.fs');
const source = fs.readFileSync(sourcePath, 'utf8');

const provider = new DefaultFsDataProvider();
const typed = Engine.evaluate(source, provider);

const toPlainValue = (value) => {
  if (value === null || value === undefined) {
    return null;
  }
  const type = Engine.typeOf(value);
  const raw = Engine.valueOf(value);
  switch (type) {
    case FSDataType.Null:
    case FSDataType.Boolean:
    case FSDataType.Integer:
    case FSDataType.Float:
    case FSDataType.String:
    case FSDataType.Guid:
    case FSDataType.DateTime:
    case FSDataType.BigInteger:
      return raw;
    case FSDataType.List: {
      if (raw instanceof FsList && typeof raw.toArray === 'function') {
        return raw.toArray().map((entry) => toPlainValue(entry));
      }
      if (Array.isArray(raw)) {
        return raw.map((entry) => toPlainValue(entry));
      }
      return [];
    }
    case FSDataType.KeyValueCollection: {
      if (raw instanceof KeyValueCollection && typeof raw.getAll === 'function') {
        const result = {};
        for (const [key, entry] of raw.getAll()) {
          result[key] = toPlainValue(entry);
        }
        return result;
      }
      return raw;
    }
    default:
      return raw;
  }
};

const library = toPlainValue(typed) || {};

module.exports = library;

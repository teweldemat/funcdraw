const test = require('node:test');
const assert = require('node:assert/strict');
const { evaluate } = require('../src');

const ROOT_KEY = '__root__';

const createMockResolver = () => {
  const tree = new Map([
    [ROOT_KEY, [
      { kind: 'expression', name: 'scalar', language: 'funcscript', createdAt: 0 },
      { kind: 'expression', name: 'rootHelper', language: 'funcscript', createdAt: 1 },
      { kind: 'expression', name: 'jsValue', language: 'javascript', createdAt: 2 },
      { kind: 'folder', name: 'nested', createdAt: 3 },
      { kind: 'expression', name: 'jsRecord', language: 'javascript', createdAt: 4 },
      { kind: 'expression', name: 'fxReadsJs', language: 'funcscript', createdAt: 5 }
    ]],
    ['nested', [
      { kind: 'expression', name: 'childFx', language: 'funcscript', createdAt: 0 },
      { kind: 'expression', name: 'childJs', language: 'javascript', createdAt: 1 }
    ]]
  ]);

  const sources = new Map([
    ['scalar', '{\n  return 21;\n}'],
    ['rootHelper', '{\n  return 5;\n}'],
    ['jsValue', 'return scalar * 2 + rootHelper;'],
    ['nested/childFx', '{\n  return 3;\n}'],
    ['nested/childJs', 'return childFx + scalar + rootHelper;'],
    [
      'jsRecord',
      'return { info: { total: scalar + rootHelper, values: [scalar, rootHelper], label: "ok" } };'
    ],
    [
      'fxReadsJs',
      '{\n  record: jsRecord;\n  return record.info.total + record.info.values[1];\n}'
    ]
  ]);

  const normalize = (path) => (path.length === 0 ? ROOT_KEY : path.join('/'));

  return {
    listItems(path) {
      const key = normalize(Array.isArray(path) ? path : []);
      const items = tree.get(key) ?? [];
      return items.map((item) => ({ ...item }));
    },
    getExpression(path) {
      const key = normalize(Array.isArray(path) ? path : []);
      const source = sources.get(key);
      return typeof source === 'string' ? source : null;
    }
  };
};

const getValue = (pathSegments) => {
  const evaluation = evaluate(createMockResolver());
  const result = evaluation.evaluateExpression(pathSegments);
  assert.ok(result, 'expected evaluation result');
  assert.equal(result.error, null);
  return result.value;
};

test('javascript expressions can use sibling FuncScript values', () => {
  const value = getValue(['jsValue']);
  assert.equal(value, 47); // (21 * 2) + 5
});

test('javascript expressions inherit parent scope bindings', () => {
  const childValue = getValue(['nested', 'childJs']);
  assert.equal(childValue, 29); // childFx (3) + scalar (21) + rootHelper (5)
});

test('funcscript expressions can consume javascript object graphs', () => {
  const bridgedValue = getValue(['fxReadsJs']);
  assert.equal(bridgedValue, 31); // (21 + 5) + values[1] (5)
});

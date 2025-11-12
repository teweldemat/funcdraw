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
      { kind: 'expression', name: 'fxReadsJs', language: 'funcscript', createdAt: 5 },
      { kind: 'expression', name: 'brokenFx', language: 'funcscript', createdAt: 6 },
      { kind: 'expression', name: 'runtimeError', language: 'funcscript', createdAt: 7 },
      { kind: 'folder', name: 'moduleF', createdAt: 8 },
      { kind: 'expression', name: 'moduleDirect', language: 'funcscript', createdAt: 9 },
      { kind: 'folder', name: 'moduleJs', createdAt: 10 },
      { kind: 'expression', name: 'moduleJsConsumer', language: 'funcscript', createdAt: 11 }
    ]],
    ['nested', [
      { kind: 'expression', name: 'childFx', language: 'funcscript', createdAt: 0 },
      { kind: 'expression', name: 'childJs', language: 'javascript', createdAt: 1 }
    ]],
    ['moduleF', [
      { kind: 'expression', name: 'eval', language: 'funcscript', createdAt: 0 }
    ]],
    ['moduleJs', [
      { kind: 'expression', name: 'eval', language: 'javascript', createdAt: 0 }
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
    ],
    [
      'brokenFx',
      '{\n  return scalar + (rootHelper;\n}'
    ],
    [
      'runtimeError',
      '{\n  return -"oops";\n}'
    ],
    [
      'moduleF/eval',
      '{\n  return { type: "point"; location: [1, 2]; };\n}'
    ],
    [
      'moduleDirect',
      '{\n  return moduleF;\n}'
    ],
    [
      'moduleJs/eval',
      'export default { value: 99 };'
    ],
    [
      'moduleJsConsumer',
      '{\n  return moduleJs.value;\n}'
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
  assert.equal(result.errorDetails, null);
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

test('parser errors include detailed diagnostics', () => {
  const evaluation = evaluate(createMockResolver());
  const result = evaluation.evaluateExpression(['brokenFx']);
  assert.ok(result, 'expected evaluation');
  assert.notEqual(result.error, null);
  assert.ok(Array.isArray(result.errorDetails));
  assert.ok(result.errorDetails.length >= 1);
  const [detail] = result.errorDetails;
  assert.equal(detail.kind, 'funcscript-parser');
  assert.ok(detail.message.toLowerCase().includes('expected'));
  assert.ok(detail.location);
  assert.equal(detail.location.line, 2);
  assert.ok(detail.location.column > 0);
  assert.ok(detail.context);
  assert.ok(detail.context.lineText.includes('return scalar'));
});

test('runtime errors capture stacks as details', () => {
  const evaluation = evaluate(createMockResolver());
  const result = evaluation.evaluateExpression(['runtimeError']);
  assert.ok(result);
  assert.equal(result.error, 'Numeric operand expected for unary minus');
  assert.ok(Array.isArray(result.errorDetails));
  assert.equal(result.errorDetails.length, 1);
  const [detail] = result.errorDetails;
  assert.equal(detail.kind, 'funcscript-runtime');
  assert.equal(detail.message, result.error);
  assert.ok(typeof detail.stack === 'string');
  assert.ok(detail.stack.includes('NegateFunction'));
});

test('folders execute eval expression when referenced directly', () => {
  const evaluation = evaluate(createMockResolver());
  const result = evaluation.evaluateExpression(['moduleDirect']);
  assert.ok(result);
  assert.equal(result.error, null);
  assert.deepEqual(result.value, {
    type: 'point',
    location: [1, 2]
  });
});

test('javascript folders with export default eval resolve in expressions', () => {
  const evaluation = evaluate(createMockResolver());
  const result = evaluation.evaluateExpression(['moduleJsConsumer']);
  assert.ok(result);
  assert.equal(result.error, null);
  assert.equal(result.value, 99);
});

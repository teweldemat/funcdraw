'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { createExpression } = require('../src');

function createResolver(expression) {
  return {
    listChildren(path) {
      return path.length === 0 ? [] : [];
    },
    getExpression(path) {
      if (path.length === 0) {
        return expression;
      }
      return null;
    },
    package() {
      return null;
    }
  };
}

// Helper functions
function expectLinePrimitive(rawResult) {
  assert.ok(Array.isArray(rawResult.graphics));
  assert.strictEqual(rawResult.graphics.length, 1);
  const line = rawResult.graphics[0];
  assert.deepStrictEqual(line, {
    type: 'line',
    from: [-5, -4],
    to: [10, 8],
    stroke: '#111827',
    width: 0.5
  });
}

function expectLineSvg(svgOutput) {
  assert.ok(typeof svgOutput === 'string' && svgOutput.length > 0);
  assert.match(
    svgOutput,
    /<line[^>]+x1="-5"[^>]+y1="-4"[^>]+x2="10"[^>]+y2="8"[^>]+stroke="#111827"[^>]+stroke-width="0.5"/
  );
}

function expectSingleGlyph(svgOutput) {
  assert.ok(typeof svgOutput === 'string' && svgOutput.length > 0);
  assert.match(svgOutput, /^<svg[^>]*>/);
  assert.match(svgOutput, /<path[^>]+d="[^"]+"/);
  assert.match(svgOutput, /fill="#e2e8f0"/);
}

function collectTracePaths(nodes) {
  const paths = [];
  for (const node of nodes || []) {
    paths.push(node.path);
    if (Array.isArray(node.children)) {
      paths.push(...collectTracePaths(node.children));
    }
  }
  return paths;
}

function findTraceNode(nodes, targetPath) {
  for (const node of nodes || []) {
    if (node.path === targetPath) {
      return node;
    }
    if (Array.isArray(node.children)) {
      const match = findTraceNode(node.children, targetPath);
      if (match) {
        return match;
      }
    }
  }
  return null;
}

test('line primitive produces expected raw data and svg', () => {
  const resolver = createResolver(`
  {
    view:[30,30];
    graphics:[
      {
        type:"line";
        from:[-5,-4];
        to:[10,8];
        stroke:"#111827";
        width:0.5;
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw', 'svg'] });

  assert.deepStrictEqual(result.view, [30, 30]);
  assert.strictEqual(result.warnings.length, 0);
  expectLinePrimitive(result.raw);
  expectLineSvg(result.svg);
});

test('text primitive renders glyph path in svg', () => {
  const resolver = createResolver(`
  {
    graphics:[
      {
        type:"text";
        text:"A";
        position:[0,0];
        fontSize:12;
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['svg'] });

  expectSingleGlyph(result.svg);
});

test('line defaults stroke color when omitted', () => {
  const resolver = createResolver(`
  {
    graphics:[
      {
        type:"line";
        from:[0,0];
        to:[5,5];
      }
    ];
  }
  `);
  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw', 'svg'] });
  const line = result.raw.graphics[0];
  assert.equal(line.stroke, '#38bdf8');
  assert.match(result.svg, /stroke="#38bdf8"/);
});

test('primitives preserve tag metadata in raw output', () => {
  const resolver = createResolver(`
  {
    graphics:[
      {
        type:"line";
        from:[0,0];
        to:[1,1];
        tag:"pose-1";
      }
    ];
  }
  `);
  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw'] });
  assert.equal(result.raw.graphics[0].tag, 'pose-1');
});

test('context values are available during evaluation', () => {
  const resolver = createResolver(`
  {
    view:[10,10];
    graphics:[
      {
        type:"text";
        text:t;
        position:[0,0];
      },
      {
        type:"text";
        text:canvas.size.width;
        position:[0,-2];
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({
    output: ['raw'],
    context: {
      t: 0.5,
      canvas: {
        size: {
          width: 100,
          height: 200
        }
      }
    }
  });

  assert.equal(result.raw.graphics[0].text, 0.5);
  assert.equal(result.raw.graphics[1].text, 100);
  assert.equal(result.valueHooks, undefined);
});

test('trace output collects package evaluation', () => {
  const resolver = {
    listChildren(path) {
      const key = path.join('/');
      if (key === '') {
        return ['eval', 'value'];
      }
      return [];
    },
    getExpression(path) {
      const key = path.join('/');
      if (key === 'eval') {
        return 'value';
      }
      if (key === 'value') {
        return '2';
      }
      return null;
    },
    package() {
      return null;
    }
  };

  const expression = createExpression(resolver);
  const result = expression.evaluate({ trace: true });

  assert.ok(Array.isArray(result.trace));
  assert.ok(result.trace.length >= 1);
  const paths = collectTracePaths(result.trace);
  assert.ok(paths.includes('eval'));
  assert.ok(paths.includes('value'));
  const evalTrace = findTraceNode(result.trace, 'eval');
  assert.ok(evalTrace);
  assert.ok(Array.isArray(evalTrace.children));
  assert.ok(evalTrace.children.some((entry) => entry.path === 'value'));
  const valueTrace = findTraceNode(result.trace, 'value');
  assert.ok(valueTrace);
  assert.equal(valueTrace.resultPreview, '2');
  assert.ok(typeof valueTrace.snippet === 'string' && valueTrace.snippet.includes('2'));
});

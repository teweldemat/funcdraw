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
    import() {
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

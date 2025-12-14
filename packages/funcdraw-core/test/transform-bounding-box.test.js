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

test('fd.translate produces a transform primitive', () => {
  const resolver = createResolver(`
  {
    graphics:[
      fd.translate(
        {
          type:"line";
          from:[0,0];
          to:[1,1];
          stroke:"#111827";
          width:0.5;
        },
        10,
        20
      )
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw', 'svg'] });

  assert.equal(result.warnings.length, 0);
  assert.equal(result.raw.graphics.length, 1);

  const transform = result.raw.graphics[0];
  assert.equal(transform.type, 'transform');
  assert.deepStrictEqual(transform.matrix, [1, 0, 0, 1, 10, 20]);
  assert.equal(transform.graphics.type, 'line');

  assert.match(result.svg, /transform="matrix\(1 0 0 1 10 20\)"/);
  assert.match(result.svg, /<line[^>]+x1="0"[^>]+y1="0"[^>]+x2="1"[^>]+y2="1"/);
});

test('fd.boundingbox includes stroke width for lines', () => {
  const resolver = createResolver(`
  {
    line:{ type:"line"; from:[0,0]; to:[10,0]; width:2; };
    bbox:fd.boundingbox(line);
    graphics:[
      {
        type:"rect";
        position:[bbox.left, bbox.bottom];
        size:[bbox.width, bbox.height];
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw'] });
  assert.equal(result.warnings.length, 0);

  const rect = result.raw.graphics[0];
  assert.equal(rect.type, 'rect');
  assert.deepStrictEqual(rect.position, [-1, -1]);
  assert.deepStrictEqual(rect.size, [12, 2]);
});

test('fd.boundingbox applies transforms when present', () => {
  const resolver = createResolver(`
  {
    line:{ type:"line"; from:[0,0]; to:[10,0]; width:2; };
    moved:fd.translate(line, 5, 7);
    bbox:fd.boundingbox(moved);
    graphics:[
      {
        type:"rect";
        position:[bbox.left, bbox.bottom];
        size:[bbox.width, bbox.height];
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw'] });
  assert.equal(result.warnings.length, 0);

  const rect = result.raw.graphics[0];
  assert.equal(rect.type, 'rect');
  assert.deepStrictEqual(rect.position, [4, 6]);
  assert.deepStrictEqual(rect.size, [12, 2]);
});

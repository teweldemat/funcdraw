'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { createExpression } = require('../src');

function createResolver(expression) {
  return {
    listChildren() {
      return [];
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

function assertSrgb(actual, expected) {
  assert.ok(actual && typeof actual === 'object');
  assert.equal(actual.type, 'color');
  assert.equal(actual.space, 'srgb');
  assert.equal(actual.r, expected.r);
  assert.equal(actual.g, expected.g);
  assert.equal(actual.b, expected.b);
  assert.ok(Math.abs(actual.a - expected.a) < 1e-12);
}

test('fd.color.alpha produces color object and svg renders rgba()', () => {
  const resolver = createResolver(`
  {
    view:[10,10];
    graphics:[
      {
        type:"group";
        opacity:0.5;
        blendMode:"multiply";
        graphics:
        [
          {
            type:"rect";
            position:[0,0];
            size:[10,10];
            fill: fd.color.alpha("#93c5fd", 0.25);
            stroke:"none";
            width:0;
          }
        ];
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw', 'svg'] });

  assert.equal(result.warnings.length, 0);
  assert.ok(Array.isArray(result.raw.graphics));
  assert.equal(result.raw.graphics.length, 1);

  const group = result.raw.graphics[0];
  assert.equal(group.type, 'group');
  assert.equal(group.opacity, 0.5);
  assert.equal(group.blendMode, 'multiply');

  assert.ok(Array.isArray(group.graphics));
  assert.equal(group.graphics.length, 1);
  const rect = group.graphics[0];
  assert.equal(rect.type, 'rect');

  assert.deepStrictEqual(rect.fill, {
    type: 'color',
    space: 'srgb',
    r: 147,
    g: 197,
    b: 253,
    a: 0.25
  });

  assert.match(result.svg, /<g[^>]+opacity="0\.5"/);
  assert.match(result.svg, /mix-blend-mode:\s*multiply/);
  assert.match(result.svg, /fill="rgba\(147, 197, 253, 0\.25\)"/);
});

test('fd.color.hex parses short and long hex forms', () => {
  const resolver = createResolver(`
  {
    view:[10,10];
    graphics:[
      { type:"rect"; name:"rgb3"; position:[0,0]; size:[1,1]; fill: fd.color.hex("#abc"); stroke:"none"; width:0; };
      { type:"rect"; name:"rgba4"; position:[0,0]; size:[1,1]; fill: fd.color.hex("#abcd"); stroke:"none"; width:0; };
      { type:"rect"; name:"rgb6"; position:[0,0]; size:[1,1]; fill: fd.color.hex("#aabbcc"); stroke:"none"; width:0; };
      { type:"rect"; name:"rgba8"; position:[0,0]; size:[1,1]; fill: fd.color.hex("#aabbccdd"); stroke:"none"; width:0; };
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw'] });
  assert.equal(result.warnings.length, 0);

  const byName = Object.fromEntries(result.raw.graphics.map((node) => [node.name, node]));
  assertSrgb(byName.rgb3.fill, { r: 170, g: 187, b: 204, a: 1 });
  assertSrgb(byName.rgb6.fill, { r: 170, g: 187, b: 204, a: 1 });
  assertSrgb(byName.rgba4.fill, { r: 170, g: 187, b: 204, a: 221 / 255 });
  assertSrgb(byName.rgba8.fill, { r: 170, g: 187, b: 204, a: 221 / 255 });
});

test('custom primitives lift compositing fields and render them in svg', () => {
  const resolver = createResolver(`
  {
    view:[10,10];
    graphics:[
      {
        type:"heatmap";
        opacity:0.5;
        blendMode:"multiply";
        palette:"thermal";
        graphics:[
          { type:"rect"; position:[0,0]; size:[10,10]; fill:"#93c5fd"; stroke:"none"; width:0; }
        ];
      }
    ];
  }
  `);

  const expression = createExpression(resolver);
  const result = expression.evaluate({ output: ['raw', 'svg'] });
  assert.equal(result.warnings.length, 0);

  const custom = result.raw.graphics[0];
  assert.equal(custom.type, 'custom');
  assert.equal(custom.name, 'heatmap');
  assert.equal(custom.opacity, 0.5);
  assert.equal(custom.blendMode, 'multiply');
  assert.equal(custom.props.opacity, undefined);
  assert.equal(custom.props.blendMode, undefined);

  assert.match(result.svg, /data-custom="heatmap"/);
  assert.match(result.svg, /opacity="0\.5"/);
  assert.match(result.svg, /mix-blend-mode:\s*multiply/);
});

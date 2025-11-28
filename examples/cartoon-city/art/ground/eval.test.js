const test = require('node:test');
const assert = require('node:assert/strict');
const ground = require('./eval');

test('ground model paints default field and road', () => {
  const result = ground();
  assert.equal(result.view.left, 0);
  assert.equal(result.view.top, 40);
  assert.equal(result.groundLevel, 4);
  assert.ok(Array.isArray(result.graphics));
  assert.ok(result.graphics.length > 0);

  assert.equal(result.road.size[1] > 0, true);
  assert.equal(result.field.bottom, 0);
  assert.equal(result.field.top > result.field.bottom, true);
});

test('ground model respects overrides', () => {
  const overrides = {
    view: { left: -10, bottom: -2, right: 60, top: 30 },
    groundLevel: 5,
    fieldDepth: 10,
    roadWidth: 8,
    roadOffset: 7,
    groundLineColor: '#000'
  };
  const result = ground(overrides);

  assert.deepEqual(result.view, overrides.view);
  assert.equal(result.groundLevel, 5);
  assert.equal(result.field.top, overrides.groundLevel + overrides.fieldDepth);
  assert.equal(result.road.position[0], overrides.view.left);
  assert.equal(result.road.size[0], overrides.view.right - overrides.view.left);
  assert.equal(result.road.size[1], 8);
});

const test = require('node:test');
const assert = require('node:assert/strict');
const skyline = require('./eval');

function createHelpers() {
  const houseCalls = [];
  const treeCalls = [];

  return {
    helpers: {
      house: (settings) => {
        houseCalls.push(settings);
        return { graphics: [{ type: 'houseStub', position: settings.position }], settings };
      },
      tree: (settings) => {
        treeCalls.push(settings);
        return { graphics: [{ type: 'treeStub', position: settings.position }], settings };
      }
    },
    houseCalls,
    treeCalls
  };
}

test('skyline builds default houses and trees with setback', () => {
  const { helpers, houseCalls, treeCalls } = createHelpers();
  const result = skyline({ helpers, groundLevel: 4 });

  assert.equal(result.houses.length, 3);
  assert.equal(result.trees.length, 3);

  assert.ok(houseCalls.every((call) => call.position[1] > 10), 'houses should be set back above the road');
  assert.ok(treeCalls.every((call) => call.position[1] > 9));
  assert.ok(result.graphics.length > 0);
});

test('skyline respects custom house/tree configurations', () => {
  const { helpers, houseCalls, treeCalls } = createHelpers();
  const overrides = {
    groundLevel: 2,
    houses: [
      { type: 'modern', position: [5, 12], width: 10 },
      { type: 'cottage', position: [30, 12], width: 15 }
    ],
    trees: [
      { type: 'round', position: [0, 11], height: 12 },
      { type: 'pine', position: [50, 11], height: 18 }
    ],
    helpers
  };

  const result = skyline(overrides);
  assert.equal(houseCalls.length, 2);
  assert.deepEqual(houseCalls[0].position, [5, 12]);
  assert.equal(treeCalls.length, 2);
  assert.deepEqual(treeCalls[1].position, [50, 11]);
  assert.equal(result.houses.length, 2);
  assert.equal(result.trees.length, 2);
});

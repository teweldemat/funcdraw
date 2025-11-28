const test = require('node:test');
const assert = require('node:assert/strict');
const sky = require('./eval');

function createStubHelpers(overrides = {}) {
  const sunCalls = [];
  const cloudCalls = [];

  return {
    helpers: {
      sun: (settings) => {
        sunCalls.push(settings);
        return {
          graphics: [{ type: 'sunStub', center: settings.position }],
          rays: settings.rays,
          center: settings.position,
          settings
        };
      },
      cloud: (settings) => {
        cloudCalls.push(settings);
        return {
          graphics: [{ type: 'cloudStub', center: settings.position }],
          anchor: settings.position,
          width: settings.width,
          settings
        };
      },
      ...overrides
    },
    sunCalls,
    cloudCalls
  };
}

test('sky model emits default view, background, and helper payloads', () => {
  const { helpers, sunCalls, cloudCalls } = createStubHelpers();
  const result = sky({ helpers });

  assert.equal(result.view.left, 0);
  assert.equal(result.view.top, 40);
  assert.equal(result.background.fill, '#bfdbfe');
  assert.ok(result.graphics.length > 0);

  assert.equal(sunCalls.length, 1);
  assert.deepEqual(sunCalls[0].position, [64, 34]);
  assert.equal(sunCalls[0].rays, 14);

  assert.equal(cloudCalls.length, 3);
  assert.deepEqual(cloudCalls[1].position, [40, 30]);
});

test('sky model respects overrides when stubs are provided', () => {
  const { helpers, sunCalls, cloudCalls } = createStubHelpers();
  const overrides = {
    view: { left: -10, bottom: 2, right: 70, top: 45 },
    backgroundColor: '#93c5fd',
    sun: { position: [10, 33], rays: 9 },
    clouds: {
      left: { position: [-6, 38], width: 14 },
      center: { position: [15, 32], width: 18 },
      right: { position: [58, 36], width: 12 }
    },
    helpers
  };

  const result = sky(overrides);
  assert.deepEqual(result.view, overrides.view);
  assert.equal(result.background.fill, '#93c5fd');

  assert.deepEqual(sunCalls[0].position, [10, 33]);
  assert.equal(sunCalls[0].rays, 9);
  assert.deepEqual(result.sun.center, [10, 33]);

  assert.deepEqual(
    cloudCalls.map((call) => call.position),
    [
      [-6, 38],
      [15, 32],
      [58, 36]
    ]
  );
});

test('sky model scales helper defaults with large views', () => {
  const { helpers, sunCalls, cloudCalls } = createStubHelpers();
  const view = { left: 0, bottom: 0, right: 400, top: 200 };
  const result = sky({ view, helpers });

  assert.deepEqual(result.view, view);
  assert.deepEqual(sunCalls[0].position, [320, 170]);
  assert.equal(sunCalls[0].radius, 26);
  assert.equal(cloudCalls[0].width, 90);
  assert.equal(cloudCalls[1].width, 110);
  assert.equal(cloudCalls[2].width, 80);
});

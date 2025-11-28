const DEFAULT_VIEW = { left: 0, bottom: 0, right: 80, top: 40 };
const DEFAULT_BACKGROUND = '#4ade80';
const DEFAULT_SECONDARY = '#86efac';
const DEFAULT_ROAD = '#475569';
const DEFAULT_ROAD_EDGE = '#cbd5f5';

function ensureObject(value, fallback = {}) {
  return value && typeof value === 'object' && !Array.isArray(value) ? value : fallback;
}

function toNumber(value, fallback) {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }
  if (value == null) {
    return fallback;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function resolveView(rawView) {
  const view = ensureObject(rawView, {});
  return {
    left: toNumber(view.left, DEFAULT_VIEW.left),
    bottom: toNumber(view.bottom, DEFAULT_VIEW.bottom),
    right: toNumber(view.right, DEFAULT_VIEW.right),
    top: toNumber(view.top, DEFAULT_VIEW.top)
  };
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function createFieldRects(view, bottom, height, colors) {
  const laneHeight = height * 0.45;
  const secondBandBottom = bottom + height * 0.35;
  const stripes = [];
  const stripeWidth = (view.right - view.left) / 12;
  for (let index = 0; index < 12; index += 1) {
    const startX = view.left + index * stripeWidth;
    const stripe = {
      type: 'rect',
      position: [startX, secondBandBottom],
      size: [stripeWidth * 0.6, laneHeight],
      fill: colors.secondary,
      stroke: colors.secondary,
      opacity: 0.65
    };
    if (index % 2 === 1) {
      stripe.opacity = 0.35;
    }
    stripes.push(stripe);
  }

  return [
    {
      type: 'rect',
      position: [view.left, bottom],
      size: [view.right - view.left, height],
      fill: colors.primary,
      stroke: colors.primary
    },
    ...stripes
  ];
}

function createRoadRect(view, centerY, thickness, color) {
  return {
    type: 'rect',
    position: [view.left, centerY - thickness / 2],
    size: [view.right - view.left, thickness],
    fill: color,
    stroke: color
  };
}

function createRoadShoulders(view, centerY, thickness, color) {
  const width = view.right - view.left;
  const shoulderThickness = Math.max(thickness * 0.08, 0.2);
  return [
    {
      type: 'line',
      from: [view.left, centerY - thickness / 2 + shoulderThickness],
      to: [view.right, centerY - thickness / 2 + shoulderThickness],
      stroke: color,
      width: shoulderThickness
    },
    {
      type: 'line',
      from: [view.left, centerY + thickness / 2 - shoulderThickness],
      to: [view.right, centerY + thickness / 2 - shoulderThickness],
      stroke: color,
      width: shoulderThickness
    }
  ];
}

function createCenterStripes(view, centerY, thickness, options) {
  const dashWidth = Math.max(toNumber(options.dashWidth, 2.5), 0.5);
  const dashGap = Math.max(toNumber(options.gap, 3), 0.5);
  const roadWidth = view.right - view.left;
  const margin = Math.max(
    toNumber(options.margin, Math.min(roadWidth * 0.015, 0.5)),
    0
  );
  const stripeHeight = Math.max(thickness * 0.15, 0.25);
  const stripeColor = options.color ?? '#e2e8f0';
  const limit = view.right - margin;
  const targetCount = options.count != null ? clamp(Math.round(options.count), 1, 64) : null;

  const stripes = [];
  let cursor = view.left + margin;
  let iterations = 0;
  const maxIterations = 128;

  while (cursor < limit && iterations < maxIterations) {
    if (targetCount != null && iterations >= targetCount) {
      break;
    }
    const remaining = limit - cursor;
    const width = Math.min(dashWidth, remaining);
    stripes.push({
      type: 'rect',
      position: [cursor, centerY - stripeHeight / 2],
      size: [width, stripeHeight],
      fill: stripeColor,
      stroke: stripeColor
    });
    cursor += dashWidth + dashGap;
    iterations += 1;
  }
  return stripes;
}

function ground(optionsInput = {}) {
  const options = ensureObject(optionsInput, {});
  const view = resolveView(options.view);
  const fieldDepth = clamp(toNumber(options.fieldDepth, 14), 4, view.top - view.bottom);
  const groundLevel = clamp(toNumber(options.groundLevel, 4), view.bottom, view.top);
  const fieldBottom = view.bottom;
  const fieldTop = clamp(groundLevel + fieldDepth, groundLevel, view.top);
  const roadThickness = clamp(toNumber(options.roadWidth, 6), 2, fieldTop - fieldBottom);
  const roadCenterOffset = clamp(toNumber(options.roadOffset, roadThickness * 1.5), roadThickness / 2, fieldTop - roadThickness / 2);
  const roadCenterY = fieldBottom + roadCenterOffset;

  const colors = {
    primary: options.fieldColor || DEFAULT_BACKGROUND,
    secondary: options.fieldAccentColor || DEFAULT_SECONDARY,
    road: options.roadColor || DEFAULT_ROAD,
    stripe: options.roadStripeColor || DEFAULT_ROAD_EDGE,
    shoulder: options.roadShoulderColor || DEFAULT_ROAD_EDGE
  };

  const fieldRects = createFieldRects(view, fieldBottom, fieldTop - fieldBottom, colors);
  const roadRect = createRoadRect(view, roadCenterY, roadThickness, colors.road);
  const shoulders = createRoadShoulders(view, roadCenterY, roadThickness, colors.shoulder);
  const stripes = createCenterStripes(view, roadCenterY, roadThickness, {
    count: options.roadStripeCount,
    dashWidth: options.roadStripeLength,
    gap: options.roadStripeGap,
    color: colors.stripe,
    margin: options.roadStripeMargin
  });

  const groundLine = {
    type: 'line',
    from: [view.left, groundLevel],
    to: [view.right, groundLevel],
    stroke: options.groundLineColor || '#1f2937',
    width: Math.max((options.groundLineWidth ?? 0.4), 0.2)
  };

  const graphics = [...fieldRects, roadRect, ...shoulders, ...stripes, groundLine];

  const result = {
    view,
    groundLevel,
    road: roadRect,
    field: {
      bottom: fieldBottom,
      top: fieldTop
    },
    graphics
  };

  return result;
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = ground;
}

return ground;

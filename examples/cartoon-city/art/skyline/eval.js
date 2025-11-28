const DEFAULT_VIEW = { left: 0, bottom: 0, right: 80, top: 40 };
const DEFAULT_SETBACK = 8;

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

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
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

function ensureGraphics(value) {
  return Array.isArray(value) ? value : [];
}

function sanitizeResult(value) {
  return value && typeof value === 'object' ? value : { graphics: [] };
}

function selectBuilder(source, fallbackBuilder) {
  return typeof source === 'function' ? source : fallbackBuilder;
}

function fallbackHouseBuilder() {
  return { graphics: [] };
}

function fallbackTreeBuilder() {
  return { graphics: [] };
}

function resolveBuilders(overrides) {
  const helperOverrides = ensureObject(overrides, {});
  const packageFn = typeof globalThis.package === 'function' ? globalThis.package : null;
  let cartoonNamespace = null;

  if (packageFn) {
    try {
      const lib = packageFn('@funcdraw/testlib');
      cartoonNamespace = lib && lib.cartoon;
    } catch {
      cartoonNamespace = null;
    }
  }

  const defaultHouse = cartoonNamespace && cartoonNamespace.house;
  const defaultTree = cartoonNamespace && cartoonNamespace.tree;

  return {
    house: selectBuilder(helperOverrides.house ?? defaultHouse, fallbackHouseBuilder),
    tree: selectBuilder(helperOverrides.tree ?? defaultTree, fallbackTreeBuilder)
  };
}

function createHouseConfigs(view, groundLevel, options) {
  const width = view.right - view.left;
  const ratios = options.houseRatios ?? [0.2, 0.45, 0.8];
  const types = options.houseTypes ?? ['classic', 'modern', 'cottage'];
  const widths = options.houseWidths ?? [14, 18, 12];
  const yOffsets = options.houseYOffset ?? [0, -0.6, 0];

  const fallback = ratios.length > 0 ? ratios : [0.5];
  const maxBase = view.top - 4;
  const baseY = clamp(groundLevel + options.setback, groundLevel + 2, maxBase);

  return fallback.map((ratio, index) => {
    const houseType = types[index % types.length];
    const widthValue = widths[index % widths.length];
    const yOffset = yOffsets[index % yOffsets.length] ?? 0;
    return {
      type: houseType,
      position: [view.left + width * clamp(ratio, 0.05, 0.95), baseY + yOffset],
      width: widthValue
    };
  });
}

function createTreeConfigs(view, groundLevel, options) {
  const width = view.right - view.left;
  const ratios = options.treeRatios ?? [0.1, 0.55, 0.9];
  const types = options.treeTypes ?? ['round', 'pine', 'column'];
  const heights = options.treeHeights ?? [16, 18, 14];
  const baseY = clamp(groundLevel + options.setback - 0.5, groundLevel + 1, view.top - 3);

  const fallback = ratios.length > 0 ? ratios : [0.35];
  return fallback.map((ratio, index) => {
    const treeType = types[index % types.length];
    const height = heights[index % heights.length];
    return {
      type: treeType,
      position: [view.left + width * clamp(ratio, 0.05, 0.95), baseY],
      height
    };
  });
}

function instantiateItems(configs, builder) {
  return configs.map((config) => sanitizeResult(builder(config)));
}

function skyline(optionsInput = {}) {
  const options = ensureObject(optionsInput, {});
  const view = resolveView(options.view);
  const groundLevel = toNumber(options.groundLevel, 4);
  const builders = resolveBuilders(options.helpers);
  const setback = toNumber(options.setback, DEFAULT_SETBACK);

  const customHouses = Array.isArray(options.houses) && options.houses.length ? options.houses : null;
  const customTrees = Array.isArray(options.trees) && options.trees.length ? options.trees : null;

  const houseConfigs = customHouses ?? createHouseConfigs(view, groundLevel, { ...options, setback });
  const treeConfigs = customTrees ?? createTreeConfigs(view, groundLevel, { ...options, setback });

  const houses = instantiateItems(houseConfigs, builders.house);
  const trees = instantiateItems(treeConfigs, builders.tree);

  const graphics = [
    ...houses.flatMap((item) => ensureGraphics(item.graphics)),
    ...trees.flatMap((item) => ensureGraphics(item.graphics))
  ];

  return {
    view,
    groundLevel,
    setback,
    houses,
    trees,
    graphics
  };
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = skyline;
}

return skyline;

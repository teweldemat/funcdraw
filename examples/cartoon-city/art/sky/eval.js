const DEFAULT_VIEW = { left: 0, bottom: 0, right: 80, top: 40 };
const DEFAULT_BACKGROUND = '#bfdbfe';
const REFERENCE_VIEW = {
  width: DEFAULT_VIEW.right - DEFAULT_VIEW.left,
  height: DEFAULT_VIEW.top - DEFAULT_VIEW.bottom
};
const PI_APPROX = typeof Math === 'object' && Number.isFinite(Math.PI) ? Math.PI : 3.141592653589793;
const TAU_APPROX = PI_APPROX * 2;
const DEG_TO_RAD = PI_APPROX / 180;

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

function normalizePoint(value, fallback) {
  const base = Array.isArray(fallback) && fallback.length >= 2 ? fallback : [0, 0];
  if (Array.isArray(value) && value.length >= 2) {
    return [toNumber(value[0], base[0]), toNumber(value[1], base[1])];
  }
  return base.slice();
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

function computeScale(view) {
  const width = Math.max(view.right - view.left, 1);
  const height = Math.max(view.top - view.bottom, 1);
  return {
    x: width / REFERENCE_VIEW.width,
    y: height / REFERENCE_VIEW.height
  };
}

function mergePalette(defaultPalette = {}, overridePalette) {
  const overrides = ensureObject(overridePalette, null);
  if (!overrides) {
    return { ...defaultPalette };
  }
  return { ...defaultPalette, ...overrides };
}

function resolveSunOptions(view, rawOverrides) {
  const scale = computeScale(view);
  const defaults = {
    position: [view.right - 16 * scale.x, view.top - 6 * scale.y],
    radius: Math.max(5.2 * scale.x, 1.5),
    rays: 14,
    rayLength: Math.max(7.28 * scale.x, 2),
    palette: {
      core: '#fde047',
      outline: '#f59e0b',
      rays: '#fb923c',
      glow: '#fefce8'
    }
  };

  const overrides = ensureObject(rawOverrides, null);
  if (!overrides) {
    return defaults;
  }

  const merged = { ...defaults, ...overrides };
  merged.position = normalizePoint(overrides.position, defaults.position);
  merged.palette = mergePalette(defaults.palette, overrides.palette);
  return merged;
}

function createCloudDefaults(view, scale) {
  return {
    left: {
      position: [view.left + 18 * scale.x, view.top - 7 * scale.y],
      width: 18 * scale.x,
      lobes: 5,
      puffiness: 1.1
    },
    center: {
      position: [(view.left + view.right) / 2, view.top - 10 * scale.y],
      width: 22 * scale.x,
      lobes: 4
    },
    right: {
      position: [view.right - 10 * scale.x, view.top - 9 * scale.y],
      width: 16 * scale.x,
      lobes: 3,
      palette: {
        fill: '#e0f2fe',
        stroke: '#bae6fd',
        highlight: '#f8fafc'
      }
    }
  };
}

function mergeCloudOptions(defaultOptions, overrideOptions) {
  const overrides = ensureObject(overrideOptions, null);
  if (!overrides) {
    return { ...defaultOptions };
  }

  const merged = { ...defaultOptions, ...overrides };
  merged.position = normalizePoint(overrides.position, defaultOptions.position);
  if (defaultOptions.palette || overrides.palette) {
    merged.palette = mergePalette(defaultOptions.palette || {}, overrides.palette);
  }

  return merged;
}

function resolveCloudOptions(view, rawOverrides) {
  const overrides = ensureObject(rawOverrides, {});
  const scale = computeScale(view);
  const defaults = createCloudDefaults(view, scale);
  return {
    left: mergeCloudOptions(defaults.left, overrides.left),
    center: mergeCloudOptions(defaults.center, overrides.center),
    right: mergeCloudOptions(defaults.right, overrides.right)
  };
}

function selectBuilder(source, fallbackBuilder) {
  return typeof source === 'function' ? source : fallbackBuilder;
}

function fallbackSunBuilder(settings = {}) {
  const position = normalizePoint(settings.position, [0, 0]);
  const radius = Math.max(toNumber(settings.radius, 4.2), 1);
  const palette = mergePalette(
    {
      core: '#fde047',
      outline: '#f59e0b',
      rays: '#fb923c',
      glow: '#fef08a'
    },
    settings.palette
  );
  const rayCount = Math.max(4, Math.min(24, Math.round(toNumber(settings.rays, 12))));
  const rayLength = Math.max(toNumber(settings.rayLength, radius * 1.2), radius * 0.5);
  const rotation = toNumber(settings.rotation, 0) * DEG_TO_RAD;

  const glow = {
    type: 'circle',
    center: position,
    radius: radius * 1.3,
    fill: palette.glow,
    stroke: palette.glow,
    width: Math.max(radius * 0.08, 0.2),
    opacity: 0.25
  };

  const rays = [];
  for (let index = 0; index < rayCount; index += 1) {
    const angle = rotation + (index / rayCount) * TAU_APPROX;
    const inner = radius * 0.6;
    const outer = radius + rayLength;
    rays.push({
      type: 'line',
      from: [position[0] + Math.cos(angle) * inner, position[1] + Math.sin(angle) * inner],
      to: [position[0] + Math.cos(angle) * outer, position[1] + Math.sin(angle) * outer],
      stroke: palette.rays,
      width: Math.max(radius * 0.12, 0.3)
    });
  }

  const core = {
    type: 'circle',
    center: position,
    radius,
    fill: palette.core,
    stroke: palette.outline,
    width: Math.max(radius * 0.3, 0.4)
  };

  return {
    graphics: [glow, ...rays, core],
    rays: rayCount,
    center: position,
    settings
  };
}

function fallbackCloudBuilder(settings = {}) {
  const position = normalizePoint(settings.position, [0, 0]);
  const width = Math.max(toNumber(settings.width, 18), 4);
  const puffiness = Math.max(toNumber(settings.puffiness ?? settings.heightFactor, 1), 0.5);
  const palette = mergePalette(
    {
      fill: '#f1f5f9',
      stroke: '#cbd5f5',
      highlight: '#ffffff'
    },
    settings.palette
  );
  const lobes = Math.max(3, Math.min(6, Math.round(toNumber(settings.lobes, 4))));
  const height = width * 0.35 * puffiness;
  const graphics = [
    {
      type: 'ellipse',
      center: [position[0], position[1] + height * 0.1],
      radiusX: width * 0.55,
      radiusY: height * 0.45,
      fill: palette.fill,
      stroke: palette.stroke,
      width: Math.max(width * 0.04, 0.25)
    }
  ];

  for (let index = 0; index < lobes; index += 1) {
    const progress = lobes === 1 ? 0.5 : index / (lobes - 1);
    const offsetX = (progress - 0.5) * width * 0.7;
    const bulge = 1 - Math.abs(progress - 0.5) * 1.5;
    const radius = width * 0.18 * (1 + bulge * 0.4);
    const centerY = position[1] + height * (0.2 + bulge * 0.2);
    graphics.push({
      type: 'circle',
      center: [position[0] + offsetX, centerY],
      radius,
      fill: palette.fill,
      stroke: palette.stroke,
      width: Math.max(radius * 0.3, 0.2)
    });
  }

  graphics.push({
    type: 'ellipse',
    center: [position[0] - width * 0.2, position[1] + height * 0.2],
    radiusX: width * 0.3,
    radiusY: height * 0.22,
    fill: palette.highlight,
    stroke: palette.highlight,
    width: Math.max(width * 0.015, 0.1),
    opacity: 0.7
  });

  return {
    graphics,
    anchor: position,
    width,
    height,
    settings
  };
}

function ensureGraphics(value) {
  return Array.isArray(value) ? value : [];
}

function sanitizeResult(value) {
  return value && typeof value === 'object' ? value : { graphics: [] };
}

function resolveHelpers(overrides) {
  const helperOverrides = ensureObject(overrides, {});
  const packageFn = typeof globalThis.package === 'function' ? globalThis.package : null;
  let cartoonNamespace = null;

  if (packageFn) {
    try {
      const cartoonLib = packageFn('@funcdraw/testlib');
      cartoonNamespace = cartoonLib && cartoonLib.cartoon;
    } catch {
      cartoonNamespace = null;
    }
  }

  const defaultSun = cartoonNamespace && cartoonNamespace.sun;
  const defaultCloud = cartoonNamespace && cartoonNamespace.cloud;

  return {
    sun: selectBuilder(helperOverrides.sun ?? defaultSun, fallbackSunBuilder),
    cloud: selectBuilder(helperOverrides.cloud ?? defaultCloud, fallbackCloudBuilder)
  };
}

function sky(optionsInput = {}) {
  const options = ensureObject(optionsInput, {});
  const view = resolveView(options.view);
  const backgroundColor = typeof options.backgroundColor === 'string' ? options.backgroundColor : DEFAULT_BACKGROUND;
  const helpers = resolveHelpers(options.helpers);

  const sunOptions = resolveSunOptions(view, options.sun);
  const sunArtwork = sanitizeResult(helpers.sun(sunOptions));

  const cloudOptions = resolveCloudOptions(view, options.clouds);
  const clouds = [
    sanitizeResult(helpers.cloud(cloudOptions.left)),
    sanitizeResult(helpers.cloud(cloudOptions.center)),
    sanitizeResult(helpers.cloud(cloudOptions.right))
  ];

  const background = {
    type: 'rect',
    position: [view.left, view.bottom],
    size: [view.right - view.left, view.top - view.bottom],
    fill: backgroundColor,
    stroke: backgroundColor
  };

  const graphics = [
    background,
    ...ensureGraphics(sunArtwork.graphics),
    ...clouds.flatMap((cloud) => ensureGraphics(cloud.graphics))
  ];

  const result = {
    view,
    background,
    sun: sunArtwork,
    clouds,
    graphics
  };

  return result;
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = sky;
}

return sky;

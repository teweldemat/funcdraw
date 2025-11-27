function normalizeCenter(center) {
  if (Array.isArray(center) && center.length >= 2) {
    if (typeof center[0] === "number" && typeof center[1] === "number") {
      return [center[0], center[1]];
    }
    return [toNumber(center[0], 0), toNumber(center[1], 0)];
  }
  if (center && typeof center[Symbol.iterator] === "function") {
    const items = [];
    for (const value of center) {
      items.push(value);
      if (items.length === 2) {
        break;
      }
    }
    if (items.length === 2) {
      return [toNumber(items[0], 0), toNumber(items[1], 0)];
    }
  }
  if (center && typeof center === "object") {
    if ("x" in center && "y" in center) {
      return [toNumber(center.x, 0), toNumber(center.y, 0)];
    }
    if ("left" in center && "top" in center) {
      return [toNumber(center.left, 0), toNumber(center.top, 0)];
    }
  }
  return [0, 0];
}

function normalizeSide(length) {
  return toNumber(length, 10);
}

function normalizeStyle(style) {
  if (!style || typeof style !== "object") {
    return {};
  }
  const valueFor = (key) => {
    if (Object.prototype.hasOwnProperty.call(style, key)) {
      return style[key];
    }
    const altKey = key.charAt(0).toUpperCase() + key.slice(1);
    if (Object.prototype.hasOwnProperty.call(style, altKey)) {
      return style[altKey];
    }
    if (typeof style.get === "function") {
      try {
        const fetched = style.get(key);
        if (fetched !== undefined && fetched !== null) {
          return fetched;
        }
      } catch {
        // ignore provider lookup failures
      }
    }
    return undefined;
  };
  return {
    fill: valueFor("fill"),
    stroke: valueFor("stroke"),
    width: valueFor("width")
  };
}

function toPlainValue(value) {
  if (value === null || value === undefined) {
    return undefined;
  }
  if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
    return value;
  }
  if (Array.isArray(value) && value.length === 2 && typeof value[0] === "number") {
    return toPlainValue(value[1]);
  }
  if (value && typeof value.valueOf === "function") {
    const raw = value.valueOf();
    if (raw !== value) {
      return toPlainValue(raw);
    }
  }
  return value;
}

function toNumber(value, fallback = 0) {
  const targetFallback = fallback;
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (Array.isArray(value)) {
    if (value.length === 2 && typeof value[0] === "number") {
      return toNumber(value[1], targetFallback);
    }
    if (value.length > 0) {
      return toNumber(value[0], targetFallback);
    }
  }
  if (value && typeof value.valueOf === "function") {
    const raw = value.valueOf();
    if (raw !== value) {
      return toNumber(raw, targetFallback);
    }
  }
  const coerced = Number(value);
  return Number.isFinite(coerced) ? coerced : targetFallback;
}

function square(center = [0, 0], sideLength = 10, style = {}) {
  const [centerX, centerY] = normalizeCenter(center);
  const side = normalizeSide(sideLength);
  const normalizedStyle = normalizeStyle(style);
  const halfSide = side / 2;
  const fill = toPlainValue(normalizedStyle.fill) ?? "#38bdf8";
  const stroke = toPlainValue(normalizedStyle.stroke) ?? "#0f172a";
  const width = toNumber(normalizedStyle.width, undefined) ?? 0.5;

  return {
    type: "rect",
    position: [centerX - halfSide, centerY - halfSide],
    size: [side, side],
    fill,
    stroke,
    width
  };
}

return square;

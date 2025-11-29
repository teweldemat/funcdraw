function toPoint(value) {
  if (Array.isArray(value) && value.length >= 2) {
    const x = Number(value[0]);
    const y = Number(value[1]);
    return [Number.isFinite(x) ? x : 0, Number.isFinite(y) ? y : 0];
  }
  if (value && typeof value === "object") {
    if ("x" in value || "y" in value) {
      const x = Number(value.x);
      const y = Number(value.y);
      return [Number.isFinite(x) ? x : 0, Number.isFinite(y) ? y : 0];
    }
    if ("left" in value || "top" in value) {
      const x = Number(value.left);
      const y = Number(value.top);
      return [Number.isFinite(x) ? x : 0, Number.isFinite(y) ? y : 0];
    }
  }
  return null;
}

return toPoint;

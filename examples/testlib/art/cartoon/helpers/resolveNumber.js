function resolveNumber(value, fallback) {
  if (value == null) {
    return fallback;
  }
  const numeric = typeof value === "number" ? value : Number(value);
  return Number.isFinite(numeric) ? numeric : fallback;
}

return resolveNumber;

function resolveOptionalNumber(value) {
  if (value == null) {
    return null;
  }
  const numeric = typeof value === "number" ? value : Number(value);
  return Number.isFinite(numeric) ? numeric : null;
}

return resolveOptionalNumber;

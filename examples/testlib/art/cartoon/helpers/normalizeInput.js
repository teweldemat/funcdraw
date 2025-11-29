function normalizeInput(value, fallback) {
  return value != null && typeof value === "object" ? value : fallback;
}

return normalizeInput;

function resolveBoolean(value, fallback) {
  return typeof value === "boolean" ? value : fallback;
}

return resolveBoolean;

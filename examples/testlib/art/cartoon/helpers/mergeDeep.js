function mergeDeep(target, source) {
  if (!source || typeof source !== "object") {
    return target;
  }
  const output = Array.isArray(target) ? target.slice() : { ...target };
  for (const [key, value] of Object.entries(source)) {
    if (value && typeof value === "object" && !Array.isArray(value)) {
      const base =
        Object.prototype.hasOwnProperty.call(output, key) && typeof output[key] === "object"
          ? output[key]
          : Array.isArray(value)
            ? []
            : {};
      output[key] = mergeDeep(base, value);
    } else {
      output[key] = value;
    }
  }
  return output;
}

return mergeDeep;

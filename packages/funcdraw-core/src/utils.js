'use strict';

function clamp(number, min, max, fallback) {
  if (typeof number !== 'number' || Number.isNaN(number)) {
    return fallback ?? min;
  }
  if (number < min) {
    return min;
  }
  if (number > max) {
    return max;
  }
  return number;
}

function toArray(iterable) {
  if (!iterable) {
    return [];
  }
  if (Array.isArray(iterable)) {
    return iterable.slice();
  }
  if (typeof iterable[Symbol.iterator] === 'function') {
    return Array.from(iterable);
  }
  if (typeof iterable.toArray === 'function') {
    return iterable.toArray();
  }
  return [];
}

function omitKeys(source, keys) {
  if (!source || typeof source !== 'object') {
    return {};
  }
  const set = new Set(keys || []);
  const result = {};
  for (const [key, value] of Object.entries(source)) {
    if (!set.has(key)) {
      result[key] = value;
    }
  }
  return result;
}

module.exports = {
  clamp,
  toArray,
  omitKeys
};

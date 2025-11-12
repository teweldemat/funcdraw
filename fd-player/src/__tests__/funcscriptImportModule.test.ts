import { describe, expect, it, vi } from 'vitest';
vi.mock('../App.css', () => ({}));
import { applyProjectImportBindings } from '../importModuleFunction';
import { evaluateExpression, prepareProvider } from '../graphics';

const SAMPLE_MODULE = {
  car: {
    type: 'rect',
    width: 6,
    height: 2
  },
  wheels: [
    { radius: 2, position: [-4, -1] },
    { radius: 2, position: [4, -1] }
  ]
};

describe('project import bindings', () => {
  it('exposes project modules to FuncScript expressions', () => {
    const provider = prepareProvider();
    applyProjectImportBindings(provider, (specifier) => {
      if (specifier === 'common-graphic') {
        return SAMPLE_MODULE;
      }
      throw new Error(`Unknown module ${String(specifier)}`);
    });

    const expression = `{
  module: import("common-graphic");
  return module;
}`;
    const result = evaluateExpression(provider, expression, 'funcscript');
    expect(result.error).toBeNull();
    expect(result.value).toEqual({
      car: SAMPLE_MODULE.car,
      wheels: SAMPLE_MODULE.wheels
    });
  });

  it('exposes fdimport inside JavaScript expressions', () => {
    const provider = prepareProvider();
    applyProjectImportBindings(provider, (specifier) => {
      if (specifier === 'common-graphic') {
        return SAMPLE_MODULE;
      }
      throw new Error(`Unknown module ${String(specifier)}`);
    });

    const expression = `{
  const module = fdimport("common-graphic");
  return module.wheels.length;
}`;
    const result = evaluateExpression(provider, expression, 'javascript');
    expect(result.error).toBeNull();
    expect(result.value).toBe(2);
  });
});

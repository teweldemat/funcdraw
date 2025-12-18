'use strict';

function createParameterListClass(ParameterList) {
  return class InlineParameterList extends ParameterList {
    constructor(values) {
      super();
      this.values = Array.isArray(values) ? values : [];
    }

    get count() {
      return this.values.length;
    }

    getParameter(_, index) {
      return this.values[index];
    }
  };
}

function createStepFunctionWrapper(fsFunction, context) {
  if (!fsFunction || typeof fsFunction.evaluate !== 'function') {
    return null;
  }

  const {
    engine,
    providerFactory,
    converter
  } = context;
  const InlineParameterList = createParameterListClass(engine.ParameterList);

  return (...args) => {
    const provider = providerFactory();
    const typedArgs = args.map((arg) => engine.normalize(arg));
    const params = new InlineParameterList(typedArgs);
    const typedResult = fsFunction.evaluate(provider, params);
    return converter.toPlain(typedResult);
  };
}

module.exports = {
  createStepFunctionWrapper
};

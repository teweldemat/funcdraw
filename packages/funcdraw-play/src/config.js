'use strict';

const path = require('path');
const { createResolverFromExpression } = require('./resolver');
const { createArtResolver } = require('./art-resolver');

const SAMPLE_EXPRESSION = `
{
  view:{
    left:0;
    bottom:0;
    right:24;
    top:24;
  };
  graphics:[
    {
      type:"line";
      from:[0,0];
      to:[24,24];
      stroke:"#38bdf8";
      width:0.5;
    },
    {
      type:"text";
      text:"FuncDraw";
      position:[4,6];
      fontSize:4;
      color:"#e2e8f0";
    }
  ];
}
`;

async function loadUserConfig(cwd) {
  const artResolver = createArtResolver(cwd);
  if (artResolver) {
    const relativeArtPath = path.relative(cwd, artResolver.watchPath) || artResolver.watchPath;
    return {
      resolver: artResolver.resolver,
      options: {},
      configPath: null,
      watchPaths: [artResolver.watchPath],
      sourceDescription: `art directory (${relativeArtPath})`
    };
  }
  return {
    resolver: createResolverFromExpression(SAMPLE_EXPRESSION),
    options: {},
    configPath: null,
    watchPaths: [],
    sourceDescription: 'inline sample FuncScript expression'
  };
}

module.exports = {
  loadUserConfig,
  SAMPLE_EXPRESSION
};

'use strict';

const path = require('path');

async function bundleBrowserRuntime() {
  const esbuild = require('esbuild');
  const entry = path.resolve(__dirname, 'client/runtime-entry.js');

  const result = await esbuild.build({
    entryPoints: [entry],
    bundle: true,
    write: false,
    platform: 'browser',
    format: 'iife',
    globalName: 'FuncDrawPlayRuntime',
    sourcemap: false,
    target: ['es2020']
  });

  const output = result && result.outputFiles && result.outputFiles.length > 0 ? result.outputFiles[0] : null;
  if (!output || !output.text) {
    throw new Error('Failed to bundle browser runtime');
  }
  return output.text;
}

module.exports = {
  bundleBrowserRuntime
};


#!/usr/bin/env node

'use strict';

const { sharePackage } = require('../src/share');

sharePackage(process.cwd()).catch((error) => {
  console.error('[funcdraw-share] Unexpected error:', error && error.message ? error.message : error);
  process.exitCode = 1;
});


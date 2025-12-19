#!/usr/bin/env node

'use strict';

const { sharePackage, login } = require('../src/share');

const argv = process.argv.slice(2);
if (argv[0] === 'login') {
  login(argv.slice(1)).catch((error) => {
    console.error('[fd-share] Login error:', error && error.message ? error.message : error);
    process.exitCode = 1;
  });
} else {
  sharePackage(process.cwd(), argv, { toolName: 'fd-share' }).catch((error) => {
    console.error('[fd-share] Unexpected error:', error && error.message ? error.message : error);
    process.exitCode = 1;
  });
}

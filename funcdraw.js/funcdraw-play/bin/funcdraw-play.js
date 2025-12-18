#!/usr/bin/env node

'use strict';

const { startPlayer } = require('../src');

startPlayer(process.cwd()).catch((error) => {
  console.error('[funcdraw-play] Unexpected error:', error);
  process.exitCode = 1;
});

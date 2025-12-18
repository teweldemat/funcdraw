'use strict';

const express = require('express');
const picocolors = require('picocolors');
const { createPreviewRouter } = require('./preview-router');

async function startServer({
  evaluateScene,
  getBootstrap,
  runtimeSource,
  fontPath,
  initialTime = null,
  title = 'FuncDraw Play',
  baseHref = '/',
  embed = false,
  host = '127.0.0.1',
  port,
  openBrowser = true
}) {
  const app = express();
  const preview = createPreviewRouter({
    evaluateScene,
    getBootstrap,
    runtimeSource,
    fontPath,
    initialTime,
    title,
    baseHref,
    embed
  });
  app.use(preview.router);

  const resolvedPort = await resolvePort(port);
  const server = await new Promise((resolve, reject) => {
    const listener = app
      .listen(resolvedPort, host, () => resolve(listener))
      .on('error', reject);
  });

  const url = `http://${host === '0.0.0.0' ? 'localhost' : host}:${resolvedPort}`;
  console.log(picocolors.cyan('FuncDraw Play ready at'), picocolors.bold(url));

  if (openBrowser) {
    await tryOpenBrowser(url);
  }

  return {
    port: resolvedPort,
    host,
    url,
    broadcastReload() {
      preview.broadcastReload();
    },
    close() {
      server.close();
      preview.close();
    }
  };
}

async function resolvePort(requested) {
  const preferred = typeof requested === 'number' ? requested : 5173;
  try {
    const mod = await import('get-port');
    const getPort = mod.default || mod;
    return getPort({ port: preferred });
  } catch {
    return preferred;
  }
}

async function tryOpenBrowser(url) {
  try {
    const mod = await import('open');
    const open = mod.default || mod;
    if (typeof open === 'function') {
      await open(url);
    } else {
      console.warn('[funcdraw-play] Failed to open browser automatically: unsupported interface');
    }
  } catch (error) {
    console.warn('[funcdraw-play] Failed to open browser automatically:', error.message);
  }
}

module.exports = {
  startServer
};

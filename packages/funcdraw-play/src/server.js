'use strict';

const express = require('express');
const picocolors = require('picocolors');
const { createHtmlTemplate } = require('./html-template');

async function startServer({ evaluateScene, host = '127.0.0.1', port, openBrowser = true }) {
  const app = express();
  const clients = new Set();
  app.get('/', (_req, res) => {
    res.set('Content-Type', 'text/html; charset=utf-8');
    res.send(createHtmlTemplate());
  });

  app.get('/__funcdraw/scene', async (req, res) => {
    const requestId = `http-${Date.now().toString(36)}`;
    const includeSvg = Boolean(req.query.svg);
    console.log(
      picocolors.gray(
        `[funcdraw-play] [${requestId}] GET /__funcdraw/scene (svg=${includeSvg ? 'yes' : 'no'}, ip=${req.ip || 'n/a'})`
      )
    );
    try {
      const scene = await evaluateScene({ includeSvg, requestId });
      console.log(picocolors.gray(`[funcdraw-play] [${requestId}] Responding with scene payload`));
      res.json(scene);
    } catch (error) {
      console.error(
        picocolors.red(`[funcdraw-play] [${requestId}] Scene evaluation failed:`),
        error.message || error
      );
      res.status(500).json({ error: error.message || 'Evaluation failed' });
    }
  });

  app.get('/__funcdraw/events', (req, res) => {
    res.writeHead(200, {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      Connection: 'keep-alive'
    });
    res.write('retry: 1000\n\n');
    clients.add(res);
    console.log(
      picocolors.gray(
        `[funcdraw-play] Event stream connected (ip=${req.ip || 'n/a'}, total clients: ${clients.size})`
      )
    );
    req.on('close', () => {
      console.log(
        picocolors.gray(
          `[funcdraw-play] Event stream disconnected (ip=${req.ip || 'n/a'}, remaining clients: ${clients.size - 1})`
        )
      );
      clients.delete(res);
    });
  });

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
      console.log(picocolors.gray(`[funcdraw-play] Broadcasting reload to ${clients.size} client(s)`));
      for (const client of clients) {
        client.write('event: reload\ndata: {}\n\n');
      }
    },
    close() {
      server.close();
      for (const client of clients) {
        client.end();
      }
      clients.clear();
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

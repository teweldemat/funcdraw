'use strict';

const express = require('express');
const picocolors = require('picocolors');
const { createHtmlTemplate } = require('./html-template');

function createPreviewRouter({
  evaluateScene,
  getBootstrap,
  runtimeSource,
  fontPath,
  initialTime = null,
  title = 'FuncDraw Play',
  baseHref = '/',
  embed = false
} = {}) {
  const router = express.Router();
  router.use(express.json({ limit: '1mb' }));

  const clients = new Set();

  router.get('/', (_req, res) => {
    res.set('Content-Type', 'text/html; charset=utf-8');
    res.send(createHtmlTemplate({ initialTime, baseHref, title, embed }));
  });

  if (typeof runtimeSource === 'string' && runtimeSource.length > 0) {
    router.get('/__funcdraw/runtime.js', (_req, res) => {
      res.set('Content-Type', 'application/javascript; charset=utf-8');
      res.set('Cache-Control', 'no-store');
      res.send(runtimeSource);
    });
  }

  if (typeof fontPath === 'string' && fontPath.length > 0) {
    router.get('/__funcdraw/assets/fonts/Inter-Regular.ttf', (_req, res) => {
      res.set('Cache-Control', 'no-store');
      res.sendFile(fontPath);
    });
  }

  if (typeof getBootstrap === 'function') {
    router.get('/__funcdraw/bootstrap', (_req, res) => {
      const payload = getBootstrap();
      res.set('Cache-Control', 'no-store');
      res.json(payload);
    });
  }

  const handleSceneRequest = async (req, res) => {
    if (typeof evaluateScene !== 'function') {
      res.status(404).json({ error: 'Server-side evaluation is disabled (browser runtime mode)' });
      return;
    }
    const requestId = `http-${Date.now().toString(36)}`;
    const includeSvg = Boolean(req.query.svg || (req.body && req.body.svg));
    const resetState = Boolean(req.query.resetState || (req.body && req.body.resetState));
    const events = collectEvents(req);
    const eventCount = Array.isArray(events) ? events.length : 0;
    const time = resolveNumeric(req.query.time ?? req.query.t ?? (req.body && (req.body.time ?? req.body.t)));
    const canvasWidth = resolveNumeric(req.query.canvasWidth ?? (req.body && req.body.canvasWidth));
    const canvasHeight = resolveNumeric(req.query.canvasHeight ?? (req.body && req.body.canvasHeight));
    console.log(
      picocolors.gray(
        `[funcdraw-play] [${requestId}] ${req.method} __funcdraw/scene (svg=${includeSvg ? 'yes' : 'no'}, resetState=${
          resetState ? 'yes' : 'no'
        }, events=${eventCount}, ip=${req.ip || 'n/a'})`
      )
    );
    if (eventCount > 0) {
      console.log(picocolors.gray(`[funcdraw-play] [${requestId}] Incoming events payload:`), events);
    }
    try {
      const scene = await evaluateScene({
        includeSvg,
        requestId,
        query: { ...req.query, time, canvasWidth, canvasHeight },
        events,
        resetState
      });
      console.log(
        picocolors.gray(
          `[funcdraw-play] [${requestId}] Responding with ${scene === null ? 'null payload (ignored)' : 'scene payload'}`
        )
      );
      res.json(scene);
    } catch (error) {
      console.error(picocolors.red(`[funcdraw-play] [${requestId}] Scene evaluation failed:`), error.message || error);
      res.status(500).json({ error: error.message || 'Evaluation failed' });
    }
  };

  router.get('/__funcdraw/scene', handleSceneRequest);
  router.post('/__funcdraw/scene', handleSceneRequest);

  router.get('/__funcdraw/events', (req, res) => {
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

  return {
    router,
    broadcastReload() {
      console.log(picocolors.gray(`[funcdraw-play] Broadcasting reload to ${clients.size} client(s)`));
      for (const client of clients) {
        client.write('event: reload\ndata: {}\n\n');
      }
    },
    close() {
      for (const client of clients) {
        client.end();
      }
      clients.clear();
    }
  };
}

module.exports = {
  createPreviewRouter
};

function collectEvents(req) {
  const collected = [];
  const bodyEvents = req.body && req.body.events;
  if (Array.isArray(bodyEvents)) {
    collected.push(...bodyEvents);
  } else if (bodyEvents !== undefined) {
    collected.push(bodyEvents);
  }

  const queryEvents = req.query && req.query.events;
  if (queryEvents !== undefined) {
    try {
      appendEvents(collected, typeof queryEvents === 'string' ? JSON.parse(queryEvents) : queryEvents);
    } catch {
      appendEvents(collected, queryEvents);
    }
  }

  const singleEvent = req.query && req.query.event;
  if (singleEvent !== undefined) {
    appendEvents(collected, singleEvent);
  }

  return collected.length > 0 ? collected : null;
}

function appendEvents(target, payload) {
  if (payload === undefined || payload === null) {
    return;
  }
  if (Array.isArray(payload)) {
    target.push(...payload);
    return;
  }
  if (typeof payload !== 'string' && payload[Symbol.iterator]) {
    target.push(...payload);
    return;
  }
  target.push(payload);
}

function resolveNumeric(value) {
  const num = Number(value);
  return Number.isFinite(num) ? num : undefined;
}


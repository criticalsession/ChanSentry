#!/usr/bin/env node
import { createServer } from 'node:http';
import { createReadStream, existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { WatchedThreadService } from './watched-thread-service.js';
import { ThreadFetchService } from './thread-fetch-service.js';
import { ThreadProcessingService } from './thread-processing-service.js';
import { MediaDownloadService } from './media-download-service.js';
import { createWatchedThread } from './models.js';
import {
  findCatalogMatches,
  formatLastChecked,
  normalizeSearchText
} from './manage-threads.js';
import { WatcherController } from './watcher-controller.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const publicDir = path.join(__dirname, 'web');

const mimeTypes = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml'
};

export function createWebApp({
  watchedThreadService = new WatchedThreadService(),
  threadFetchService = new ThreadFetchService(),
  watcherController = null,
  publicRoot = publicDir,
  logger = console
} = {}) {
  let controller = watcherController;
  if (!controller) {
    const downloadService = new MediaDownloadService({
      logger,
      onProgress: progress => {
        controller.emit('download-progress', {
          message: `Downloading ${progress.completed}/${progress.total} for /${progress.thread.Board}/${progress.thread.ThreadId}`,
          thread: progress.thread,
          download: {
            completed: progress.completed,
            total: progress.total
          }
        });
      }
    });
    controller = new WatcherController({
      watchedThreadService,
      threadProcessingService: new ThreadProcessingService({
        watchedThreadService,
        fetchService: threadFetchService,
        downloadService
      }),
      logger
    });
  }

  async function handleRequest(req, res) {
    try {
      const requestUrl = new URL(req.url, 'http://localhost');

      if (requestUrl.pathname.startsWith('/api/')) {
        await handleApi(req, res, requestUrl, { watchedThreadService, threadFetchService, controller });
        return;
      }

      await serveStatic(req, res, requestUrl, publicRoot);
    } catch (error) {
      logger.error(error.stack ?? error.message);
      sendJson(res, 500, { error: 'Internal server error' });
    }
  }

  return {
    server: createServer(handleRequest),
    watcherController: controller
  };
}

async function handleApi(req, res, requestUrl, context) {
  const { watchedThreadService, threadFetchService, controller } = context;
  const method = req.method ?? 'GET';

  if (method === 'GET' && requestUrl.pathname === '/api/threads') {
    const threads = await watchedThreadService.readWatchedThreads();
    sendJson(res, 200, { threads: threads.map(toThreadViewModel) });
    return;
  }

  if (method === 'POST' && requestUrl.pathname === '/api/threads') {
    const body = await readJsonBody(req);
    const board = String(body.board ?? '').trim().toLowerCase();
    const threadId = Number(body.threadId);

    if (!board || !Number.isSafeInteger(threadId) || threadId <= 0) {
      sendJson(res, 400, { error: 'Board and positive numeric threadId are required.' });
      return;
    }

    const stoppedWatcher = controller.getStatus().running;
    if (stoppedWatcher) {
      await controller.stopAndWait();
    }

    const watchedThreads = await watchedThreadService.readWatchedThreads();
    if (watchedThreads.some(thread => thread.Board.toLowerCase() === board && thread.ThreadId === threadId)) {
      sendJson(res, 409, { error: 'Thread is already watched.' });
      return;
    }

    const newThread = createWatchedThread({ Board: board, ThreadId: threadId });
    const fetchResult = await threadFetchService.fetchThread(newThread);
    if (!fetchResult.isSuccess || !fetchResult.threadData) {
      sendJson(res, fetchResult.statusCode === 404 ? 404 : 502, {
        error: fetchResult.statusCode === 404 ? 'Thread not found.' : 'Failed to fetch thread.',
        statusCode: fetchResult.statusCode
      });
      return;
    }

    newThread.Subject = fetchResult.threadData.Posts[0]?.Subject ?? '';
    watchedThreads.push(newThread);
    await watchedThreadService.saveWatchedThreads(watchedThreads);
    sendJson(res, 201, { thread: toThreadViewModel(newThread), stoppedWatcher });
    return;
  }

  const deleteMatch = requestUrl.pathname.match(/^\/api\/threads\/([^/]+)\/(\d+)$/);
  if (method === 'DELETE' && deleteMatch) {
    const board = decodeURIComponent(deleteMatch[1]).toLowerCase();
    const threadId = Number(deleteMatch[2]);
    const watchedThreads = await watchedThreadService.readWatchedThreads();
    const nextThreads = watchedThreads.filter(thread => !(
      thread.Board.toLowerCase() === board && thread.ThreadId === threadId
    ));

    if (nextThreads.length === watchedThreads.length) {
      sendJson(res, 404, { error: 'Thread not found.' });
      return;
    }

    await watchedThreadService.saveWatchedThreads(nextThreads);
    sendJson(res, 200, { threads: nextThreads.map(toThreadViewModel) });
    return;
  }

  if (method === 'GET' && requestUrl.pathname === '/api/catalog/search') {
    const board = String(requestUrl.searchParams.get('board') ?? '').trim().toLowerCase();
    const query = String(requestUrl.searchParams.get('q') ?? '').trim();

    if (!board || !query) {
      sendJson(res, 400, { error: 'Board and query are required.' });
      return;
    }

    const catalogResult = await threadFetchService.fetchCatalog(board);
    if (!catalogResult.isSuccess) {
      sendJson(res, catalogResult.statusCode === 404 ? 404 : 502, {
        error: catalogResult.statusCode === 404 ? 'Board not found.' : 'Failed to fetch catalog.',
        statusCode: catalogResult.statusCode
      });
      return;
    }

    const watchedThreads = await watchedThreadService.readWatchedThreads();
    const matches = findCatalogMatches(catalogResult.catalogPages, query)
      .filter(thread => !watchedThreads.some(watched => (
        watched.Board.toLowerCase() === board && watched.ThreadId === thread.ThreadId
      )))
      .slice(0, 50)
      .map(thread => ({
        threadId: thread.ThreadId,
        subject: thread.Subject || 'No Subject',
        replies: thread.ReplyCount,
        images: thread.ImageCount,
        preview: truncateText(normalizeSearchText(thread.Comment) || 'No Comment', 180)
      }));

    sendJson(res, 200, { matches });
    return;
  }

  if (method === 'POST' && requestUrl.pathname === '/api/watcher/start') {
    sendJson(res, 200, { status: controller.start() });
    return;
  }

  if (method === 'POST' && requestUrl.pathname === '/api/watcher/stop') {
    sendJson(res, 200, { status: await controller.stopAndWait() });
    return;
  }

  if (method === 'GET' && requestUrl.pathname === '/api/watcher/status') {
    sendJson(res, 200, { status: controller.getStatus() });
    return;
  }

  if (method === 'GET' && requestUrl.pathname === '/api/events') {
    handleEvents(res, controller);
    return;
  }

  sendJson(res, 404, { error: 'Not found.' });
}

async function serveStatic(req, res, requestUrl, publicRoot) {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    sendJson(res, 405, { error: 'Method not allowed.' });
    return;
  }

  const pathname = requestUrl.pathname === '/' ? '/index.html' : requestUrl.pathname;
  const safePath = path.normalize(decodeURIComponent(pathname)).replace(/^(\.\.[/\\])+/, '');
  const filePath = path.join(publicRoot, safePath);

  if (!filePath.startsWith(publicRoot) || !existsSync(filePath)) {
    sendJson(res, 404, { error: 'Not found.' });
    return;
  }

  const extension = path.extname(filePath);
  res.writeHead(200, {
    'Content-Type': mimeTypes[extension] ?? 'application/octet-stream',
    'Cache-Control': 'no-store'
  });

  if (req.method === 'HEAD') {
    res.end();
    return;
  }

  createReadStream(filePath).pipe(res);
}

function handleEvents(res, controller) {
  res.writeHead(200, {
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-store',
    Connection: 'keep-alive'
  });
  res.write(`event: status\ndata: ${JSON.stringify({ status: controller.getStatus() })}\n\n`);

  const unsubscribe = controller.subscribe(event => {
    res.write(`event: activity\ndata: ${JSON.stringify(event)}\n\n`);
  });

  res.on('close', unsubscribe);
}

async function readJsonBody(req) {
  const chunks = [];
  for await (const chunk of req) {
    chunks.push(chunk);
  }

  if (chunks.length === 0) {
    return {};
  }

  return JSON.parse(Buffer.concat(chunks).toString('utf8'));
}

function sendJson(res, statusCode, body) {
  res.writeHead(statusCode, {
    'Content-Type': 'application/json; charset=utf-8',
    'Cache-Control': 'no-store'
  });
  res.end(JSON.stringify(body));
}

function toThreadViewModel(thread) {
  return {
    board: thread.Board,
    threadId: thread.ThreadId,
    subject: thread.Subject || 'No Subject',
    downloaded: thread.TotalDownloadedFiles,
    errors: thread.ErrorCount,
    lastChecked: thread.LastChecked,
    lastCheckedDisplay: formatLastChecked(thread.LastChecked)
  };
}

function truncateText(value, maxLength) {
  return value.length <= maxLength ? value : `${value.slice(0, maxLength - 3)}...`;
}

export function startWebServer({ port = Number(process.env.PORT) || 3131, host = '127.0.0.1' } = {}) {
  const app = createWebApp();
  app.server.listen(port, host, () => {
    console.log(`ChanSentry dashboard running at http://${host}:${port}`);
  });
  return app;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  startWebServer();
}

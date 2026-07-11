import test from 'node:test';
import assert from 'node:assert/strict';
import { createWebApp } from '../src/web-server.js';
import { ThreadFetchResult } from '../src/thread-fetch-service.js';
import { createThread, createWatchedThread } from '../src/models.js';
import { WatcherController } from '../src/watcher-controller.js';

class MemoryWatchedThreadService {
  constructor(threads = []) {
    this.threads = threads.map(createWatchedThread);
    this.saveCalls = 0;
  }

  async readWatchedThreads() {
    return this.threads.map(createWatchedThread);
  }

  async saveWatchedThreads(threads) {
    this.saveCalls += 1;
    this.threads = threads.map(createWatchedThread);
  }

  removeFailedThreads(threads) {
    return threads.filter(thread => thread.ErrorCount < 3);
  }
}

function listen(server) {
  return new Promise(resolve => {
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      resolve(`http://${address.address}:${address.port}`);
    });
  });
}

function close(server) {
  return new Promise((resolve, reject) => {
    server.close(error => {
      if (error) reject(error);
      else resolve();
    });
  });
}

async function request(baseUrl, path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options
  });
  return {
    status: response.status,
    body: await response.json()
  };
}

test('web API lists, adds, and deletes watched threads', async () => {
  const watchedThreadService = new MemoryWatchedThreadService([
    { Board: 'g', ThreadId: 10, Subject: 'GPU', TotalDownloadedFiles: 2 }
  ]);
  const threadFetchService = {
    fetchThread: async thread => ThreadFetchResult.success(createThread({
      posts: [{ sub: `Subject ${thread.ThreadId}` }]
    }))
  };
  const { server } = createWebApp({
    watchedThreadService,
    threadFetchService,
    watcherController: new WatcherController({ watchedThreadService, intervalMs: 1 }),
    logger: { log() {}, error() {} }
  });
  const baseUrl = await listen(server);

  try {
    const list = await request(baseUrl, '/api/threads');
    assert.equal(list.status, 200);
    assert.equal(list.body.threads.length, 1);
    assert.equal(list.body.threads[0].subject, 'GPU');

    const added = await request(baseUrl, '/api/threads', {
      method: 'POST',
      body: JSON.stringify({ board: 'g', threadId: 11 })
    });
    assert.equal(added.status, 201);
    assert.equal(added.body.thread.subject, 'Subject 11');

    const duplicate = await request(baseUrl, '/api/threads', {
      method: 'POST',
      body: JSON.stringify({ board: 'g', threadId: 11 })
    });
    assert.equal(duplicate.status, 409);

    const deleted = await request(baseUrl, '/api/threads/g/10', { method: 'DELETE' });
    assert.equal(deleted.status, 200);
    assert.deepEqual(deleted.body.threads.map(thread => thread.threadId), [11]);
  } finally {
    await close(server);
  }
});

test('web API stops a running watcher before adding a watched thread', async () => {
  const watchedThreadService = new MemoryWatchedThreadService([]);
  const controller = new WatcherController({
    watchedThreadService,
    threadProcessingService: { processThread: async () => {} },
    intervalMs: 1000,
    betweenThreadDelayMs: 1,
    logger: { log() {}, error() {} }
  });
  const threadFetchService = {
    fetchThread: async thread => ThreadFetchResult.success(createThread({
      posts: [{ sub: `Subject ${thread.ThreadId}` }]
    }))
  };
  const { server } = createWebApp({
    watchedThreadService,
    threadFetchService,
    watcherController: controller,
    logger: { log() {}, error() {} }
  });
  const baseUrl = await listen(server);
  controller.start();

  try {
    const added = await request(baseUrl, '/api/threads', {
      method: 'POST',
      body: JSON.stringify({ board: 'g', threadId: 22 })
    });

    assert.equal(added.status, 201);
    assert.equal(added.body.stoppedWatcher, true);
    assert.equal(controller.getStatus().running, false);
    assert.deepEqual((await watchedThreadService.readWatchedThreads()).map(thread => thread.ThreadId), [22]);
  } finally {
    controller.stop();
    await controller.runPromise;
    await close(server);
  }
});

test('web API stop returns idle status after the watcher stops', async () => {
  const watchedThreadService = new MemoryWatchedThreadService([]);
  const controller = new WatcherController({
    watchedThreadService,
    threadProcessingService: { processThread: async () => {} },
    intervalMs: 1000,
    betweenThreadDelayMs: 1,
    logger: { log() {}, error() {} }
  });
  const { server } = createWebApp({
    watchedThreadService,
    threadFetchService: {},
    watcherController: controller,
    logger: { log() {}, error() {} }
  });
  const baseUrl = await listen(server);

  try {
    const started = await request(baseUrl, '/api/watcher/start', { method: 'POST' });
    assert.equal(started.body.status.running, true);

    const stopped = await request(baseUrl, '/api/watcher/stop', { method: 'POST' });
    assert.equal(stopped.body.status.running, false);
    assert.equal(stopped.body.status.stopping, false);
  } finally {
    controller.stop();
    await controller.runPromise;
    await close(server);
  }
});

test('web API searches catalog and excludes already watched threads', async () => {
  const watchedThreadService = new MemoryWatchedThreadService([
    { Board: 'g', ThreadId: 10, Subject: 'Watched GPU' }
  ]);
  const threadFetchService = {
    fetchCatalog: async board => ({
      isSuccess: true,
      statusCode: 200,
      catalogPages: [
        {
          Page: 1,
          ThreadList: [
            { ThreadId: 10, Subject: 'GPU prices', Comment: 'Already watched', ReplyCount: 3, ImageCount: 1 },
            { ThreadId: 12, Subject: 'GPU drivers', Comment: '<b>linux</b> notes', ReplyCount: 5, ImageCount: 2 },
            { ThreadId: 13, Subject: 'CPU news', Comment: 'Other topic', ReplyCount: 1, ImageCount: 0 }
          ]
        }
      ]
    })
  };
  const { server } = createWebApp({
    watchedThreadService,
    threadFetchService,
    watcherController: new WatcherController({ watchedThreadService, intervalMs: 1 }),
    logger: { log() {}, error() {} }
  });
  const baseUrl = await listen(server);

  try {
    const result = await request(baseUrl, '/api/catalog/search?board=g&q=gpu');
    assert.equal(result.status, 200);
    assert.deepEqual(result.body.matches.map(match => match.threadId), [12]);
    assert.equal(result.body.matches[0].preview, 'linux notes');
  } finally {
    await close(server);
  }
});

test('watcher controller start and stop are idempotent', async () => {
  const watchedThreadService = new MemoryWatchedThreadService([]);
  const events = [];
  const controller = new WatcherController({
    watchedThreadService,
    threadProcessingService: { processThread: async () => {} },
    intervalMs: 5,
    betweenThreadDelayMs: 1,
    logger: { log() {}, error() {} }
  });
  controller.subscribe(event => events.push(event.type));

  assert.equal(controller.start().running, true);
  assert.equal(controller.start().running, true);
  assert.equal(controller.stop().stopping, true);

  await controller.runPromise;

  assert.equal(controller.getStatus().running, false);
  assert.ok(events.includes('watcher-started'));
  assert.ok(events.includes('cycle-empty'));
  assert.ok(events.includes('watcher-stopped'));
});

test('watcher controller exposes only the actively checked thread', async () => {
  const watchedThreadService = new MemoryWatchedThreadService([
    { Board: 'g', ThreadId: 10, Subject: 'GPU' }
  ]);
  let statusDuringCheck = null;
  let controller = null;
  controller = new WatcherController({
    watchedThreadService,
    threadProcessingService: {
      processThread: async () => {
        statusDuringCheck = controller.getStatus();
        controller.stop();
      }
    },
    intervalMs: 1,
    betweenThreadDelayMs: 1,
    logger: { log() {}, error() {} }
  });

  controller.start();
  await controller.runPromise;

  assert.deepEqual(statusDuringCheck.currentThread, {
    Board: 'g',
    ThreadId: 10,
    Subject: 'GPU'
  });
  assert.equal(controller.getStatus().currentThread, null);
});

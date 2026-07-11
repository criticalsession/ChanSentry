import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm, mkdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { WatchedThreadService } from '../src/watched-thread-service.js';
import { ThreadFetchService } from '../src/thread-fetch-service.js';
import { MediaDownloadService, buildFileName, getDownloadFolderName } from '../src/media-download-service.js';
import { ThreadProcessingService } from '../src/thread-processing-service.js';
import { sanitizeFileName } from '../src/file-name-sanitizer.js';
import { ThreadFetchResult } from '../src/thread-fetch-service.js';
import { createPost, createWatchedThread } from '../src/models.js';

test('watched thread service creates, reads, saves, and filters threads', async () => {
  const dir = await mkdtemp(path.join(tmpdir(), 'chansentry-'));
  const filePath = path.join(dir, 'watched-threads.json');

  try {
    const service = new WatchedThreadService(filePath);
    assert.deepEqual(await service.readWatchedThreads(), []);

    const threads = [
      createWatchedThread({ Board: 'g', ThreadId: 1, ErrorCount: 0 }),
      createWatchedThread({ Board: 'g', ThreadId: 2, ErrorCount: 3 })
    ];
    await service.saveWatchedThreads(threads);

    assert.equal((await service.readWatchedThreads()).length, 2);
    assert.deepEqual(service.removeFailedThreads(threads).map(thread => thread.ThreadId), [1]);
  } finally {
    await rm(dir, { recursive: true, force: true });
  }
});

test('thread fetch service returns success, not modified, and failures', async () => {
  const watchedThread = createWatchedThread({
    Board: 'g',
    ThreadId: 12345,
    LastChecked: '2024-01-01T00:00:00Z'
  });

  const successService = new ThreadFetchService(async (url, options) => {
    assert.equal(url, 'https://a.4cdn.org/g/thread/12345.json');
    assert.equal(options.headers['User-Agent'], 'ChanSentry/1.0');
    assert.ok(options.headers['If-Modified-Since'].includes('GMT'));
    return {
      ok: true,
      status: 200,
      json: async () => ({ posts: [{ filename: 'image', tim: 123, ext: '.jpg' }] })
    };
  });

  const success = await successService.fetchThread(watchedThread);
  assert.equal(success.isSuccess, true);
  assert.equal(success.threadData.Posts[0].InternalFileIdentifier, 123);

  const notModified = await new ThreadFetchService(async () => ({ ok: false, status: 304 })).fetchThread(watchedThread);
  assert.equal(notModified.isNotModified, true);

  const failed = await new ThreadFetchService(async () => ({ ok: false, status: 404 })).fetchThread(watchedThread);
  assert.equal(failed.statusCode, 404);
});

test('download folders and filenames match existing behavior', () => {
  assert.equal(sanitizeFileName('bad/file[name].jpg'), 'bad_file_name_.jpg');
  assert.equal(sanitizeFileName('  '.repeat(5)), '');

  const longName = 'a'.repeat(250);
  assert.equal(sanitizeFileName(longName).length, 200);

  assert.equal(getDownloadFolderName(createWatchedThread({ ThreadId: 12345, Subject: 'My Cool Thread' })), '12345 - My Cool Thread');
  assert.equal(getDownloadFolderName(createWatchedThread({ ThreadId: 12345, Subject: '' })), '12345');

  assert.equal(buildFileName(createPost({ filename: 'testfile', tim: 1234567890, ext: '.jpg' })), 'testfile - 1234567890.jpg');
  assert.equal(buildFileName(createPost({ filename: null, tim: 1234567890, ext: '.jpg' })), '1234567890.jpg');
});

test('media download service creates subject folders, renames old folders, and skips existing files', async () => {
  const dir = await mkdtemp(path.join(tmpdir(), 'chansentry-download-'));
  const downloadRoot = path.join(dir, 'downloads');
  const thread = createWatchedThread({ Board: 'g', ThreadId: 12345, Subject: 'Subject' });
  const oldPath = path.join(downloadRoot, 'g', '12345');

  try {
    await mkdir(oldPath, { recursive: true });

    let fetchCalls = 0;
    const service = new MediaDownloadService({
      downloadRoot,
      logger: { log() {}, error() {} },
      fetchImpl: async () => {
        fetchCalls += 1;
        return {
          ok: true,
          status: 200,
          arrayBuffer: async () => Buffer.from('image')
        };
      }
    });

    await service.downloadMediaFiles([
      createPost({ filename: 'image', tim: 111, ext: '.jpg' })
    ], thread);

    const filePath = path.join(downloadRoot, 'g', '12345 - Subject', 'image - 111.jpg');
    assert.equal(await readFile(filePath, 'utf8'), 'image');
    assert.equal(fetchCalls, 1);

    await service.downloadMediaFiles([
      createPost({ filename: 'image', tim: 111, ext: '.jpg' })
    ], thread);
    assert.equal(fetchCalls, 1);
  } finally {
    await rm(dir, { recursive: true, force: true });
  }
});

test('thread processing downloads only new media and updates thread state', async () => {
  const watchedThread = createWatchedThread({
    Board: 'g',
    ThreadId: 12345,
    Subject: '',
    TotalDownloadedFiles: 2,
    LastChecked: '2024-01-01T00:00:00Z'
  });

  const downloaded = [];
  const service = new ThreadProcessingService({
    logger: { log() {}, error() {} },
    fetchService: {
      fetchThread: async () => ThreadFetchResult.success({
        Posts: [
          createPost({ sub: 'Retrieved Subject', tim: 1, ext: '.jpg' }),
          createPost({ tim: 2, ext: '.jpg' }),
          createPost({ tim: 3, ext: '.png' }),
          createPost({ time: 1 })
        ]
      })
    },
    downloadService: {
      downloadMediaFiles: async posts => downloaded.push(...posts)
    }
  });

  await service.processThread(watchedThread, [watchedThread]);

  assert.equal(watchedThread.Subject, 'Retrieved Subject');
  assert.equal(watchedThread.TotalDownloadedFiles, 3);
  assert.equal(downloaded.length, 1);
  assert.equal(downloaded[0].InternalFileIdentifier, 3);
  assert.notEqual(watchedThread.LastChecked, '2024-01-01T00:00:00Z');
});

test('thread processing removes thread after third fetch error', async () => {
  const watchedThread = createWatchedThread({ Board: 'g', ThreadId: 12345, ErrorCount: 2 });
  const watchedThreads = [watchedThread];
  let savedThreads = null;

  const service = new ThreadProcessingService({
    logger: { log() {}, error() {} },
    fetchService: {
      fetchThread: async () => ThreadFetchResult.failed(404)
    },
    watchedThreadService: {
      saveWatchedThreads: async threads => {
        savedThreads = [...threads];
      }
    }
  });

  await service.processThread(watchedThread, watchedThreads);

  assert.equal(watchedThread.ErrorCount, 3);
  assert.equal(watchedThreads.length, 0);
  assert.deepEqual(savedThreads, []);
});

import { readFile, writeFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { WATCHED_THREADS_FILE } from './constants.js';
import { createWatchedThread } from './models.js';

export class WatchedThreadService {
  constructor(filePath = WATCHED_THREADS_FILE) {
    this.filePath = filePath;
  }

  async readWatchedThreads() {
    if (!existsSync(this.filePath)) {
      await this.saveWatchedThreads([]);
    }

    const json = await readFile(this.filePath, 'utf8');
    const parsed = JSON.parse(json);
    return Array.isArray(parsed) ? parsed.map(createWatchedThread) : [];
  }

  async saveWatchedThreads(watchedThreads) {
    await writeFile(this.filePath, JSON.stringify(watchedThreads), 'utf8');
  }

  removeFailedThreads(watchedThreads) {
    return watchedThreads.filter(thread => thread.ErrorCount < 3);
  }
}

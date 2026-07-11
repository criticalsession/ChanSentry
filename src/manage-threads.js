import { createInterface } from 'node:readline/promises';
import { stdin as input, stdout as output } from 'node:process';
import { WatchedThreadService } from './watched-thread-service.js';
import { ThreadFetchService } from './thread-fetch-service.js';
import { createWatchedThread } from './models.js';

export class ManageThreadsHandler {
  constructor({
    watchedThreadService = new WatchedThreadService(),
    threadFetchService = new ThreadFetchService(),
    io = null,
    logger = console
  } = {}) {
    this.watchedThreadService = watchedThreadService;
    this.threadFetchService = threadFetchService;
    this.io = io;
    this.logger = logger;
  }

  async addThread() {
    const rl = this.io ?? createInterface({ input, output });
    try {
      console.clear();
      printTitle();
      this.logger.log('Add Watched Thread\n');

      const board = (await rl.question('Enter the board code (e.g., g, pol, v): ')).trim().toLowerCase();
      if (!board) {
        this.logger.error('Board cannot be empty.');
        return;
      }

      const threadId = Number(await rl.question('Enter the thread ID: '));
      if (!Number.isSafeInteger(threadId) || threadId <= 0) {
        this.logger.error('Invalid thread ID.');
        return;
      }

      const watchedThreads = await this.watchedThreadService.readWatchedThreads();
      if (watchedThreads.some(thread => thread.Board.toLowerCase() === board && thread.ThreadId === threadId)) {
        this.logger.log(`Thread ${threadId} on /${board}/ is already being watched.`);
        return;
      }

      await this.fetchAndAddThread(watchedThreads, board, threadId);
    } finally {
      if (!this.io) {
        rl.close();
      }
    }
  }

  async fetchAndAddThread(watchedThreads, board, threadId) {
    this.logger.log(`\nFetching thread ${threadId} from /${board}/...`);

    const newThread = createWatchedThread({ Board: board, ThreadId: threadId });
    const fetchResult = await this.threadFetchService.fetchThread(newThread);

    if (fetchResult.isSuccess && fetchResult.threadData) {
      newThread.Subject = fetchResult.threadData.Posts[0]?.Subject ?? '';
      const mediaCount = fetchResult.threadData.Posts.filter(post => (
        post.InternalFileIdentifier !== null && post.InternalFileIdentifier !== undefined && post.FileExtension
      )).length;

      watchedThreads.push(newThread);
      await this.watchedThreadService.saveWatchedThreads(watchedThreads);

      this.logger.log('\nThread added successfully!');
      this.logger.log(`Board: ${board}`);
      this.logger.log(`Thread ID: ${threadId}`);
      this.logger.log(`Subject: ${newThread.Subject || 'No Subject'}`);
      this.logger.log(`Media Files: ${mediaCount}`);
    } else if (fetchResult.statusCode === 404) {
      this.logger.error(`\nThread ${threadId} not found on /${board}/.`);
      this.logger.log('Please check the board code and thread ID.');
    } else {
      this.logger.error(`\nFailed to fetch thread. Status: ${fetchResult.statusCode}`);
    }
  }

  async listAndDeleteThreads() {
    const rl = this.io ?? createInterface({ input, output });
    try {
      const watchedThreads = await this.watchedThreadService.readWatchedThreads();

      if (watchedThreads.length === 0) {
        this.logger.log('No threads found in watched-threads.json');
        return;
      }

      while (true) {
        console.clear();
        printTitle();
        this.logger.log('Manage Watched Threads\n');
        printThreadsTable(watchedThreads);

        const action = (await rl.question('Choose: [d]elete threads, [b]ack: ')).trim().toLowerCase();
        if (action === 'b' || action === 'back') {
          return;
        }

        if (action !== 'd' && action !== 'delete') {
          continue;
        }

        const selected = await rl.question('Enter thread numbers to delete, comma separated, or blank to cancel: ');
        const indexes = selected
          .split(',')
          .map(value => Number(value.trim()) - 1)
          .filter(index => Number.isInteger(index) && index >= 0 && index < watchedThreads.length);

        const uniqueIndexes = [...new Set(indexes)].sort((a, b) => b - a);
        if (uniqueIndexes.length === 0) {
          continue;
        }

        const confirm = (await rl.question(`Delete ${uniqueIndexes.length} thread(s)? [y/N]: `)).trim().toLowerCase();
        if (confirm !== 'y' && confirm !== 'yes') {
          continue;
        }

        for (const index of uniqueIndexes) {
          const [deleted] = watchedThreads.splice(index, 1);
          this.logger.log(`Deleted: /${deleted.Board}/ - ${deleted.ThreadId} - ${deleted.Subject || 'No Subject'}`);
        }

        await this.watchedThreadService.saveWatchedThreads(watchedThreads);
        this.logger.log(`Successfully deleted ${uniqueIndexes.length} thread(s)!`);

        if (watchedThreads.length === 0) {
          this.logger.log('All threads have been deleted.');
          return;
        }

        await rl.question('Press enter to continue...');
      }
    } finally {
      if (!this.io) {
        rl.close();
      }
    }
  }
}

export function printTitle() {
  console.log('Welcome to ChanSentry!\n');
}

export function printThreadsTable(watchedThreads) {
  const rows = watchedThreads.map((thread, index) => ({
    '#': index + 1,
    Board: thread.Board,
    ThreadId: thread.ThreadId,
    Subject: thread.Subject || 'No Subject',
    Downloaded: thread.TotalDownloadedFiles,
    Errors: thread.ErrorCount,
    LastChecked: formatLastChecked(thread.LastChecked)
  }));
  console.table(rows);
}

export function formatLastChecked(value) {
  if (!value || String(value).startsWith('0001-01-01')) {
    return 'Never';
  }

  const checked = new Date(value);
  if (Number.isNaN(checked.getTime())) {
    return 'Never';
  }

  const minutes = (Date.now() - checked.getTime()) / 60000;
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${Math.floor(minutes)}m ago`;

  const hours = minutes / 60;
  if (hours < 24) return `${Math.floor(hours)}h ago`;

  const days = hours / 24;
  if (days < 7) return `${Math.floor(days)}d ago`;

  return checked.toISOString().slice(0, 10);
}

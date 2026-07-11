import { WatchedThreadService } from './watched-thread-service.js';
import { ThreadProcessingService } from './thread-processing-service.js';
import { MAX_ERROR_COUNT } from './constants.js';
import { printTitle } from './manage-threads.js';

const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

export class DownloadHandler {
  constructor({
    watchedThreadService = new WatchedThreadService(),
    threadProcessingService = new ThreadProcessingService({ watchedThreadService }),
    logger = console
  } = {}) {
    this.watchedThreadService = watchedThreadService;
    this.threadProcessingService = threadProcessingService;
    this.logger = logger;
  }

  async start() {
    let watchedThreads = await this.watchedThreadService.readWatchedThreads();

    if (watchedThreads.length === 0) {
      this.logger.log('No threads found in watched-threads.json');
      return;
    }

    this.logger.log('Downloader running. Press ESC or Q to return to main menu.\n');
    const exitWatcher = createExitWatcher();

    try {
      let running = true;
      while (running) {
        console.clear();
        printTitle();
        this.logger.log('Downloader Running...');
        this.logger.log('Press ESC or Q to return to main menu.\n');

        running = await this.processAllThreads(watchedThreads, exitWatcher);
        watchedThreads = this.watchedThreadService.removeFailedThreads(watchedThreads);
        await this.watchedThreadService.saveWatchedThreads(watchedThreads);

        if (running) {
          running = await countdownWithExitCheck(10, exitWatcher, this.logger);
        }
      }
    } finally {
      exitWatcher.dispose();
      await this.watchedThreadService.saveWatchedThreads(watchedThreads);
      this.logger.log('Returning to main menu...');
      await delay(500);
    }
  }

  async processAllThreads(watchedThreads, exitWatcher) {
    const activeThreads = watchedThreads.filter(thread => thread.ErrorCount < MAX_ERROR_COUNT);

    for (let i = 0; i < activeThreads.length; i += 1) {
      if (exitWatcher.shouldExit()) {
        return false;
      }

      await this.threadProcessingService.processThread(activeThreads[i], watchedThreads);
      await this.watchedThreadService.saveWatchedThreads(watchedThreads);

      if (i < activeThreads.length - 1) {
        const shouldContinue = await countdownWithExitCheck(2, exitWatcher, this.logger);
        if (!shouldContinue) {
          return false;
        }
      }
    }

    return !exitWatcher.shouldExit();
  }
}

function createExitWatcher() {
  let shouldExit = false;

  if (!process.stdin.isTTY) {
    return {
      shouldExit: () => false,
      dispose: () => {}
    };
  }

  const onData = chunk => {
    const key = chunk.toString('utf8').toLowerCase();
    if (key === '\u001b' || key === 'q') {
      shouldExit = true;
    }
  };

  process.stdin.setRawMode(true);
  process.stdin.resume();
  process.stdin.on('data', onData);

  return {
    shouldExit: () => shouldExit,
    dispose: () => {
      process.stdin.off('data', onData);
      process.stdin.setRawMode(false);
      process.stdin.pause();
    }
  };
}

async function countdownWithExitCheck(seconds, exitWatcher, logger) {
  for (let remaining = seconds * 100; remaining > 0; remaining -= 1) {
    const displaySeconds = Math.ceil(remaining / 100);
    const suffix = displaySeconds === 1 ? '' : 's';
    process.stdout.write(`\rNext check in ${displaySeconds} second${suffix}... (Press ESC or Q to exit)  `);
    await delay(10);

    if (exitWatcher.shouldExit()) {
      logger.log('');
      return false;
    }
  }

  logger.log('');
  return true;
}

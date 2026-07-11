import { WatchedThreadService } from './watched-thread-service.js';
import { ThreadProcessingService } from './thread-processing-service.js';
import { MAX_ERROR_COUNT } from './constants.js';

const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

export class WatcherController {
  constructor({
    watchedThreadService = new WatchedThreadService(),
    threadProcessingService = new ThreadProcessingService({ watchedThreadService }),
    intervalMs = 10000,
    betweenThreadDelayMs = 2000,
    logger = console,
    now = () => new Date()
  } = {}) {
    this.watchedThreadService = watchedThreadService;
    this.threadProcessingService = threadProcessingService;
    this.intervalMs = intervalMs;
    this.betweenThreadDelayMs = betweenThreadDelayMs;
    this.logger = logger;
    this.now = now;
    this.running = false;
    this.stopping = false;
    this.currentThread = null;
    this.lastRunStartedAt = null;
    this.lastRunFinishedAt = null;
    this.lastError = null;
    this.listeners = new Set();
    this.runPromise = null;
  }

  start() {
    if (this.running) {
      return this.getStatus();
    }

    this.running = true;
    this.stopping = false;
    this.lastError = null;
    this.emit('watcher-started', { message: 'Watcher started' });
    this.runPromise = this.runLoop()
      .catch(error => {
        this.lastError = error.message;
        this.emit('watcher-error', { message: error.message });
      })
      .finally(() => {
        this.running = false;
        this.stopping = false;
        this.currentThread = null;
        this.emit('watcher-stopped', { message: 'Watcher stopped' });
      });

    return this.getStatus();
  }

  stop() {
    if (!this.running) {
      return this.getStatus();
    }

    this.stopping = true;
    this.emit('watcher-stopping', { message: 'Watcher stopping' });
    return this.getStatus();
  }

  async stopAndWait() {
    if (!this.running) {
      return this.getStatus();
    }

    this.stop();
    await this.runPromise;
    return this.getStatus();
  }

  getStatus() {
    return {
      running: this.running,
      stopping: this.stopping,
      currentThread: this.currentThread,
      lastRunStartedAt: this.lastRunStartedAt,
      lastRunFinishedAt: this.lastRunFinishedAt,
      lastError: this.lastError
    };
  }

  subscribe(listener) {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  emit(type, payload = {}) {
    const event = {
      type,
      timestamp: this.now().toISOString(),
      status: this.getStatus(),
      ...payload
    };

    for (const listener of this.listeners) {
      listener(event);
    }

    if (type.endsWith('error')) {
      this.logger.error(event.message);
    } else if (event.message) {
      this.logger.log(event.message);
    }
  }

  async runLoop() {
    while (!this.stopping) {
      this.lastRunStartedAt = this.now().toISOString();
      this.emit('cycle-started', { message: 'Checking watched threads' });

      let watchedThreads = await this.watchedThreadService.readWatchedThreads();
      if (watchedThreads.length === 0) {
        this.emit('cycle-empty', { message: 'No watched threads found' });
      } else {
        await this.processAllThreads(watchedThreads);
        watchedThreads = this.watchedThreadService.removeFailedThreads(watchedThreads);
        await this.watchedThreadService.saveWatchedThreads(watchedThreads);
      }

      this.currentThread = null;
      this.lastRunFinishedAt = this.now().toISOString();
      this.emit('cycle-finished', { message: 'Watcher cycle finished' });

      if (!this.stopping) {
        await this.waitWithStop(this.intervalMs);
      }
    }
  }

  async processAllThreads(watchedThreads) {
    const activeThreads = watchedThreads.filter(thread => thread.ErrorCount < MAX_ERROR_COUNT);

    for (let index = 0; index < activeThreads.length; index += 1) {
      if (this.stopping) {
        return;
      }

      const thread = activeThreads[index];
      this.currentThread = {
        Board: thread.Board,
        ThreadId: thread.ThreadId,
        Subject: thread.Subject
      };
      this.emit('thread-started', {
        message: `Checking /${thread.Board}/${thread.ThreadId}`,
        thread: this.currentThread
      });

      await this.threadProcessingService.processThread(thread, watchedThreads);
      await this.watchedThreadService.saveWatchedThreads(watchedThreads);

      this.currentThread = null;
      this.emit('thread-finished', {
        message: `Finished /${thread.Board}/${thread.ThreadId}`,
        thread: {
          Board: thread.Board,
          ThreadId: thread.ThreadId,
          Subject: thread.Subject
        }
      });

      if (index < activeThreads.length - 1) {
        await this.waitWithStop(this.betweenThreadDelayMs);
      }
    }
  }

  async waitWithStop(ms) {
    const step = Math.min(250, ms);
    let elapsed = 0;

    while (!this.stopping && elapsed < ms) {
      const wait = Math.min(step, ms - elapsed);
      await delay(wait);
      elapsed += wait;
    }
  }
}

import { MAX_ERROR_COUNT } from './constants.js';
import { postHasMedia } from './models.js';
import { escapeMarkup } from './file-name-sanitizer.js';
import { ThreadFetchService } from './thread-fetch-service.js';
import { MediaDownloadService } from './media-download-service.js';
import { WatchedThreadService } from './watched-thread-service.js';

export class ThreadProcessingService {
  constructor({
    fetchService = new ThreadFetchService(),
    downloadService = new MediaDownloadService(),
    watchedThreadService = new WatchedThreadService(),
    logger = console
  } = {}) {
    this.fetchService = fetchService;
    this.downloadService = downloadService;
    this.watchedThreadService = watchedThreadService;
    this.logger = logger;
  }

  async processThread(thread, allWatchedThreads) {
    const displaySubject = thread.Subject?.trim() ? thread.Subject : 'No Subject';
    this.logger.log(`Checking: ${thread.Board}/${thread.ThreadId} - ${escapeMarkup(displaySubject)}`);

    const fetchResult = await this.fetchService.fetchThread(thread);

    if (fetchResult.isSuccess && fetchResult.threadData) {
      thread.LastChecked = new Date().toISOString();
      await this.processSuccessfulFetch(thread, fetchResult.threadData);
      return;
    }

    if (fetchResult.isNotModified) {
      thread.LastChecked = new Date().toISOString();
      this.logger.log(`Thread ${thread.ThreadId} on /${thread.Board}/ has not been modified since last check.`);
      return;
    }

    await this.handleFetchError(thread, allWatchedThreads, fetchResult.statusCode);
  }

  async processSuccessfulFetch(thread, threadData) {
    if (!thread.Subject?.trim()) {
      thread.Subject = threadData.Posts[0]?.Subject ?? '';
      this.logger.log(`Retrieved subject: ${escapeMarkup(thread.Subject)}`);
    }

    this.logger.log(`Successfully fetched thread ${thread.ThreadId} on /${thread.Board}/`);

    const mediaPosts = threadData.Posts.filter(postHasMedia);
    const newMedia = mediaPosts.slice(thread.TotalDownloadedFiles);
    this.logger.log(`Found ${newMedia.length} new media files in thread ${thread.ThreadId}`);

    await this.downloadService.downloadMediaFiles(newMedia, thread);
    thread.TotalDownloadedFiles = mediaPosts.length;
  }

  async handleFetchError(thread, allWatchedThreads, statusCode) {
    if (statusCode === 404) {
      this.logger.error(`Thread ${thread.ThreadId} on /${thread.Board}/ returned 404 and will be deleted from Watched Threads list immediately.`);
      const index = allWatchedThreads.indexOf(thread);
      if (index >= 0) {
        allWatchedThreads.splice(index, 1);
      }
      await this.watchedThreadService.saveWatchedThreads(allWatchedThreads);
      return;
    }

    thread.ErrorCount += 1;
    this.logger.error(`Failed to fetch thread ${thread.ThreadId} on /${thread.Board}/ - Status Code: ${statusCode} (Total Errors: ${thread.ErrorCount}/3)`);

    if (thread.ErrorCount >= MAX_ERROR_COUNT) {
      this.logger.error(`Thread has failed ${thread.ErrorCount} times and will be deleted from Watched Threads list.`);
      const index = allWatchedThreads.indexOf(thread);
      if (index >= 0) {
        allWatchedThreads.splice(index, 1);
      }
      await this.watchedThreadService.saveWatchedThreads(allWatchedThreads);
    }
  }
}

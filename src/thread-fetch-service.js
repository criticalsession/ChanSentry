import { URLS, USER_AGENT } from './constants.js';
import { createCatalogThreads, createThread } from './models.js';

export class ThreadFetchResult {
  constructor({ isSuccess = false, isNotModified = false, threadData = null, statusCode = null } = {}) {
    this.isSuccess = isSuccess;
    this.isNotModified = isNotModified;
    this.threadData = threadData;
    this.statusCode = statusCode;
  }

  static success(threadData) {
    return new ThreadFetchResult({ isSuccess: true, threadData });
  }

  static notModified() {
    return new ThreadFetchResult({ isNotModified: true });
  }

  static failed(statusCode) {
    return new ThreadFetchResult({ statusCode });
  }
}

export class ThreadFetchService {
  constructor(fetchImpl = globalThis.fetch) {
    this.fetchImpl = fetchImpl;
  }

  async fetchCatalog(board) {
    const response = await this.fetchImpl(URLS.catalogUrl(board), {
      headers: {
        'User-Agent': USER_AGENT
      }
    });

    if (response.ok) {
      const catalogPages = await response.json();
      return {
        isSuccess: true,
        statusCode: response.status,
        catalogPages: catalogPages.map(createCatalogThreads)
      };
    }

    return {
      isSuccess: false,
      statusCode: response.status,
      catalogPages: []
    };
  }

  async fetchThread(thread) {
    const response = await this.fetchImpl(URLS.threadUrl(thread.Board, thread.ThreadId), {
      headers: {
        'User-Agent': USER_AGENT,
        'If-Modified-Since': formatHttpDate(thread.LastChecked)
      }
    });

    if (response.ok) {
      return ThreadFetchResult.success(createThread(await response.json()));
    }

    if (response.status === 304) {
      return ThreadFetchResult.notModified();
    }

    return ThreadFetchResult.failed(response.status);
  }
}

export function formatHttpDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? new Date(0).toUTCString() : date.toUTCString();
}
